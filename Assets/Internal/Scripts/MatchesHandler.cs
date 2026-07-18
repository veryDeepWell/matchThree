using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    private float _moveDuration = 0.15f;
    
    public HashSet<Item> FindMatches(Item[,] _allItems, int width, int height)
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

    private void ProcessMatches()
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