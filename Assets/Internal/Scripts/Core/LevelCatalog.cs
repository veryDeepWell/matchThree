using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Game/Level Catalog")]
public sealed class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<LevelData> _levels = new List<LevelData>();

    public IReadOnlyList<LevelData> Levels => _levels;
    public int Count => _levels != null ? _levels.Count : 0;

    public LevelData GetLevel(int index)
    {
        return index >= 0 && index < Count ? _levels[index] : null;
    }

    public int GetLevelNumber(string levelName)
    {
        if (_levels == null || string.IsNullOrWhiteSpace(levelName))
            return 0;

        int index = _levels.FindIndex(level => level != null && level.name == levelName);
        return index >= 0 ? index + 1 : 0;
    }
}
