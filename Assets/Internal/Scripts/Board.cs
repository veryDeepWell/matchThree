using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public Item[,] _allItems;
    
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private GameObject[] _dotsPrefabs;
    
    private bool _isProcessing;
    private float _moveDuration = 0.15f;

    void Start()
    {
        _allItems = new Item[width, height];
        Setup();
        StartCoroutine(ClearInitialMatches());
    }

    private void Setup()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
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
                    itemComponent.board = this;
                }
                
                _allItems[i, j] = itemComponent;
            }
        }
    }

    // Очистка начальных комбинаций
    private IEnumerator ClearInitialMatches()
    {
        yield return new WaitForSeconds(0.1f);
        
        bool hasMatches = true;
        int maxAttempts = 100;
        int attempts = 0;
        
        while (hasMatches && attempts < maxAttempts)
        {
            attempts++;
            hasMatches = false;
            
            // Находим все совпадения
            HashSet<Item> matches = FindMatches();
            
            if (matches.Count > 0)
            {
                hasMatches = true;
                
                // Заменяем каждый предмет в совпадении
                foreach (Item item in matches)
                {
                    if (item != null)
                    {
                        ReplaceItem(item);
                    }
                }
                
                // Даем время на обновление
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        Debug.Log($"Initial matches cleared after {attempts} attempts");
    }

    // Замена предмета на новый с другим типом
    private void ReplaceItem(Item oldItem)
    {
        int col = oldItem.column;
        int row = oldItem.row;
        
        // Сохраняем родительский объект (Tile)
        Transform parent = oldItem.transform.parent;
        
        // Удаляем старый предмет
        Destroy(oldItem.gameObject);
        
        // Создаем новый предмет другого типа
        int newDotIndex;
        int attempts = 0;
        do
        {
            newDotIndex = Random.Range(0, _dotsPrefabs.Length);
            attempts++;
        } 
        // Проверяем, что новый тип не создаст комбинацию
        while (attempts < 20 && WillCreateMatch(col, row, (ItemTypes)newDotIndex));
        
        // Создаем новый предмет
        Vector2 pos = new Vector2(col, row);
        GameObject newDot = Instantiate(_dotsPrefabs[newDotIndex], pos, Quaternion.identity, parent);
        newDot.name = "Item(" + col + "," + row + ")";
        
        Item newItem = newDot.GetComponent<Item>();
        if (newItem != null)
        {
            newItem.column = col;
            newItem.row = row;
            newItem.board = this;
        }
        
        _allItems[col, row] = newItem;
    }

    // Проверка, создаст ли новый тип комбинацию
    private bool WillCreateMatch(int col, int row, ItemTypes type)
    {
        // Проверяем горизонталь
        int count = 1;
        
        // Влево
        for (int x = col - 1; x >= 0; x--)
        {
            if (_allItems[x, row] != null && _allItems[x, row]._itemType == type)
                count++;
            else break;
        }
        
        // Вправо
        for (int x = col + 1; x < width; x++)
        {
            if (_allItems[x, row] != null && _allItems[x, row]._itemType == type)
                count++;
            else break;
        }
        
        if (count >= 3) return true;
        
        // Проверяем вертикаль
        count = 1;
        
        // Вниз
        for (int y = row - 1; y >= 0; y--)
        {
            if (_allItems[col, y] != null && _allItems[col, y]._itemType == type)
                count++;
            else break;
        }
        
        // Вверх
        for (int y = row + 1; y < height; y++)
        {
            if (_allItems[col, y] != null && _allItems[col, y]._itemType == type)
                count++;
            else break;
        }
        
        return count >= 3;
    }

    public void CheckMatches()
    {
        if (_isProcessing) return;
        StartCoroutine(ProcessMatches());
    }

    private IEnumerator ProcessMatches()
    {
        _isProcessing = true;
        
        HashSet<Item> itemsToRemove = FindMatches();
        
        if (itemsToRemove.Count == 0)
        {
            _isProcessing = false;
            yield break;
        }

        yield return StartCoroutine(RemoveItems(itemsToRemove));
        yield return StartCoroutine(DropItems());

        _isProcessing = false;
        yield return new WaitForSeconds(0.1f);
        CheckMatches();
    }

    private HashSet<Item> FindMatches()
    {
        HashSet<Item> itemsToRemove = new HashSet<Item>();

        // Горизонтальные совпадения
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                Item current = _allItems[x, y];
                if (current == null) continue;

                if (_allItems[x + 1, y] != null && 
                    _allItems[x + 2, y] != null &&
                    _allItems[x + 1, y]._itemType == current._itemType &&
                    _allItems[x + 2, y]._itemType == current._itemType)
                {
                    int endX = x + 2;
                    while (endX + 1 < width && 
                           _allItems[endX + 1, y] != null && 
                           _allItems[endX + 1, y]._itemType == current._itemType)
                    {
                        endX++;
                    }

                    for (int i = x; i <= endX; i++)
                    {
                        if (_allItems[i, y] != null)
                        {
                            itemsToRemove.Add(_allItems[i, y]);
                        }
                    }
                }
            }
        }

        // Вертикальные совпадения
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                Item current = _allItems[x, y];
                if (current == null) continue;

                if (_allItems[x, y + 1] != null && 
                    _allItems[x, y + 2] != null &&
                    _allItems[x, y + 1]._itemType == current._itemType &&
                    _allItems[x, y + 2]._itemType == current._itemType)
                {
                    int endY = y + 2;
                    while (endY + 1 < height && 
                           _allItems[x, endY + 1] != null && 
                           _allItems[x, endY + 1]._itemType == current._itemType)
                    {
                        endY++;
                    }

                    for (int i = y; i <= endY; i++)
                    {
                        if (_allItems[x, i] != null)
                        {
                            itemsToRemove.Add(_allItems[x, i]);
                        }
                    }
                }
            }
        }

        return itemsToRemove;
    }

    private IEnumerator RemoveItems(HashSet<Item> itemsToRemove)
    {
        foreach (Item item in itemsToRemove)
        {
            if (item != null)
            {
                _allItems[item.column, item.row] = null;
                Destroy(item.gameObject);
            }
        }
        
        yield return new WaitForSeconds(0.05f);
    }

    private IEnumerator DropItems()
    {
        bool hasDropped = false;

        for (int x = 0; x < width; x++)
        {
            int emptySpaces = 0;
            
            for (int y = 0; y < height; y++)
            {
                if (_allItems[x, y] == null)
                {
                    emptySpaces++;
                }
                else if (emptySpaces > 0)
                {
                    Item item = _allItems[x, y];
                    int newY = y - emptySpaces;
                    
                    _allItems[x, y] = null;
                    _allItems[x, newY] = item;
                    
                    item.row = newY;
                    StartCoroutine(item.MoveToPosition(x, newY));
                    hasDropped = true;
                }
            }
        }

        if (hasDropped)
        {
            yield return new WaitForSeconds(_moveDuration + 0.1f);
        }
    }
}