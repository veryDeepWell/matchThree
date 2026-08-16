using System;
using System.Collections.Generic;

public enum LevelSessionStatus
{
    None,
    InProgress,
    ContinueOffer,
    Victory,
    Defeat
}

public enum SaveReason
{
    Manual,
    LevelStarted,
    LevelPaused,
    BeforeAdvertisement,
    AfterAdvertisement,
    ContinueOffer,
    ExtraTimeGranted,
    RewardGranted,
    CosmeticPurchased,
    CosmeticEquipped,
    LevelVictory,
    LevelDefeat,
    ApplicationPaused,
    ApplicationQuit
}

[Serializable]
public sealed class MetaSaveData
{
    public int SaveVersion = 1;
    public EconomySaveData Economy = new EconomySaveData();
    public LevelProgressSaveData LevelProgress = new LevelProgressSaveData();
    public RunningLevelSaveData RunningLevel;
    public MacroProgressSaveData MacroProgress = new MacroProgressSaveData();
    public PendingAdSaveData PendingAd;
    public VictorySaveData Victory;
}

[Serializable]
public sealed class EconomySaveData
{
    public int Gold;
    public int Crystals;
    public int Lives = 5;

    // Unix-время в UTC. Ноль означает, что восстановление жизни сейчас не запущено.
    public long NextLifeRestoreUtcSeconds;
    public List<BonusInventoryItemSaveData> Bonuses = new List<BonusInventoryItemSaveData>();
}

[Serializable]
public sealed class BonusInventoryItemSaveData
{
    public string BonusId = string.Empty;
    public int Count;
}

[Serializable]
public sealed class LevelProgressSaveData
{
    public string CurrentLevelName = string.Empty;
    public int CurrentLevelNumber = 1;
    public bool AllAvailableLevelsCompleted;
    public bool IsReplayMode;
    public List<string> CompletedLevelNames = new List<string>();
}

[Serializable]
public sealed class RunningLevelSaveData
{
    public string LevelName = string.Empty;
    public LevelSessionStatus Status = LevelSessionStatus.InProgress;
    public float RemainingTime;
    public int ExtraTimeUses;
    public int GoldReward;
    public int CrystalReward;
    public int ExtraTimeSeconds = 120;
    public int ExtraTimeGoldCost = 1000;
    public bool AllowRepeatedExtraTime;
    public bool VictoryRewardsGranted;
    public List<GoalProgressSaveData> Goals = new List<GoalProgressSaveData>();
    public BoardData Board;
}

[Serializable]
public sealed class GoalProgressSaveData
{
    public string TargetItemId = string.Empty;
    public int RequiredCount;
    public int CurrentCount;
}

[Serializable]
public sealed class MacroProgressSaveData
{
    public List<CosmeticSaveData> Cosmetics = new List<CosmeticSaveData>();
}

[Serializable]
public sealed class CosmeticSaveData
{
    public string CosmeticId = string.Empty;
    public bool Purchased;
    public bool Equipped;
}

[Serializable]
public sealed class PendingAdSaveData
{
    public string TransactionId = string.Empty;
    public string PlacementId = string.Empty;
    public string RewardId = string.Empty;
    public bool RewardGranted;
}

[Serializable]
public sealed class VictorySaveData
{
    public string CompletedLevelName = string.Empty;
    public bool PostLevelAdCompleted;
}
