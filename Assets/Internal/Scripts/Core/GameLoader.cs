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
        // 1. Находим всё, что не привязано в инспекторе
        if (_itemHandler == null) _itemHandler = FindObjectOfType<ItemHandler>();
        if (_itemGenerator == null) _itemGenerator = FindObjectOfType<ItemGenerator>();
        if (_matchesHandler == null) _matchesHandler = FindObjectOfType<MatchesHandler>();
        if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
        if (_board == null) _board = FindObjectOfType<Board>();

        // 2. Проверяем что всё найдено
        if (_itemHandler == null) Debug.LogError("[GameLoader] ItemHandler not found!");
        if (_itemGenerator == null) Debug.LogError("[GameLoader] ItemGenerator not found!");
        if (_matchesHandler == null) Debug.LogError("[GameLoader] MatchesHandler not found!");
        if (_levelManager == null) Debug.LogError("[GameLoader] LevelManager not found!");
        if (_board == null) Debug.LogError("[GameLoader] Board not found!");

        // 3. Инициализируем в правильном порядке
        _itemHandler.ForceInitialize();
        _itemGenerator.ForceInitialize(_itemHandler);
        
        // 4. Сначала пробуем продолжить сохранённую попытку.
        SaveService saveService = SaveService.Instance;
        if (saveService != null && saveService.TryConsumeContinueRequest(out RunningLevelSaveData runningLevel))
        {
            LevelData savedLevel = _levelManager.LoadLevel(runningLevel.LevelName);
            if (savedLevel != null && runningLevel.Board != null)
            {
                _board.RestoreSnapshot(runningLevel.Board, savedLevel);
                Debug.Log($"[GameLoader] Restored saved level: {runningLevel.LevelName}");
                return;
            }

            Debug.LogWarning("[GameLoader] Saved level could not be restored. Starting a configured level instead.");
        }

        // 5. Незавершённой попытки нет — загружаем текущий открытый уровень или Inspector fallback.
        LevelData level = null;
        string currentLevelName = saveService?.Data?.LevelProgress?.CurrentLevelName;
        if (!string.IsNullOrWhiteSpace(currentLevelName))
            level = _levelManager.LoadLevel(currentLevelName);

        if (level == null)
            level = _levelManager.LoadLevel(_levelIndex);

        if (level != null)
        {
            _board.ForceLoadLevel(level);
            saveService?.BeginLevel(level, _board);
        }
        else
        {
            Debug.LogError("[GameLoader] Failed to load level!");
        }
    }
}
