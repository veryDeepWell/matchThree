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

    public void BeginLevel(LevelData level, Board board, int levelNumber = 1)
    {
        if (level == null || board == null)
        {
            Debug.LogError("[SaveService] Cannot begin a level without LevelData and Board.");
            return;
        }

        if (level.GoalData == null)
        {
            Debug.LogError($"[SaveService] Level '{level.name}' has no LevelGoalData. The running level was not created.");
            Data.RunningLevel = null;
            SaveNow(SaveReason.Manual);
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
        Data.LevelProgress.CurrentLevelNumber = Mathf.Max(1, levelNumber);
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

    public void RepairRunningLevelRules(LevelData level)
    {
        if (level == null || level.GoalData == null || Data == null || Data.RunningLevel == null)
            return;

        RunningLevelSaveData runningLevel = Data.RunningLevel;
        bool changed = false;

        if (runningLevel.RemainingTime <= 0f && runningLevel.Status == LevelSessionStatus.InProgress)
        {
            runningLevel.RemainingTime = Mathf.Max(1, level.GoalData.TimeLimit);
            changed = true;
        }

        if (runningLevel.Goals == null || runningLevel.Goals.Count == 0)
        {
            runningLevel.Goals = new List<GoalProgressSaveData>();
            if (level.GoalData.Goals != null)
            {
                foreach (LevelGoal goal in level.GoalData.Goals)
                {
                    if (goal == null || string.IsNullOrEmpty(goal.TargetItemId))
                        continue;

                    runningLevel.Goals.Add(new GoalProgressSaveData
                    {
                        TargetItemId = goal.TargetItemId,
                        RequiredCount = Mathf.Max(1, goal.RequiredCount),
                        CurrentCount = 0
                    });
                }
            }
            changed = true;
        }

        if (runningLevel.GoldReward == 0 && level.GoalData.GoldReward > 0)
        {
            runningLevel.GoldReward = level.GoalData.GoldReward;
            changed = true;
        }
        if (runningLevel.CrystalReward == 0 && level.GoalData.CrystalReward > 0)
        {
            runningLevel.CrystalReward = level.GoalData.CrystalReward;
            changed = true;
        }

        if (!changed)
            return;

        runningLevel.ExtraTimeSeconds = Mathf.Max(1, level.GoalData.ExtraTimeSeconds);
        runningLevel.ExtraTimeGoldCost = Mathf.Max(0, level.GoalData.ExtraTimeGoldCost);
        runningLevel.AllowRepeatedExtraTime = level.GoalData.AllowRepeatedExtraTime;
        SaveNow(SaveReason.Manual);
    }

    public bool GrantVictoryRewards()
    {
        if (Data == null || Data.RunningLevel == null || Data.RunningLevel.VictoryRewardsGranted)
            return false;

        RunningLevelSaveData level = Data.RunningLevel;
        if (Data.LevelProgress != null && Data.LevelProgress.IsReplayMode)
        {
            level.VictoryRewardsGranted = true;
            level.Status = LevelSessionStatus.Victory;
            SaveNow(SaveReason.LevelVictory);
            return false;
        }

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

    public CosmeticSaveData GetCosmetic(string cosmeticId)
    {
        if (Data == null || Data.MacroProgress == null || Data.MacroProgress.Cosmetics == null)
            return null;

        return Data.MacroProgress.Cosmetics.Find(item => item.CosmeticId == cosmeticId);
    }

    public bool TryPurchaseCosmetic(string cosmeticId, int crystalPrice)
    {
        if (string.IsNullOrWhiteSpace(cosmeticId) || crystalPrice < 0 ||
            Data == null || Data.Economy == null || Data.MacroProgress == null)
            return false;

        CosmeticSaveData cosmetic = GetOrCreateCosmetic(cosmeticId);
        if (cosmetic.Purchased)
            return true;

        if (Data.Economy.Crystals < crystalPrice)
            return false;

        // Списание и покупка меняются до единственной записи файла, поэтому
        // сохранение не сможет содержать только половину этой операции.
        Data.Economy.Crystals -= crystalPrice;
        cosmetic.Purchased = true;
        SaveNow(SaveReason.CosmeticPurchased);
        return true;
    }

    public bool EquipCosmetic(string cosmeticId)
    {
        CosmeticSaveData selected = GetCosmetic(cosmeticId);
        if (selected == null || !selected.Purchased)
            return false;

        foreach (CosmeticSaveData cosmetic in Data.MacroProgress.Cosmetics)
            cosmetic.Equipped = cosmetic == selected;

        SaveNow(SaveReason.CosmeticEquipped);
        return true;
    }

    private CosmeticSaveData GetOrCreateCosmetic(string cosmeticId)
    {
        CosmeticSaveData cosmetic = GetCosmetic(cosmeticId);
        if (cosmetic != null)
            return cosmetic;

        cosmetic = new CosmeticSaveData { CosmeticId = cosmeticId };
        Data.MacroProgress.Cosmetics.Add(cosmetic);
        return cosmetic;
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

    public void GrantBonus(string bonusId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(bonusId) || amount <= 0)
            return;

        GetOrCreateBonus(bonusId).Count += amount;
        SaveNow(SaveReason.RewardGranted);
    }

    public bool TryBuyBonus(string bonusId, int goldPrice)
    {
        if (string.IsNullOrWhiteSpace(bonusId) || goldPrice < 0 ||
            Data == null || Data.Economy == null || Data.Economy.Gold < goldPrice)
            return false;

        // Списание и выдача записываются одним сохранением: при сбое игрок
        // не останется без золота и без купленного бонуса.
        Data.Economy.Gold -= goldPrice;
        GetOrCreateBonus(bonusId).Count++;
        SaveNow(SaveReason.BonusPurchased);
        return true;
    }

    public void GrantLife(int amount = 1)
    {
        if (amount <= 0 || Data == null || Data.Economy == null)
            return;

        Data.Economy.Lives = Mathf.Min(MaximumLives, Data.Economy.Lives + amount);
        if (Data.Economy.Lives >= MaximumLives)
            Data.Economy.NextLifeRestoreUtcSeconds = 0;
        SaveNow(SaveReason.RewardGranted);
    }

    public void RestoreAllLives()
    {
        if (Data == null || Data.Economy == null)
            return;

        Data.Economy.Lives = MaximumLives;
        Data.Economy.NextLifeRestoreUtcSeconds = 0;
        SaveNow(SaveReason.RewardGranted);
    }

    public bool TryBuyLife(int goldPrice)
    {
        if (goldPrice < 0 || Data == null || Data.Economy == null ||
            Data.Economy.Lives > 0 || Data.Economy.Gold < goldPrice)
            return false;

        Data.Economy.Gold -= goldPrice;
        Data.Economy.Lives = MaximumLives;
        Data.Economy.NextLifeRestoreUtcSeconds = 0;
        SaveNow(SaveReason.LifePurchased);
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

    public void SelectLevelForReplay(LevelData level, int levelNumber)
    {
        if (Data == null || level == null || levelNumber < 1)
            return;

        Data.LevelProgress.CurrentLevelName = level.name;
        Data.LevelProgress.CurrentLevelNumber = levelNumber;
        Data.LevelProgress.IsReplayMode = true;
        Data.RunningLevel = null;
        SaveNow(SaveReason.Manual);
    }

    public void CompleteRunningLevel(string nextLevelName = null, int nextLevelNumber = 0)
    {
        if (Data == null || Data.RunningLevel == null)
        {
            Debug.LogWarning("[SaveService] There is no running level to complete.");
            return;
        }

        string completedLevelName = Data.RunningLevel.LevelName;

        if (Data.LevelProgress.IsReplayMode)
        {
            Data.RunningLevel = null;
            SaveNow(SaveReason.LevelVictory);
            return;
        }

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

        if (string.IsNullOrWhiteSpace(nextLevelName))
        {
            Data.LevelProgress.AllAvailableLevelsCompleted = true;
        }
        else
        {
            Data.LevelProgress.CurrentLevelName = nextLevelName;
            Data.LevelProgress.CurrentLevelNumber = Mathf.Max(1, nextLevelNumber);
            Data.LevelProgress.AllAvailableLevelsCompleted = false;
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
        if (data.LevelProgress.CurrentLevelNumber <= 0)
            data.LevelProgress.CurrentLevelNumber = Mathf.Max(1, data.LevelProgress.CompletedLevelNames.Count + 1);
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
