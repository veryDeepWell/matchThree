using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-60)]
public class ItemGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private ItemHandler _itemHandler;

    private List<GameObject> _itemPrefabs;

    private void Start()
    {
        if (_itemHandler == null) _itemHandler = FindObjectOfType<ItemHandler>();
        _itemPrefabs = _itemHandler?.GetItemPrefabs();
    }

    public void GenerateItems(Board board)
    {
        if (board?.Data == null || _itemHandler == null) return;

        var data = board.Data;
        int w = data.Width;
        int h = data.Height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector2 pos = new Vector2(x, y);
                GameObject tile = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
                tile.name = $"Tile({x},{y})";

                if (!data.IsActive(x, y))
                {
                    board.Items[x, y] = null;
                    var sr = tile.GetComponent<SpriteRenderer>();
                    if (sr) sr.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    continue;
                }

                ItemTypes type = data.GetItem(x, y);
                if (type == ItemTypes.None)
                {
                    int idx = Random.Range(0, _itemPrefabs.Count);
                    type = _itemPrefabs[idx].GetComponent<Item>().ItemType;
                    data.SetItem(x, y, type);
                }

                GameObject go = _itemHandler.CreateItem(type, pos, tile.transform);
                go.name = $"Item({x},{y})";

                var item = go.GetComponent<Item>();
                if (item)
                {
                    item.Column = x;
                    item.Row = y;
                    item.Board = board;
                    item.ItemType = type;
                }

                board.Items[x, y] = item;
            }
        }

        ClearInitialMatches(board);
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
            DestroyImmediate(old.gameObject);

            int prefabIdx = Random.Range(0, _itemPrefabs.Count);
            var type = _itemPrefabs[prefabIdx].GetComponent<Item>().ItemType;

            data.SetItem(x, y, type);

            Vector2 pos = new Vector2(x, y);
            var go = _itemHandler.CreateItem(type, pos, parent);
            var item = go.GetComponent<Item>();
            item.Column = x;
            item.Row = y;
            item.Board = board;
            item.ItemType = type;
            board.Items[x, y] = item;
        }
    }
}