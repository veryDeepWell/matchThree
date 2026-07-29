using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-60)]
public class ItemGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _tilePrefab;
    
    private Administrator _administrator;
    private ItemHandler _itemHandler;
    private SpecialItemHandler _specialItemHandler;
    private Board _board;
    
    private Item[,] _allItems;
    private List<GameObject> _itemPrefabs;
    
    private bool _isInitialized;

    private void Awake()
    {
        _administrator = FindObjectOfType<Administrator>();
        if (_administrator == null)
        {
            Debug.LogError("Administrator not found!");
            return;
        }
        
        _board = _administrator.board;
        _itemHandler = _administrator.itemHandler;
        _specialItemHandler = _administrator.specialItemHandler;
    }
    
    private void Start()
    {
        if (_administrator == null)
        {
            _administrator = FindObjectOfType<Administrator>();
            if (_administrator == null)
            {
                Debug.LogError("Administrator not found in Start!");
                return;
            }
        }
        
        if (_administrator.board == null)
        {
            Debug.LogError("Board is null in Administrator!");
            return;
        }
        
        if (_administrator.itemHandler == null)
        {
            Debug.LogError("ItemHandler is null in Administrator!");
            return;
        }
        
        _board = _administrator.board;
        _allItems = _board.allItems;
        _itemHandler = _administrator.itemHandler;
        _specialItemHandler = _administrator.specialItemHandler;
        
        if (_itemHandler != null)
        {
            _itemPrefabs = _itemHandler.GetItemPrefabs();
        }
        
        _isInitialized = true;
        
        // НЕ вызываем GetItems() здесь - ждем пока Board загрузит уровень
        // GetItems() будет вызван из Board после загрузки уровня
    }

    public void Initialization()
    {
        if (_administrator == null)
        {
            _administrator = FindObjectOfType<Administrator>();
            if (_administrator == null)
            {
                Debug.LogError("Administrator not found in Initialization!");
                return;
            }
        }
        
        _board = _administrator.board;
        if (_board == null)
        {
            Debug.LogError("Board is null in ItemGenerator.Initialization!");
            return;
        }
        
        _allItems = _board.allItems;
        if (_allItems == null)
        {
            Debug.LogError("Board.allItems is null in ItemGenerator.Initialization! Create array first.");
            return;
        }
        
        _itemHandler = _administrator.itemHandler;
        if (_itemHandler == null)
        {
            Debug.LogError("ItemHandler is null in ItemGenerator.Initialization!");
            return;
        }
        
        _specialItemHandler = _administrator.specialItemHandler;
        _itemPrefabs = _itemHandler.GetItemPrefabs();
        
        _isInitialized = true;
    }

    public void GetItems()
    {
        if (!_isInitialized)
        {
            Initialization();
        }
        
        if (_board == null)
        {
            Debug.LogError("Missing board in ItemGenerator.GetItems!");
            return;
        }
        
        if (_allItems == null)
        {
            Debug.LogError("Missing all items array in ItemGenerator.GetItems!");
            return;
        }
        
        if (_itemHandler == null)
        {
            Debug.LogError("Missing item handler in ItemGenerator.GetItems!");
            return;
        }
        
        Setup();
        ClearInitialMatches();
    }

    private void RawdogInitialization()
    {
        _board = FindAnyObjectByType<Board>();
        if (_board == null)
        {
            Debug.LogError("Board not found!");
            return;
        }
        
        _allItems = _board.allItems;
        _itemHandler = FindAnyObjectByType<ItemHandler>();
        _specialItemHandler = FindAnyObjectByType<SpecialItemHandler>();
        _itemPrefabs = _itemHandler?.GetItemPrefabs();
        _isInitialized = true;
    }

    private void Setup()
    {
        if (_board == null || _allItems == null || _itemHandler == null)
        {
            Debug.LogError("Cannot setup items - dependencies missing!");
            return;
        }
        
        for (int x = 0; x < _board.width; x++)
        {
            for (int y = 0; y < _board.height; y++)
            {
                Vector2 tilePos = new Vector2(x, y);

                GameObject newTile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
                newTile.name = "Tile(" + x + "," + y + ")";
                
                if (!_board.IsActiveCell(x, y))
                {
                    _allItems[x, y] = null;
                    SpriteRenderer tileSr = newTile.GetComponent<SpriteRenderer>();
                    if (tileSr != null)
                        tileSr.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    continue;
                }

                // Если в LevelData уже есть предмет - используем его
                ItemTypes itemType;
                if (_board.currentLevel != null)
                {
                    itemType = _board.currentLevel.GetItem(x, y);
                    if (itemType == ItemTypes.None || itemType == ItemTypes.Special)
                    {
                        int dotToUse = Random.Range(0, _itemPrefabs.Count);
                        itemType = _itemPrefabs[dotToUse].GetComponent<Item>().itemType;
                    }
                }
                else
                {
                    int dotToUse = Random.Range(0, _itemPrefabs.Count);
                    itemType = _itemPrefabs[dotToUse].GetComponent<Item>().itemType;
                }
                
                GameObject newDot = _itemHandler.CreateItem(itemType, tilePos, newTile.transform);
                newDot.name = "Item(" + x + "," + y + ")";

                Item itemComponent = newDot.GetComponent<Item>();
                if (itemComponent != null)
                {
                    itemComponent.column = x;
                    itemComponent.row = y;
                    itemComponent.board = _board;
                }

                _allItems[x, y] = itemComponent;
            }
        }
    }

    private void ClearInitialMatches()
    {
        if (_administrator?.matchesHandler == null) return;
        
        bool hasMatches = true;
        int maxAttempts = 100;
        int attempts = 0;

        while (hasMatches && attempts < maxAttempts)
        {
            attempts++;
            hasMatches = false;

            _administrator.matchesHandler.VariablesEstablishment();
            HashSet<Item> matches = _administrator.matchesHandler.FindMatches();

            if (matches.Count > 0)
            {
                hasMatches = true;
                foreach (Item item in matches)
                {
                    if (item != null)
                        ReplaceItem(item);
                }
            }
        }

        if (attempts > 1)
            Debug.Log($"Initial matches cleared after {attempts} attempts");
    }

    private void ReplaceItem(Item oldItem)
    {
        if (_board == null || _allItems == null || _itemHandler == null) return;
        
        int col = oldItem.column;
        int row = oldItem.row;

        if (!_board.IsActiveCell(col, row)) return;

        Transform parent = oldItem.transform.parent;
        DestroyImmediate(oldItem.gameObject);

        int newDotIndex;
        int attempts = 0;
        do
        {
            newDotIndex = Random.Range(0, _itemPrefabs.Count);
            attempts++;
        } while (attempts < 20 && WillCreateMatch(col, row, (ItemTypes)newDotIndex));

        Vector2 pos = new Vector2(col, row);
        Item prefabItem = _itemPrefabs[newDotIndex].GetComponent<Item>();
        GameObject newDot = _itemHandler.CreateItem(prefabItem.itemType, pos, parent);
        newDot.name = "Item(" + col + "," + row + ")";

        Item newItem = newDot.GetComponent<Item>();
        if (newItem != null)
        {
            newItem.column = col;
            newItem.row = row;
            newItem.board = _board;
        }

        _allItems[col, row] = newItem;
    }

    private bool WillCreateMatch(int col, int row, ItemTypes type)
    {
        if (_board == null || _allItems == null) return false;
        if (!_board.IsActiveCell(col, row)) return false;

        int count = 1;

        for (int x = col - 1; x >= 0; x--)
        {
            if (_board.IsActiveCell(x, row) && _allItems[x, row] != null && _allItems[x, row].itemType == type)
                count++;
            else break;
        }

        for (int x = col + 1; x < _board.width; x++)
        {
            if (_board.IsActiveCell(x, row) && _allItems[x, row] != null && _allItems[x, row].itemType == type)
                count++;
            else break;
        }

        if (count >= 3) return true;

        count = 1;

        for (int y = row - 1; y >= 0; y--)
        {
            if (_board.IsActiveCell(col, y) && _allItems[col, y] != null && _allItems[col, y].itemType == type)
                count++;
            else break;
        }

        for (int y = row + 1; y < _board.height; y++)
        {
            if (_board.IsActiveCell(col, y) && _allItems[col, y] != null && _allItems[col, y].itemType == type)
                count++;
            else break;
        }

        return count >= 3;
    }

    public void CreateSpecialItem(SpecialItemTypes specialType, int column, int row)
    {
        if (_board == null || _allItems == null || _specialItemHandler == null) return;
        if (!_board.IsActiveCell(column, row)) return;

        Item oldItem = _allItems[column, row];
        if (oldItem != null)
            DestroyImmediate(oldItem.gameObject);

        Vector2 pos = new Vector2(column, row);
        Transform parent = _board.transform;

        GameObject newDot = _specialItemHandler.CreateSpecialItem(specialType, pos, parent);
        newDot.name = "Special(" + specialType + ")";

        Item itemComponent = newDot.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.column = column;
            itemComponent.row = row;
            itemComponent.board = _board;
        }

        _allItems[column, row] = itemComponent;
    }
}