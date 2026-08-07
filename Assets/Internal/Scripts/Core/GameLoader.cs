using UnityEngine;

[DefaultExecutionOrder(-999)]
public class GameLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemHandler _itemHandler;
    [SerializeField] private ItemGenerator _itemGenerator;
    [SerializeField] private MatchesHandler _matchesHandler;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private Board _board;

    [Header("Level to load")]
    [SerializeField] private int _levelIndex;

    private void Awake()
    {
        ResolveReferences();
        if (!ValidateReferences()) return;

        _itemHandler.ForceInitialize();
        _itemGenerator.ForceInitialize(_itemHandler);

        var level = _levelManager.LoadLevel(_levelIndex);
        if (level == null)
        {
            Debug.LogError("[GameLoader] Failed to load level.");
            return;
        }

        _board.ForceLoadLevel(level);
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
