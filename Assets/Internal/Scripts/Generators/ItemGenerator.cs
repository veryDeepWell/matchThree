using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _tilePrefab;

    private ItemHandler _itemHandler;
    private List<string> _availableItemIds;
    private bool _isInitialized = false;

    public void ForceInitialize(ItemHandler handler)
    {
        if (_isInitialized) return;

        _itemHandler = handler;
        if (_itemHandler != null)
        {
            // Загружаем реестр не через Resources, а через ItemHandler
            var registry = _itemHandler.GetRegistry();
            if (registry != null)
            {
                _availableItemIds = new List<string>();
                foreach (var def in registry.GetNormalItems())
                {
                    if (def != null && !string.IsNullOrEmpty(def.Id))
                        _availableItemIds.Add(def.Id);
                }
                _isInitialized = true;
                Debug.Log($"[ItemGenerator] Initialized with {_availableItemIds?.Count ?? 0} item types");
            }
            else
            {
                Debug.LogError("[ItemGenerator] Registry not found in ItemHandler!");
            }
        }
        else
        {
            Debug.LogError("[ItemGenerator] ItemHandler is null!");
        }
    }

    public void GenerateItems(Board board)
{
    if (!_isInitialized)
    {
        Debug.LogError("[ItemGenerator] Not initialized! Call ForceInitialize first.");
        return;
    }

    if (board?.Data == null)
    {
        Debug.LogError("[ItemGenerator] Board or Board.Data is null!");
        return;
    }

    if (_availableItemIds == null || _availableItemIds.Count == 0)
    {
        Debug.LogError("[ItemGenerator] No item types available!");
        return;
    }

    var data = board.Data;
    int w = data.Width;
    int h = data.Height;

    for (int x = 0; x < w; x++)
    {
        for (int y = 0; y < h; y++)
        {
            Vector2 pos = board.GetWorldPosition(x, y);
            GameObject tile = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
            tile.name = $"Tile({x},{y})";

            if (!data.IsActive(x, y))
            {
                board.Items[x, y] = null;
                var sr = tile.GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                continue;
            }

            string itemId = data.GetItem(x, y);
            if (string.IsNullOrEmpty(itemId))
            {
                int idx = Random.Range(0, _availableItemIds.Count);
                itemId = _availableItemIds[idx];
                data.SetItem(x, y, itemId);
            }

            GameObject go = _itemHandler.CreateItem(itemId, pos, tile.transform);
            if (go == null)
            {
                Debug.LogError($"ItemGenerator: Failed to create item '{itemId}' at ({x},{y})");
                board.Items[x, y] = null;
                continue;
            }
            
            go.name = $"Item({x},{y})";

            var item = go.GetComponent<Item>();
            if (item)
            {
                item.Column = x;
                item.Row = y;
                item.Board = board;
                item.ItemId = itemId;
            }

            board.Items[x, y] = item;
        }
    }

    ClearInitialMatches(board);

    if (!MatchValidator.HasPossibleMoves(data))
    {
        Debug.Log("No possible moves! Reshuffling...");
        ReshuffleBoard(board);
    }
}

    private void ClearInitialMatches(Board board)
    {
        bool hasMatches = true;
        int attempts = 0;
        var handler = FindObjectOfType<MatchesHandler>();

        while (hasMatches && attempts < 100)
        {
            attempts++;
            hasMatches = false;
            var matches = handler?.FindMatches(board);

            if (matches != null && matches.Count > 0)
            {
                hasMatches = true;
                foreach (int idx in matches)
                {
                    int x = idx % board.Data.Width;
                    int y = idx / board.Data.Width;
                    ReplaceItem(board, x, y);
                }
            }
        }
    }

    private void ReplaceItem(Board board, int x, int y)
    {
        var data = board.Data;
        int idx = data.GetIndex(x, y);
        if (!data.ActiveCells[idx]) return;

        var old = board.Items[x, y];
        if (old)
        {
            var parent = old.transform.parent;
            Vector2 pos = board.GetWorldPosition(x, y);
            DestroyImmediate(old.gameObject);

            int randomIdx = Random.Range(0, _availableItemIds.Count);
            string newId = _availableItemIds[randomIdx];

            data.SetItem(x, y, newId);

            var go = _itemHandler.CreateItem(newId, pos, parent);
            var item = go.GetComponent<Item>();
            item.Column = x;
            item.Row = y;
            item.Board = board;
            item.ItemId = newId;
            board.Items[x, y] = item;
        }
    }

    private void ReshuffleBoard(Board board)
    {
        var data = board.Data;
        int w = data.Width;
        int h = data.Height;

        // Сохраняем специальные предметы и ячейки
        var specialItems = new Dictionary<int, string>();
        var specialCells = new Dictionary<int, SpecialCell>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;

                string specialId = data.GetSpecialItem(x, y);
                if (!string.IsNullOrEmpty(specialId))
                    specialItems[idx] = specialId;

                var cell = board.GetSpecialCell(x, y);
                if (cell != null)
                    specialCells[idx] = cell;
            }
        }

        // Собираем все обычные предметы в список
        List<string> items = new List<string>();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;
                if (specialItems.ContainsKey(idx)) continue;
                if (specialCells.ContainsKey(idx)) continue;

                string id = data.GetItem(x, y);
                if (!string.IsNullOrEmpty(id))
                    items.Add(id);
            }
        }

        // Перемешиваем
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        // Раскладываем обратно
        int index = 0;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;
                if (specialItems.ContainsKey(idx)) continue;
                if (specialCells.ContainsKey(idx)) continue;

                if (index < items.Count)
                {
                    string newId = items[index];
                    data.SetItem(x, y, newId);

                    var item = board.Items[x, y];
                    if (item != null)
                    {
                        item.ItemId = newId;
                        var handler = FindObjectOfType<ItemHandler>();
                        if (handler != null)
                        {
                            var sr = item.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                var sprite = handler.GetSprite(newId);
                                if (sprite != null) sr.sprite = sprite;
                            }
                        }
                    }
                    index++;
                }
            }
        }

        if (!MatchValidator.HasPossibleMoves(data))
        {
            ReshuffleBoard(board);
        }
    }

    public List<GameObject> GetItemPrefabs()
    {
        return _itemHandler != null ? _itemHandler.GetItemPrefabs() : new List<GameObject>();
    }
}