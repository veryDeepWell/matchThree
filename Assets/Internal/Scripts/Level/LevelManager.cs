using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class LevelManager : MonoBehaviour
{
    private Administrator _administrator;
    
    [SerializeField] private List<LevelData> _levels = new List<LevelData>();
    
    public int lastLevel;

    private void Awake()
    {
        _administrator = FindObjectOfType<Administrator>();
        if (_administrator == null)
        {
            Debug.LogError("Administrator not found in LevelManager!");
        }
    }

    private void Start()
    {
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogWarning("No levels added to LevelManager! Add levels in inspector.");
        }
    }

    public LevelData LoadLevel(int levelIndex)
    {
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError($"Level list is empty! Can't load level {levelIndex}");
            return null;
        }
        
        if (levelIndex < 0 || levelIndex >= _levels.Count)
        {
            Debug.LogError($"Level {levelIndex} not found! Total levels: {_levels.Count}");
            return null;
        }

        LevelData levelToLoad = _levels[levelIndex];
        
        if (levelToLoad == null)
        {
            Debug.LogError($"Level at index {levelIndex} is null!");
            return null;
        }

        Debug.Log($"Level loaded: {levelToLoad.name}");
        lastLevel = levelIndex;
        return levelToLoad;
    }
    
    public LevelData LoadLevel(string levelName)
    {
        if (_levels == null || _levels.Count == 0)
        {
            Debug.LogError("Level list is empty!");
            return null;
        }
        
        foreach (LevelData level in _levels)
        {
            if (level != null && level.name == levelName)
            {
                Debug.Log($"Level loaded: {level.name}");
                return level;
            }
        }
        
        Debug.LogError($"Level '{levelName}' not found!");
        return null;
    }
    
    public int GetLevelCount()
    {
        return _levels?.Count ?? 0;
    }
}