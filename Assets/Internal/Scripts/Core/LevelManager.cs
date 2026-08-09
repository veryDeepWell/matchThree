using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("levels")] private List<LevelData> _levels = new List<LevelData>();
    [SerializeField, FormerlySerializedAs("lastLevel")] private int _lastLevel;

    public int LastLevel => _lastLevel;

    public LevelData LoadLevel(int levelIndex)
    {
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError($"[LevelManager] Level list is empty. Cannot load level {levelIndex}.");
            return null;
        }

        if (levelIndex < 0 || levelIndex >= _levels.Count)
        {
            Debug.LogError($"[LevelManager] Level {levelIndex} not found. Total levels: {_levels.Count}.");
            return null;
        }

        var level = _levels[levelIndex];
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
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError("[LevelManager] Level list is empty.");
            return null;
        }

        foreach (var level in _levels)
        {
            if (level != null && level.name == levelName)
                return level;
        }

        Debug.LogError($"[LevelManager] Level '{levelName}' not found.");
        return null;
    }

    public int GetLevelCount() => _levels?.Count ?? 0;
}
