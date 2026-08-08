using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private ItemHandler _itemHandler;
    [SerializeField] private SpecialItemHandler _specialItemHandler;
    [SerializeField] private SpecialCellHandler _specialCellHandler;

    private List<string> _availableItemIds = new List<string>();
    private bool _isInitialized;

    public GameObject GetTilePrefab() => _tilePrefab;

    public void ForceInitialize(ItemHandler itemHandler)
    {
        if (itemHandler != null)
            _itemHandler = itemHandler;

        if (_itemHandler == null)
        {
            Debug.LogError("[ItemGenerator] ItemHandler is not assigned.");
            return;
        }

        var registry = _itemHandler.GetRegistry();
        if (registry == null)
        {
            Debug.LogError("[ItemGenerator] ItemRegistry is missing.");
            return;
        }

        registry.Initialize();
        _availableItemIds.Clear();

        foreach (var definition in registry.GetNormalItems())
        {
            if (definition != null && !string.IsNullOrEmpty(definition.Id))
                _availableItemIds.Add(definition.Id);
        }

        _specialItemHandler ??= FindObjectOfType<SpecialItemHandler>();
        _specialCellHandler ??= FindObjectOfType<SpecialCellHandler>();
        _isInitialized = _availableItemIds.Count > 0;

        if (!_isInitialized)
            Debug.LogError("[ItemGenerator] No normal item definitions are available.");
    }

    public void GenerateItems(Board board)
    {
        if (!ValidateGeneration(board)) return;

        var data = board.Data;
        for (int column = 0; column < board.Width; column++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                GenerateCell(board, column, row);
            }
        }

        ClearInitialMatches(board);

        if (!MatchValidator.HasPossibleMoves(data))
            ReshuffleBoard(board);
    }

    public void CreateSpecialItem(Board board, int column, int row, string specialId)
    {
        ReplaceWithSpecial(board, column, row, specialId);
    }

    public void ReplaceWithSpecial(Board board, int column, int row, string specialId)
    {
        if (board == null || board.Data == null || !board.IsCellActive(column, row))
            return;

        if (string.IsNullOrEmpty(specialId))
            return;

        _specialItemHandler ??= FindObjectOfType<SpecialItemHandler>();
        if (_specialItemHandler == null)
        {
            Debug.LogError("[ItemGenerator] SpecialItemHandler is missing.");
            return;
        }

        var oldItem = board.Items[column, row];
        if (oldItem != null && !string.IsNullOrEmpty(oldItem.SpecialItemId))
            return;

        if (oldItem != null)
        {
            board.Items[column, row] = null;
            board.SetItemId(column, row, "");
            Destroy(oldItem.gameObject);
        }

        board.SetSpecialItemId(column, row, specialId);
        CreateSpecialItemAt(board, column, row, specialId);
    }

    private void GenerateCell(Board board, int column, int row)
    {
        var tile = GetOrCreateTile(board, column, row);
        if (!board.IsCellActive(column, row))
        {
            board.Items[column, row] = null;
            board.SetItemId(column, row, "");
            board.SetSpecialItemId(column, row, "");
            board.Data.SetSpecialCell(column, row, 0);
            var renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            return;
        }

        string specialItemId = board.Data.GetSpecialItem(column, row);
        if (!string.IsNullOrEmpty(specialItemId))
        {
            CreateSpecialItemAt(board, column, row, specialItemId);
            return;
        }

        CreateNormalItemAt(board, column, row, tile);
        CreateSpecialCellAt(board, column, row, tile);
    }

    private void CreateNormalItemAt(Board board, int column, int row, GameObject tile)
    {
        string itemId = board.Data.GetItem(column, row);
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = GetRandomNormalItemId();
            board.SetItemId(column, row, itemId);
        }

        if (string.IsNullOrEmpty(itemId)) return;

        var itemObject = _itemHandler.CreateItem(itemId, board.GetWorldPosition(column, row), tile.transform);
        var item = itemObject != null ? itemObject.GetComponent<Item>() : null;
        if (item == null)
        {
            Debug.LogError($"[ItemGenerator] Failed to create item '{itemId}' at ({column},{row}).");
            return;
        }

        item.name = $"Item({column},{row})";
        item.Column = column;
        item.Row = row;
        item.Board = board;
        item.ItemId = itemId;
        item.SpecialItemId = "";
        board.Items[column, row] = item;
    }

    private void CreateSpecialItemAt(Board board, int column, int row, string specialId)
    {
        _specialItemHandler ??= FindObjectOfType<SpecialItemHandler>();
        if (_specialItemHandler == null) return;

        var tile = GetOrCreateTile(board, column, row);
        var itemObject = _specialItemHandler.CreateSpecialItem(specialId, board.GetWorldPosition(column, row), tile.transform);
        var item = itemObject != null ? itemObject.GetComponent<Item>() : null;
        if (item == null)
        {
            board.SetSpecialItemId(column, row, "");
            Debug.LogError($"[ItemGenerator] Failed to create special item '{specialId}' at ({column},{row}).");
            return;
        }

        item.Column = column;
        item.Row = row;
        item.Board = board;
        item.ItemId = "";
        item.SpecialItemId = specialId;
        item.SnapToPosition(column, row);
        board.Items[column, row] = item;

        var specialItem = item.GetComponent<SpecialItem>();
        specialItem?.SetBoard(board);
        specialItem?.SetGridPosition(column, row);

        CreateSpecialCellAt(board, column, row, tile);
    }

    private void CreateSpecialCellAt(Board board, int column, int row, GameObject tile)
    {
        int typeIndex = board.Data.GetSpecialCell(column, row);
        if (typeIndex <= 0) return;

        _specialCellHandler ??= FindObjectOfType<SpecialCellHandler>();
        if (_specialCellHandler == null) return;

        var cellObject = _specialCellHandler.CreateCell(typeIndex, board.GetWorldPosition(column, row), tile.transform);
        var cell = _specialCellHandler.InitializeCell(cellObject, typeIndex, board, column, row);
        if (cell == null) return;

        board.SetSpecialCell(column, row, cell);
        cell.AttachItem(board.Items[column, row]);
    }

    private void ClearInitialMatches(Board board)
    {
        var matchesHandler = FindObjectOfType<MatchesHandler>();
        if (matchesHandler == null) return;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            var matches = matchesHandler.FindMatches(board);
            if (matches.Count == 0) return;

            foreach (int index in matches)
                ReplaceRandomNormalItem(board, index % board.Width, index / board.Width);
        }
    }

    private void ReplaceRandomNormalItem(Board board, int column, int row)
    {
        var oldItem = board.Items[column, row];
        if (oldItem == null || !string.IsNullOrEmpty(oldItem.SpecialItemId)) return;

        string newId = GetRandomNormalItemId();
        if (string.IsNullOrEmpty(newId)) return;

        var tile = GetOrCreateTile(board, column, row);
        Destroy(oldItem.gameObject);
        board.Items[column, row] = null;
        board.SetItemId(column, row, newId);
        CreateNormalItemAt(board, column, row, tile);

        var cell = board.GetSpecialCell(column, row);
        cell?.AttachItem(board.Items[column, row]);
    }

    public void EnsurePlayableBoard(Board board)
    {
        if (board == null || board.Data == null)
            return;

        if (!MatchValidator.HasPossibleMoves(board.Data))
            ReshuffleBoard(board);
    }

    private void ReshuffleBoard(Board board)
    {
        var data = board.Data;
        var movableItems = new List<string>();

        for (int column = 0; column < board.Width; column++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (!data.IsActive(column, row))
                    continue;

                // Special cells are obstacles/containers. Their occupants must
                // stay where they are and never participate in reshuffling.
                if (data.GetSpecialCell(column, row) > 0)
                    continue;

                var item = board.Items[column, row];
                if (item != null && string.IsNullOrEmpty(item.SpecialItemId))
                    movableItems.Add(item.ItemId);
            }
        }

        if (movableItems.Count < 2)
            return;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            Shuffle(movableItems);
            int itemIndex = 0;

            for (int column = 0; column < board.Width; column++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    if (!data.IsActive(column, row) ||
                        data.GetSpecialCell(column, row) > 0)
                        continue;

                    var item = board.Items[column, row];
                    if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                        continue;

                    string newId = movableItems[itemIndex++];
                    item.ItemId = newId;
                    data.SetItem(column, row, newId);
                    _itemHandler.ApplyVisual(item, newId);
                }
            }

            if (MatchValidator.HasPossibleMoves(data))
                return;
        }

        Debug.LogWarning("[ItemGenerator] Failed to find a reshuffle with a possible move after 100 attempts.");
    }

    private bool ValidateGeneration(Board board)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[ItemGenerator] Not initialized. Call ForceInitialize first.");
            return false;
        }

        if (board == null || board.Data == null)
        {
            Debug.LogError("[ItemGenerator] Board or Board.Data is null.");
            return false;
        }

        if (_tilePrefab == null)
        {
            Debug.LogError("[ItemGenerator] Tile prefab is not assigned.");
            return false;
        }

        return true;
    }

    private GameObject GetOrCreateTile(Board board, int column, int row)
    {
        string tileName = $"Tile({column},{row})";
        var tile = board.transform.Find(tileName);
        if (tile != null) return tile.gameObject;

        var tileObject = Instantiate(_tilePrefab, board.GetWorldPosition(column, row), Quaternion.identity, board.transform);
        tileObject.name = tileName;
        return tileObject;
    }

    private string GetRandomNormalItemId()
    {
        return _availableItemIds.Count == 0
            ? ""
            : _availableItemIds[Random.Range(0, _availableItemIds.Count)];
    }

    private static void Shuffle(List<string> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    public List<GameObject> GetItemPrefabs()
    {
        return _itemHandler != null ? _itemHandler.GetItemPrefabs() : new List<GameObject>();
    }
}
