using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    [Header("Prefabs")] [SerializeField] private GameObject _tilePrefab;

    private ItemHandler _itemHandler;
    private List<string> _availableItemIds;
    private bool _isInitialized = false;

    public GameObject GetTilePrefab() => _tilePrefab;

    public void ForceInitialize(ItemHandler handler)
    {
        if (_isInitialized) return;

        _itemHandler = handler;
        if (_itemHandler != null)
        {
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
                Vector2 tilePos = board.GetWorldPosition(x, y); // ← было pos, стало tilePos
                GameObject tile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
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

                GameObject go = _itemHandler.CreateItem(itemId, tilePos, tile.transform);
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

        var oldItem = board.Items[x, y];
        if (oldItem == null) return;

        // Находим или создаём Tile
        Transform tileParent = board.transform.Find($"Tile({x},{y})");
        if (tileParent == null)
        {
            Vector2 tileWorldPos = board.GetWorldPosition(x, y);
            GameObject newTile = Instantiate(_tilePrefab, tileWorldPos, Quaternion.identity, board.transform);
            newTile.name = $"Tile({x},{y})";
            tileParent = newTile.transform;
        }

        // Удаляем старый предмет
        Vector2 itemWorldPos = board.GetWorldPosition(x, y);
        DestroyImmediate(oldItem.gameObject);

        // Создаём новый
        int randomIdx = Random.Range(0, _availableItemIds.Count);
        string newItemId = _availableItemIds[randomIdx];

        data.SetItem(x, y, newItemId);

        GameObject newGo = _itemHandler.CreateItem(newItemId, itemWorldPos, tileParent);
        Item newItem = newGo.GetComponent<Item>();
        newItem.Column = x;
        newItem.Row = y;
        newItem.Board = board;
        newItem.ItemId = newItemId;
        board.Items[x, y] = newItem;
    }

    public void CreateSpecialItem(Board board, int x, int y, string specialId)
    {
        if (board == null || board.Data == null)
        {
            Debug.LogError("[ItemGenerator] Board or Board.Data is null!");
            return;
        }

        if (!board.IsCellActive(x, y))
        {
            Debug.LogWarning($"[ItemGenerator] Cell ({x},{y}) is not active!");
            return;
        }

        // Проверяем, не спец-предмет ли уже
        var oldItem = board.Items[x, y];
        if (oldItem != null && !string.IsNullOrEmpty(oldItem.SpecialItemId))
        {
            Debug.Log($"[ItemGenerator] Cell ({x},{y}) already has special item '{oldItem.SpecialItemId}'");
            return;
        }

        // Находим родительский Tile
        Transform tileParent = null;
        if (oldItem != null)
        {
            tileParent = oldItem.transform.parent;
            board.SetItemId(x, y, specialId);
            board.Items[x, y] = null;
            Destroy(oldItem.gameObject);
        }
        else
        {
            // Если нет старого предмета — ищем тайл по имени или создаём новый
            // В твоей архитектуре тайлы создаются в ItemGenerator.GenerateItems()
            // И они всегда есть, даже если предмета нет
            tileParent = board.transform.Find($"Tile({x},{y})");
            if (tileParent == null)
            {
                // Создаём тайл если не найден
                Vector2 tilePos = board.GetWorldPosition(x, y);
                GameObject tile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, board.transform);
                tile.name = $"Tile({x},{y})";
                tileParent = tile.transform;
            }
        }

        // Создаём специальный предмет через SpecialItemHandler
        var handler = FindObjectOfType<SpecialItemHandler>();
        if (handler == null)
        {
            Debug.LogError("[ItemGenerator] SpecialItemHandler not found!");
            return;
        }

        Vector2 pos = board.GetWorldPosition(x, y);
        GameObject go = handler.CreateSpecialItem(specialId, pos, tileParent); // ← parent = tileParent
        if (go == null)
        {
            Debug.LogError($"[ItemGenerator] Failed to create special item '{specialId}' at ({x},{y})!");
            return;
        }

        var item = go.GetComponent<Item>();
        if (item != null)
        {
            item.Column = x;
            item.Row = y;
            item.Board = board;
            item.ItemId = "";
            item.SpecialItemId = specialId;

            // Принудительно устанавливаем позицию (на случай если CreateSpecialItem не правильно поставил)
            item.transform.position = pos;
            item.SnapToPosition(x, y);

            board.Items[x, y] = item;

            // Убеждаемся что есть коллайдер
            if (go.GetComponent<Collider2D>() == null)
                go.AddComponent<BoxCollider2D>();

            Debug.Log($"[ItemGenerator] Special item '{specialId}' created at ({x},{y})");
        }
        else
        {
            Debug.LogError($"[ItemGenerator] Created special item has no Item component!");
        }
    }

    private void ReshuffleBoard(Board board)
    {
        var data = board.Data;
        int w = data.Width;
        int h = data.Height;

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

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

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

    public void ReplaceWithSpecial(Board board, int x, int y, string specialId)
    {
        if (board == null || board.Data == null)
        {
            Debug.LogError("[ItemGenerator] Board or Board.Data is null!");
            return;
        }

        if (!board.IsCellActive(x, y))
        {
            Debug.LogWarning($"[ItemGenerator] Cell ({x},{y}) is not active!");
            return;
        }

        var oldItem = board.Items[x, y];
        if (oldItem != null && !string.IsNullOrEmpty(oldItem.SpecialItemId))
        {
            Debug.Log($"[ItemGenerator] Cell ({x},{y}) already has special item '{oldItem.SpecialItemId}'");
            return;
        }

        // НАХОДИМ TILE
        Transform tileParent = board.transform.Find($"Tile({x},{y})");
        if (tileParent == null)
        {
            Vector2 tileWorldPos = board.GetWorldPosition(x, y);
            GameObject newTile = Instantiate(_tilePrefab, tileWorldPos, Quaternion.identity, board.transform);
            newTile.name = $"Tile({x},{y})";
            tileParent = newTile.transform;
            Debug.Log($"[ItemGenerator] Created new Tile({x},{y})");
        }

        // Удаляем СТАРЫЙ ПРЕДМЕТ (НЕ TILE)
        if (oldItem != null)
        {
            // Проверяем что oldItem не является Tile
            if (oldItem.gameObject != tileParent.gameObject)
            {
                board.SetItemId(x, y, specialId);
                board.Items[x, y] = null;
                DestroyImmediate(oldItem.gameObject);
                Debug.Log($"[ItemGenerator] Destroyed old item at ({x},{y})");
            }
            else
            {
                Debug.LogError($"[ItemGenerator] Old item is actually a Tile! This shouldn't happen.");
                return;
            }
        }

        // Создаём бомбу
        var specialHandler = FindObjectOfType<SpecialItemHandler>();
        if (specialHandler == null)
        {
            Debug.LogError("[ItemGenerator] SpecialItemHandler not found!");
            return;
        }

        Vector2 bombWorldPos = board.GetWorldPosition(x, y);
        GameObject bombGo = specialHandler.CreateSpecialItem(specialId, bombWorldPos, tileParent);
        if (bombGo == null) return;

        Item bombItem = bombGo.GetComponent<Item>();
        if (bombItem == null)
        {
            bombItem = bombGo.AddComponent<Item>();
        }

        bombItem.Column = x;
        bombItem.Row = y;
        bombItem.Board = board;
        bombItem.ItemId = "";
        bombItem.SpecialItemId = specialId;

        bombGo.transform.position = bombWorldPos;

        if (bombGo.GetComponent<Collider2D>() == null)
        {
            bombGo.AddComponent<BoxCollider2D>();
        }

        board.Items[x, y] = bombItem;

        var specialItemComponent = bombGo.GetComponent<SpecialItem>();
        if (specialItemComponent != null)
        {
            specialItemComponent.SetBoard(board);
            specialItemComponent.SetGridPosition(x, y);
        }

        Debug.Log($"[ItemGenerator] Replaced with special item '{specialId}' at ({x},{y})");
    }
}