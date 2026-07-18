using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemGenerator : MonoBehaviour
{
    private Administrator _administrator;
    
    private Board _board;
    private Item[,] _allItems;
    private bool _isInitialized;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject[] _dotsPrefabs;

    private void Start()
    {
        _administrator = FindAnyObjectByType<Administrator>().GetComponent<Administrator>();
    }

    public void Initialization(Item[,] itemsArray)
    {
        _allItems = itemsArray;
        _board = _administrator.board;
        _isInitialized = true;
    }
    
    public void GetItems()
    {
        if (!_isInitialized) { RawdogInitialization(); }

        Setup();
        ClearInitialMatches();
    }

    private void RawdogInitialization()
    {
        _board = FindAnyObjectByType<Board>();
        _allItems = _board._allItems;
        _isInitialized = true;
    }
    
    private void Setup()
    {
        for (int i = 0; i < _board.width; i++)
        {
            for (int j = 0; j < _board.height; j++)
            {
                Vector2 tilePos = new Vector2(i, j);
                
                GameObject newTile = Instantiate(_tilePrefab, tilePos, Quaternion.identity, transform);
                newTile.name = "Tile(" + i + "," + j + ")";
                
                int dotToUse = Random.Range(0, _dotsPrefabs.Length);
                GameObject newDot = Instantiate(_dotsPrefabs[dotToUse], tilePos, Quaternion.identity, newTile.transform);
                newDot.name = "Item(" + i + "," + j + ")";
                
                Item itemComponent = newDot.GetComponent<Item>();
                if (itemComponent != null)
                {
                    itemComponent.column = i;
                    itemComponent.row = j;
                    itemComponent.board = _board;
                    itemComponent._itemType = (ItemTypes)dotToUse;
                }
                
                _allItems[i, j] = itemComponent;
            }
        }
    }

    private void ClearInitialMatches()
    {
        bool hasMatches = true;
        int maxAttempts = 100;
        int attempts = 0;
        
        while (hasMatches && attempts < maxAttempts)
        {
            attempts++;
            hasMatches = false;
            
            // Обновляем ссылку на массив в MatchesHandler
            _administrator.matchesHandler.VariablesEstablishment();
            
            HashSet<Item> matches = _administrator.matchesHandler.FindMatches();
            
            if (matches.Count > 0)
            {
                hasMatches = true;
                
                foreach (Item item in matches)
                {
                    if (item != null)
                    {
                        ReplaceItem(item);
                    }
                }
            }
        }
        
        Debug.Log($"Initial matches cleared after {attempts} attempts");
    }

    private void ReplaceItem(Item oldItem)
    {
        int col = oldItem.column;
        int row = oldItem.row;
        
        Transform parent = oldItem.transform.parent;
        
        // Используем DestroyImmediate для синхронного удаления
        DestroyImmediate(oldItem.gameObject);
        
        int newDotIndex;
        int attempts = 0;
        do
        {
            newDotIndex = Random.Range(0, _dotsPrefabs.Length);
            attempts++;
        } 
        while (attempts < 20 && WillCreateMatch(col, row, (ItemTypes)newDotIndex));
        
        Vector2 pos = new Vector2(col, row);
        GameObject newDot = Instantiate(_dotsPrefabs[newDotIndex], pos, Quaternion.identity, parent);
        newDot.name = "Item(" + col + "," + row + ")";
        
        Item newItem = newDot.GetComponent<Item>();
        if (newItem != null)
        {
            newItem.column = col;
            newItem.row = row;
            newItem.board = _board;
            newItem._itemType = (ItemTypes)newDotIndex;
        }
        
        _allItems[col, row] = newItem;
    }

    private bool WillCreateMatch(int col, int row, ItemTypes type)
    {
        int count = 1;
        
        for (int x = col - 1; x >= 0; x--)
        {
            if (_allItems[x, row] != null && _allItems[x, row]._itemType == type)
                count++;
            else break;
        }
        
        for (int x = col + 1; x < _board.width; x++)
        {
            if (_allItems[x, row] != null && _allItems[x, row]._itemType == type)
                count++;
            else break;
        }
        
        if (count >= 3) return true;
        
        count = 1;
        
        for (int y = row - 1; y >= 0; y--)
        {
            if (_allItems[col, y] != null && _allItems[col, y]._itemType == type)
                count++;
            else break;
        }
        
        for (int y = row + 1; y < _board.height; y++)
        {
            if (_allItems[col, y] != null && _allItems[col, y]._itemType == type)
                count++;
            else break;
        }
        
        return count >= 3;
    }
}