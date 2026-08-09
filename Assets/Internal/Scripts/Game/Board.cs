using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private bool _useRandomLevel = false;
    [SerializeField] private LevelData _testLevel;
    [SerializeField] private ItemGenerator _itemGenerator;
    [SerializeField] private MatchesHandler _matchesHandler;

    [Header("Position")]
    [SerializeField] private Vector2 _offset = Vector2.zero;
    [SerializeField] private float _cellSize = 1f;

    [Header("Hints")]
    [SerializeField, Min(1f)] private float _hintDelay = 8f;
    [SerializeField] private float _hintDistance = 0.12f;
    [SerializeField] private float _hintDuration = 0.18f;

    public BoardData Data { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Item[,] Items { get; private set; }
    public SpecialCell[,] SpecialCells { get; private set; }

    public bool IsProcessing => _matchesHandler != null && _matchesHandler.IsProcessing;
    public bool HasQueuedSpecialItems => _queuedSpecialItems.Count > 0;

    private readonly HashSet<SpecialItem> _queuedSpecialItems = new HashSet<SpecialItem>();

    private int _lastSwapX = -1;
    private int _lastSwapY = -1;
    private int _secondSwapX = -1;
    private int _secondSwapY = -1;
    private int _bombTriggerX = -1;
    private int _bombTriggerY = -1;
    private Coroutine _hintCoroutine;

    public void SetBombTriggerPosition(int x, int y)
    {
        _bombTriggerX = x;
        _bombTriggerY = y;
    }

    public (int x, int y) GetBombTriggerPosition() => (_bombTriggerX, _bombTriggerY);

    public void ClearBombTriggerPosition()
    {
        _bombTriggerX = -1;
        _bombTriggerY = -1;
    }

    public void ForceLoadLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[Board] Level is null.");
            return;
        }

        LoadLevel(level);
    }

    public Vector2 GetWorldPosition(int column, int row)
    {
        return new Vector2(column * _cellSize, row * _cellSize) + _offset;
    }

    public void GetGridPosition(Vector2 worldPosition, out int column, out int row)
    {
        Vector2 localPosition = worldPosition - _offset;
        column = Mathf.RoundToInt(localPosition.x / _cellSize);
        row = Mathf.RoundToInt(localPosition.y / _cellSize);
    }

    public Vector2 GetCellSize() => Vector2.one * _cellSize;

    public void LoadLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[Board] Cannot load null level.");
            return;
        }

        CurrentLevel = level;
        level.EnsureArrays();

        ClearRuntimeTiles();
        Data = level.ToBoardData();

        if (!Data.IsStructurallyValid())
        {
            Debug.LogError("[Board] Level data is structurally invalid.");
            return;
        }

        Width = Data.Width;
        Height = Data.Height;
        Items = new Item[Width, Height];
        SpecialCells = new SpecialCell[Width, Height];

        if (_itemGenerator == null)
            _itemGenerator = FindObjectOfType<ItemGenerator>();

        if (_matchesHandler == null)
            _matchesHandler = FindObjectOfType<MatchesHandler>();

        if (_itemGenerator != null)
            _itemGenerator.GenerateItems(this);
        else
            Debug.LogError("[Board] ItemGenerator is missing.");

        ResetHintTimer();
    }

    public void LoadFromData(BoardData data)
    {
        if (data == null || !data.IsStructurallyValid())
        {
            Debug.LogWarning("[Board] Cannot load invalid board data.");
            return;
        }

        ClearRuntimeTiles();
        Data = data;
        Width = data.Width;
        Height = data.Height;
        Items = new Item[Width, Height];
        SpecialCells = new SpecialCell[Width, Height];

        _itemGenerator?.GenerateItems(this);
        ResetHintTimer();
    }

    public bool IsCellActive(int column, int row)
    {
        return Data != null && Data.IsActive(column, row);
    }

    public string GetItemId(int column, int row)
    {
        return Data != null ? Data.GetItem(column, row) : string.Empty;
    }

    public void SetItemId(int column, int row, string id)
    {
        Data?.SetItem(column, row, id);
    }

    public void SetSpecialItemId(int column, int row, string id)
    {
        Data?.SetSpecialItem(column, row, id);
    }

    public SpecialCell GetSpecialCell(int column, int row)
    {
        if (SpecialCells == null || column < 0 || column >= Width || row < 0 || row >= Height)
            return null;

        return SpecialCells[column, row];
    }

    public void SetSpecialCell(int column, int row, SpecialCell cell)
    {
        if (SpecialCells == null || column < 0 || column >= Width || row < 0 || row >= Height)
            return;

        SpecialCells[column, row] = cell;
    }

    public void CheckMatches(int swapX, int swapY)
    {
        CheckMatches(swapX, swapY, -1, -1);
    }

    public void CheckMatches(int firstSwapX, int firstSwapY, int secondSwapX, int secondSwapY)
    {
        _lastSwapX = firstSwapX;
        _lastSwapY = firstSwapY;
        _secondSwapX = secondSwapX;
        _secondSwapY = secondSwapY;
        ResetHintTimer();
        _matchesHandler?.ProcessMatches(this);
    }

    public void CheckMatches()
    {
        _matchesHandler?.ProcessMatches(this);
    }

    public (int x, int y) GetLastSwapPosition() => (_lastSwapX, _lastSwapY);
    public (int x, int y) GetSecondSwapPosition() => (_secondSwapX, _secondSwapY);

    public void ClearLastSwapPosition()
    {
        _lastSwapX = -1;
        _lastSwapY = -1;
        _secondSwapX = -1;
        _secondSwapY = -1;
    }

    public void QueueSpecialItem(SpecialItem specialItem)
    {
        if (specialItem != null)
            _queuedSpecialItems.Add(specialItem);
    }

    public List<SpecialItem> ConsumeQueuedSpecialItems()
    {
        var result = new List<SpecialItem>(_queuedSpecialItems);
        _queuedSpecialItems.Clear();
        return result;
    }

    public void ResetHintTimer()
    {
        if (!Application.isPlaying)
            return;

        if (_hintCoroutine != null)
            StopCoroutine(_hintCoroutine);

        _hintCoroutine = StartCoroutine(HintRoutine());
    }

    public BoardData CreateSnapshot()
    {
        return Data?.Clone();
    }

    public void RestoreSnapshot(BoardData snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[Board] Snapshot is null.");
            return;
        }

        LoadFromData(snapshot);
    }

    private IEnumerator HintRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_hintDelay);

            if (IsProcessing || Data == null)
                continue;

            if (!MatchValidator.TryFindPossibleMove(Data, out var first, out var second))
                continue;

            var firstItem = Items[first.x, first.y];
            var secondItem = Items[second.x, second.y];

            if (firstItem == null || secondItem == null)
                continue;

            Vector2 direction = new Vector2(second.x - first.x, second.y - first.y).normalized;
            yield return StartCoroutine(firstItem.PlayHint(direction, _hintDistance, _hintDuration));
            yield return StartCoroutine(secondItem.PlayHint(-direction, _hintDistance, _hintDuration));
        }
    }

    private void ClearRuntimeTiles()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            var child = transform.GetChild(childIndex);
            if (!child.name.StartsWith("Tile("))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Data == null)
            return;

        Gizmos.color = Color.green;

        for (int column = 0; column < Data.Width; column++)
        {
            for (int row = 0; row < Data.Height; row++)
            {
                Vector2 position = GetWorldPosition(column, row);
                Gizmos.DrawWireCube(position, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}
