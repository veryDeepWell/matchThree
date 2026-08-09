using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class SaveService : MonoBehaviour
{
    private const int CurrentSaveVersion = 1;
    private const int LifeRestoreIntervalSeconds = 15 * 60;
    private const int MaximumLives = 5;
    private const string SaveFileName = "player-save.json";
    private const string BackupFileName = "player-save.backup.json";
    private const string TemporaryFileName = "player-save.tmp.json";

    public static SaveService Instance { get; private set; }
    public MetaSaveData Data { get; private set; }
    public string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public bool HasRunningLevel =>
        Data?.RunningLevel != null &&
        Data.RunningLevel.Status != LevelSessionStatus.None &&
        Data.RunningLevel.Board != null;

    public event Action<MetaSaveData> SaveLoaded;
    public event Action<SaveReason> Saved;

    private bool _continueRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var serviceObject = new GameObject(nameof(SaveService));
        serviceObject.AddComponent<SaveService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Load()
    {
        string backupPath = Path.Combine(Application.persistentDataPath, BackupFileName);
        Data = TryReadSave(SavePath) ?? TryReadSave(backupPath) ?? CreateDefaultData();
        Normalize(Data);
        RefreshLives();
        SaveLoaded?.Invoke(Data);
    }

    public bool TryGetRunningLevel(out RunningLevelSaveData runningLevel)
    {
        runningLevel = HasRunningLevel ? Data.RunningLevel : null;
        return runningLevel != null;
    }

    public void RequestContinue()
    {
        _continueRequested = true;
    }

    public bool TryConsumeContinueRequest(out RunningLevelSaveData runningLevel)
    {
        bool shouldContinue = _continueRequested;
        _continueRequested = false;

        runningLevel = shouldContinue && HasRunningLevel ? Data.RunningLevel : null;
        return runningLevel != null;
    }

    public void BeginLevel(LevelData level, Board board)
    {
        if (level == null || board == null)
        {
            Debug.LogError("[SaveService] Cannot begin a level without LevelData and Board.");
            return;
        }

        var goals = new List<GoalProgressSaveData>();
        if (level.GoalData?.Goals != null)
        {
            foreach (LevelGoal goal in level.GoalData.Goals)
            {
                if (goal == null)
                    continue;

                goals.Add(new GoalProgressSaveData
                {
                    TargetItemId = goal.TargetItemId ?? string.Empty,
                    RequiredCount = goal.RequiredCount,
                    CurrentCount = 0
                });
            }
        }

        Data.LevelProgress.CurrentLevelName = level.name;
        Data.RunningLevel = new RunningLevelSaveData
        {
            LevelName = level.name,
            Status = LevelSessionStatus.InProgress,
            RemainingTime = level.GoalData != null ? level.GoalData.TimeLimit : 0f,
            GoldReward = level.GoalData != null ? level.GoalData.GoldReward : 0,
            CrystalReward = level.GoalData != null ? level.GoalData.CrystalReward : 0,
            ExtraTimeSeconds = level.GoalData != null ? level.GoalData.ExtraTimeSeconds : 120,
            ExtraTimeGoldCost = level.GoalData != null ? level.GoalData.ExtraTimeGoldCost : 1000,
            AllowRepeatedExtraTime = level.GoalData != null && level.GoalData.AllowRepeatedExtraTime,
            Goals = goals,
            Board = board.CreateSnapshot()
        };

        SaveNow(SaveReason.LevelStarted);
    }

    public bool GrantVictoryRewards()
    {
        if (Data == null || Data.RunningLevel == null || Data.RunningLevel.VictoryRewardsGranted)
            return false;

        RunningLevelSaveData level = Data.RunningLevel;
        Data.Economy.Gold += Mathf.Max(0, level.GoldReward);
        Data.Economy.Crystals += Mathf.Max(0, level.CrystalReward);
        level.VictoryRewardsGranted = true;
        level.Status = LevelSessionStatus.Victory;
        SaveNow(SaveReason.RewardGranted);
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (Data == null || Data.Economy == null || amount < 0 || Data.Economy.Gold < amount)
            return false;

        Data.Economy.Gold -= amount;
        SaveNow(SaveReason.RewardGranted);
        return true;
    }

    public int GetBonusCount(string bonusId)
    {
        BonusInventoryItemSaveData bonus = FindBonus(bonusId);
        return bonus != null ? Mathf.Max(0, bonus.Count) : 0;
    }

    public bool TryConsumeBonus(string bonusId)
    {
        BonusInventoryItemSaveData bonus = FindBonus(bonusId);
        if (bonus == null || bonus.Count <= 0)
            return false;

        bonus.Count--;
        SaveNow(SaveReason.RewardGranted);
        return true;
    }

    public void SetAllGameplayBonuses(int count)
    {
        string[] bonusIds = { "bomb", "sweeper_h", "sweeper_cross", "magnet", "sweeper_v" };
        foreach (string bonusId in bonusIds)
            GetOrCreateBonus(bonusId).Count = Mathf.Max(0, count);

        SaveNow(SaveReason.RewardGranted);
    }

    private BonusInventoryItemSaveData FindBonus(string bonusId)
    {
        if (Data == null || Data.Economy == null || Data.Economy.Bonuses == null)
            return null;

        return Data.Economy.Bonuses.Find(bonus => bonus.BonusId == bonusId);
    }

    private BonusInventoryItemSaveData GetOrCreateBonus(string bonusId)
    {
        BonusInventoryItemSaveData bonus = FindBonus(bonusId);
        if (bonus != null)
            return bonus;

        bonus = new BonusInventoryItemSaveData { BonusId = bonusId, Count = 0 };
        Data.Economy.Bonuses.Add(bonus);
        return bonus;
    }

    public bool RefreshLives()
    {
        if (Data == null || Data.Economy == null)
            return false;

        EconomySaveData economy = Data.Economy;
        int previousLives = economy.Lives;
        long previousRestoreTime = economy.NextLifeRestoreUtcSeconds;

        if (economy.Lives >= MaximumLives)
        {
            economy.Lives = MaximumLives;
            economy.NextLifeRestoreUtcSeconds = 0;
            return previousLives != economy.Lives || previousRestoreTime != economy.NextLifeRestoreUtcSeconds;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (economy.NextLifeRestoreUtcSeconds == 0)
        {
            economy.NextLifeRestoreUtcSeconds = now + LifeRestoreIntervalSeconds;
            return true;
        }

        while (economy.Lives < MaximumLives && now >= economy.NextLifeRestoreUtcSeconds)
        {
            economy.Lives++;
            economy.NextLifeRestoreUtcSeconds += LifeRestoreIntervalSeconds;
        }

        if (economy.Lives >= MaximumLives)
            economy.NextLifeRestoreUtcSeconds = 0;

        return previousLives != economy.Lives || previousRestoreTime != economy.NextLifeRestoreUtcSeconds;
    }

    public bool CaptureBoard(Board board, SaveReason reason = SaveReason.Manual)
    {
        if (board == null || board.Data == null)
        {
            Debug.LogWarning("[SaveService] Board is not ready, snapshot was not saved.");
            return false;
        }

        if (Data.RunningLevel == null)
        {
            Data.RunningLevel = new RunningLevelSaveData
            {
                LevelName = board.CurrentLevel != null ? board.CurrentLevel.name : string.Empty
            };
        }

        Data.RunningLevel.Board = board.CreateSnapshot();
        SaveNow(reason);
        return true;
    }

    public bool CaptureCurrentBoard(SaveReason reason)
    {
        Board board = FindFirstObjectByType<Board>();
        if (board == null || board.Data == null)
        {
            SaveNow(reason);
            return false;
        }

        return CaptureBoard(board, reason);
    }

    public void SetLevelStatus(LevelSessionStatus status, SaveReason reason)
    {
        if (Data.RunningLevel == null)
        {
            Debug.LogWarning("[SaveService] There is no running level whose status can be changed.");
            return;
        }

        Data.RunningLevel.Status = status;
        SaveNow(reason);
    }

    public void SetRemainingTime(float remainingTime, bool saveImmediately = false)
    {
        if (Data.RunningLevel == null)
            return;

        Data.RunningLevel.RemainingTime = Mathf.Max(0f, remainingTime);
        if (saveImmediately)
            SaveNow(SaveReason.Manual);
    }

    public void SetGoalProgress(string targetItemId, int currentCount, bool saveImmediately = false)
    {
        if (Data.RunningLevel?.Goals == null)
            return;

        GoalProgressSaveData goal = Data.RunningLevel.Goals.Find(x => x.TargetItemId == targetItemId);
        if (goal == null)
            return;

        goal.CurrentCount = Mathf.Clamp(currentCount, 0, goal.RequiredCount);
        if (saveImmediately)
            SaveNow(SaveReason.Manual);
    }

    public void ClearRunningLevel(bool saveImmediately = true)
    {
        Data.RunningLevel = null;
        if (saveImmediately)
            SaveNow(SaveReason.Manual);
    }

    public void CompleteRunningLevel()
    {
        if (Data == null || Data.RunningLevel == null)
        {
            Debug.LogWarning("[SaveService] There is no running level to complete.");
            return;
        }

        string completedLevelName = Data.RunningLevel.LevelName;

        if (!string.IsNullOrWhiteSpace(completedLevelName))
        {
            if (!Data.LevelProgress.CompletedLevelNames.Contains(completedLevelName))
                Data.LevelProgress.CompletedLevelNames.Add(completedLevelName);

            Data.Victory = new VictorySaveData
            {
                CompletedLevelName = completedLevelName,
                PostLevelAdCompleted = false
            };
        }

        // Победная попытка закончена и больше не должна предлагаться кнопкой «Продолжить».
        Data.RunningLevel = null;
        SaveNow(SaveReason.LevelVictory);
    }

    public void FinishRunningLevelWithDefeat()
    {
        if (Data == null || Data.RunningLevel == null)
        {
            Debug.LogWarning("[SaveService] There is no running level to finish with defeat.");
            return;
        }

        if (Data.RunningLevel.Status != LevelSessionStatus.Defeat)
        {
            Debug.LogWarning("[SaveService] A level can lose a life only after final defeat.");
            return;
        }

        if (Data.Economy.Lives > 0)
        {
            Data.Economy.Lives--;

            if (Data.Economy.NextLifeRestoreUtcSeconds == 0)
            {
                long currentUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Data.Economy.NextLifeRestoreUtcSeconds = currentUtcSeconds + LifeRestoreIntervalSeconds;
            }
        }

        // После окончательного поражения продолжать эту попытку уже нельзя.
        Data.RunningLevel = null;
        SaveNow(SaveReason.LevelDefeat);
    }

    public void SaveNow(SaveReason reason = SaveReason.Manual)
    {
        if (Data == null)
            Data = CreateDefaultData();

        Normalize(Data);

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);

            string json = JsonUtility.ToJson(Data, true);
            string temporaryPath = Path.Combine(Application.persistentDataPath, TemporaryFileName);
            string backupPath = Path.Combine(Application.persistentDataPath, BackupFileName);

            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(SavePath))
            {
                try
                {
                    File.Replace(temporaryPath, SavePath, backupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceWithCopy(temporaryPath, SavePath, backupPath);
                }
                catch (IOException)
                {
                    ReplaceWithCopy(temporaryPath, SavePath, backupPath);
                }
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }

            Saved?.Invoke(reason);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveService] Failed to save progress: {exception}");
        }
    }

    private static void ReplaceWithCopy(string temporaryPath, string savePath, string backupPath)
    {
        if (File.Exists(savePath))
            File.Copy(savePath, backupPath, true);

        File.Copy(temporaryPath, savePath, true);
        File.Delete(temporaryPath);
    }

    private static MetaSaveData TryReadSave(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return JsonUtility.FromJson<MetaSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveService] Could not read '{path}': {exception.Message}");
            return null;
        }
    }

    private static MetaSaveData CreateDefaultData()
    {
        return new MetaSaveData
        {
            SaveVersion = CurrentSaveVersion
        };
    }

    private static void Normalize(MetaSaveData data)
    {
        data.SaveVersion = CurrentSaveVersion;
        data.Economy ??= new EconomySaveData();
        data.Economy.Bonuses ??= new List<BonusInventoryItemSaveData>();
        data.LevelProgress ??= new LevelProgressSaveData();
        data.LevelProgress.CurrentLevelName ??= string.Empty;
        data.LevelProgress.CompletedLevelNames ??= new List<string>();
        data.MacroProgress ??= new MacroProgressSaveData();
        data.MacroProgress.Cosmetics ??= new List<CosmeticSaveData>();

        if (data.RunningLevel != null)
        {
            data.RunningLevel.LevelName ??= string.Empty;
            data.RunningLevel.Goals ??= new List<GoalProgressSaveData>();

            if (data.RunningLevel.Board != null && !data.RunningLevel.Board.IsStructurallyValid())
            {
                Debug.LogWarning("[SaveService] Saved board has invalid dimensions or arrays. The running level was discarded.");
                data.RunningLevel = null;
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            CaptureCurrentBoard(SaveReason.ApplicationPaused);
    }

    private void OnApplicationQuit()
    {
        CaptureCurrentBoard(SaveReason.ApplicationQuit);
    }
}
