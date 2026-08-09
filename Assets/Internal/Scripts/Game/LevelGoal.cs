using System;
using UnityEngine;

[Serializable]
public class LevelGoal
{
    public string TargetItemId;  // ← вместо ItemTypes
    public int RequiredCount;
    public int CurrentCount;
}

[CreateAssetMenu(fileName = "LevelGoalData", menuName = "Game/Level Goal Data")]
public class LevelGoalData : ScriptableObject
{
    public LevelGoal[] Goals = Array.Empty<LevelGoal>();
    public int TimeLimit = 60;
    public int GoldReward = 200;
    public int CrystalReward = 10;
    public int ExtraTimeSeconds = 120;
    public int ExtraTimeGoldCost = 1000;
    public bool AllowRepeatedExtraTime;
}
