using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelCatalog _catalog;
    [SerializeField, FormerlySerializedAs("levels")] private List<LevelData> _levels = new List<LevelData>();
    [SerializeField, FormerlySerializedAs("lastLevel")] private int _lastLevel;

    public int LastLevel => _lastLevel;

    private IReadOnlyList<LevelData> Levels => _catalog != null ? _catalog.Levels : _levels;
    public LevelData FirstLevel => GetLevelCount() > 0 ? Levels[0] : null;

    public LevelData LoadLevel(int levelIndex)
    {
        if (Levels == null || Levels.Count == 0)
        {
            Debug.LogError($"[LevelManager] Level list is empty. Cannot load level {levelIndex}.");
            return null;
        }

        if (levelIndex < 0 || levelIndex >= Levels.Count)
        {
            Debug.LogError($"[LevelManager] Level {levelIndex} not found. Total levels: {Levels.Count}.");
            return null;
        }

        var level = Levels[levelIndex];
        if (level == null)
        {
            Debug.LogError($"[LevelManager] Level at index {levelIndex} is null.");
            return null;
        }

        _lastLevel = levelIndex;
        return level;
    }

    public LevelData LoadLevel(string levelName)
    {
        if (Levels == null || Levels.Count == 0)
        {
            Debug.LogError("[LevelManager] Level list is empty.");
            return null;
        }

        foreach (var level in Levels)
        {
            if (level != null && level.name == levelName)
                return level;
        }

        Debug.LogError($"[LevelManager] Level '{levelName}' not found.");
        return null;
    }

    public int GetLevelCount() => Levels?.Count ?? 0;

    public int GetLevelNumber(string levelName)
    {
        if (Levels == null || string.IsNullOrWhiteSpace(levelName))
            return 0;

        for (int index = 0; index < Levels.Count; index++)
        {
            LevelData level = Levels[index];
            if (level != null && level.name == levelName)
                return index + 1;
        }

        return 0;
    }

    public bool TryGetNextLevel(string currentLevelName, out LevelData nextLevel)
    {
        nextLevel = null;
        if (Levels == null || Levels.Count == 0 || string.IsNullOrWhiteSpace(currentLevelName))
            return false;

        int currentIndex = -1;
        for (int index = 0; index < Levels.Count; index++)
        {
            if (Levels[index] != null && Levels[index].name == currentLevelName)
            {
                currentIndex = index;
                break;
            }
        }
        if (currentIndex < 0)
        {
            Debug.LogError($"[LevelManager] Level '{currentLevelName}' is not present in the ordered level list.");
            return false;
        }

        for (int index = currentIndex + 1; index < Levels.Count; index++)
        {
            if (Levels[index] == null)
                continue;

            nextLevel = Levels[index];
            return true;
        }

        return false;
    }
}
