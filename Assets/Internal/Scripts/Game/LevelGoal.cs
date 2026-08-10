using System;

[Serializable]
public class LevelGoal
{
    public string TargetItemId;  // ← вместо ItemTypes
    public int RequiredCount;
    public int CurrentCount;
}
