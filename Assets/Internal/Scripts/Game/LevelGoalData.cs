using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelGoalData", menuName = "Game/Level Goal Data")]
public sealed class LevelGoalData : ScriptableObject
{
    public LevelGoal[] Goals = Array.Empty<LevelGoal>();
    public int TimeLimit = 60;
    public int GoldReward = 200;
    public int CrystalReward = 10;
    public int ExtraTimeSeconds = 120;
    public int ExtraTimeGoldCost = 1000;
    public bool AllowRepeatedExtraTime;
}
