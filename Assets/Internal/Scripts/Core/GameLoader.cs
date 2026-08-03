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
        if (_itemHandler == null) _itemHandler = FindObjectOfType<ItemHandler>();
        if (_itemGenerator == null) _itemGenerator = FindObjectOfType<ItemGenerator>();
        if (_matchesHandler == null) _matchesHandler = FindObjectOfType<MatchesHandler>();
        if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
        if (_board == null) _board = FindObjectOfType<Board>();

        if (_itemHandler == null) Debug.LogError("[GameLoader] ItemHandler not found!");
        if (_itemGenerator == null) Debug.LogError("[GameLoader] ItemGenerator not found!");
        if (_matchesHandler == null) Debug.LogError("[GameLoader] MatchesHandler not found!");
        if (_levelManager == null) Debug.LogError("[GameLoader] LevelManager not found!");
        if (_board == null) Debug.LogError("[GameLoader] Board not found!");

        _itemHandler.ForceInitialize();
        _itemGenerator.ForceInitialize(_itemHandler);
        
        LevelData level = _levelManager.LoadLevel(_levelIndex);
        if (level != null)
        {
            _board.ForceLoadLevel(level);
        }
        else
        {
            Debug.LogError("[GameLoader] Failed to load level!");
        }
    }
}