using UnityEngine;

[DefaultExecutionOrder(-999)]
public class GameLoader : MonoBehaviour
{
    [Header("References (drag from scene)")]
    [SerializeField] private ItemHandler _itemHandler;
    [SerializeField] private ItemGenerator _itemGenerator;
    [SerializeField] private MatchesHandler _matchesHandler;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private Board _board;

    [Header("Level to load")]
    [SerializeField] private int _levelIndex = 0;

    private void Awake()
    {
        ResolveReferences();
        if (!ValidateReferences())
            return;

        _itemHandler.ForceInitialize();
        _itemGenerator.ForceInitialize(_itemHandler);

        SaveService saveService = SaveService.Instance;

        // Сначала пробуем продолжить попытку, выбранную кнопкой «Продолжить» в меню.
        if (saveService != null &&
            saveService.TryConsumeContinueRequest(out RunningLevelSaveData runningLevel))
        {
            LevelData savedLevel = _levelManager.LoadLevel(runningLevel.LevelName);
            if (savedLevel != null && runningLevel.Board != null)
            {
                _board.RestoreSnapshot(runningLevel.Board);
                Debug.Log($"[GameLoader] Восстановлен сохранённый уровень: {runningLevel.LevelName}");
                return;
            }

            Debug.LogWarning("[GameLoader] Сохранённый уровень не удалось восстановить. Запускается настроенный уровень.");
        }

        LevelData level = LoadCurrentLevel(saveService);
        if (level == null)
        {
            Debug.LogError("[GameLoader] Failed to load level.");
            return;
        }

        _board.ForceLoadLevel(level);
        if (saveService != null)
            saveService.BeginLevel(level, _board);
    }

    private LevelData LoadCurrentLevel(SaveService saveService)
    {
        string currentLevelName = null;

        if (saveService != null)
        {
            if (saveService.Data != null)
            {
                if (saveService.Data.LevelProgress != null)
                    currentLevelName = saveService.Data.LevelProgress.CurrentLevelName;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentLevelName))
        {
            LevelData currentLevel = _levelManager.LoadLevel(currentLevelName);
            if (currentLevel != null)
                return currentLevel;
        }

        return _levelManager.LoadLevel(_levelIndex);
    }

    private void ResolveReferences()
    {
        _itemHandler ??= FindObjectOfType<ItemHandler>();
        _itemGenerator ??= FindObjectOfType<ItemGenerator>();
        _matchesHandler ??= FindObjectOfType<MatchesHandler>();
        _levelManager ??= FindObjectOfType<LevelManager>();
        _board ??= FindObjectOfType<Board>();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;
        if (_itemHandler == null) { Debug.LogError("[GameLoader] ItemHandler not found."); isValid = false; }
        if (_itemGenerator == null) { Debug.LogError("[GameLoader] ItemGenerator not found."); isValid = false; }
        if (_matchesHandler == null) { Debug.LogError("[GameLoader] MatchesHandler not found."); isValid = false; }
        if (_levelManager == null) { Debug.LogError("[GameLoader] LevelManager not found."); isValid = false; }
        if (_board == null) { Debug.LogError("[GameLoader] Board not found."); isValid = false; }
        return isValid;
    }
}
