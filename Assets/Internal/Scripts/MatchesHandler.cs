using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    private Administrator _administrator;
    private Item[,] _allItems;
    private Board _board;
    private int _height;
    private bool _isProcessing = false;

    private float _moveDuration = 0.15f;

    private int _width;

    private void Start()
    {
        _administrator = FindAnyObjectByType<Administrator>().GetComponent<Administrator>();
        _board = _administrator.board;

        VariablesEstablishment();
    }

    public void VariablesEstablishment()
    {
        if (_board != null)
        {
            _width = _board.width;
            _height = _board.height;
            _allItems = _board._allItems;
        }
    }

    public HashSet<Item> FindMatches()
    {
        if (_allItems == null || _allItems.Length == 0)
        {
            VariablesEstablishment();
        }

        HashSet<Item> itemsToRemove = new HashSet<Item>();

        // Горизонтальные совпадения
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width - 2; x++)
            {
                Item current = _allItems[x, y];
                if (current == null) continue;

                if (_allItems[x + 1, y] != null && _allItems[x + 2, y] != null &&
                    _allItems[x + 1, y]._itemType == current._itemType &&
                    _allItems[x + 2, y]._itemType == current._itemType)
                {
                    int endX = x + 2;
                    while (endX + 1 < _width &&
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
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height - 2; y++)
            {
                Item current = _allItems[x, y];
                if (current == null) continue;

                if (_allItems[x, y + 1] != null && _allItems[x, y + 2] != null &&
                    _allItems[x, y + 1]._itemType == current._itemType &&
                    _allItems[x, y + 2]._itemType == current._itemType)
                {
                    int endY = y + 2;
                    while (endY + 1 < _height &&
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

    public void ProcessMatches()
    {
        if (_isProcessing) return;
        StartCoroutine(ProcessMatchesCoroutine());
    }

    private IEnumerator ProcessMatchesCoroutine()
    {
        _isProcessing = true;

        HashSet<Item> itemsToRemove = FindMatches();

        if (itemsToRemove.Count == 0)
        {
            _isProcessing = false;
            yield break;
        }

        RemoveItems(itemsToRemove);
        yield return new WaitForSeconds(0.05f);

        DropItems();
        yield return new WaitForSeconds(_moveDuration + 0.1f);

        _isProcessing = false;

        _board.CheckMatches();
    }

    public void RemoveItems(HashSet<Item> itemsToRemove)
    {
        foreach (Item item in itemsToRemove)
        {
            if (item != null)
            {
                _allItems[item.column, item.row] = null;
                Destroy(item.gameObject);
            }
        }
    }

    public void DropItems()
    {
        bool hasDropped = false;

        for (int x = 0; x < _width; x++)
        {
            int emptySpaces = 0;

            for (int y = 0; y < _height; y++)
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
                    _board.StartCoroutine(item.MoveToPosition(x, newY));
                    hasDropped = true;
                }
            }
        }
    }

    public void UpdateReferences()
    {
        if (_administrator == null)
        {
            _administrator = FindAnyObjectByType<Administrator>();
        }

        if (_board == null)
        {
            _board = _administrator.board;
        }

        VariablesEstablishment();
    }

    public void SpecialItemCreation(int column, int row)
    {
        Item itemToCreate = _allItems[column, row];
    }
}