using System;

[Serializable]
public sealed class LevelGoalData
{
    public LevelGoal[] Goals = Array.Empty<LevelGoal>();
    public int TimeLimit = 60;
    public int GoldReward = 200;
    public int CrystalReward = 10;
    public int ExtraTimeSeconds = 120;
    public int ExtraTimeGoldCost = 1000;
    public bool AllowRepeatedExtraTime;
}
