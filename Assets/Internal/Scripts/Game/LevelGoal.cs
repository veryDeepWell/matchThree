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
    public LevelGoal[] Goals;
    public int TimeLimit = 60;
}