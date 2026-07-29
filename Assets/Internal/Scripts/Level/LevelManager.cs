using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class LevelManager : MonoBehaviour
{
    private Administrator _administrator;
    
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    
    public int lastLevel;

    private void Awake()
    {
        _administrator = FindFirstObjectByType<Administrator>();
        if (_administrator == null)
        {
            Debug.LogError("Administrator not found in LevelManager!");
        }
    }

    private void Start()
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("No levels added to LevelManager! Add levels in inspector.");
        }
    }

    public LevelData LoadLevel(int levelIndex)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError($"Level list is empty! Can't load level {levelIndex}");
            return null;
        }
        
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError($"Level {levelIndex} not found! Total levels: {levels.Count}");
            return null;
        }

        LevelData levelToLoad = levels[levelIndex];
        
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
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("Level list is empty!");
            return null;
        }
        
        foreach (LevelData level in levels)
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
        return levels?.Count ?? 0;
    }
}