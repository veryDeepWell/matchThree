using System;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour, ISpecialItem
{
    private SpecialItemTypes _specialItemType;
    
    private int _explosionDiameter = 3; // Диаметр взрыва
    private int _myColumn;
    private int _myRow;

    private Administrator _administrator;

    private void Start()
    {
        _administrator = FindFirstObjectByType<Administrator>();
        _specialItemType = GetComponent<Item>()._specialType;
    }

    public void CreateSpecialItem(int column, int row)
    {
        _myColumn = column;
        _myRow = row;
    }

    public void TriggerSpecialItem()
    {
        Board board = _administrator.board;
        MatchesHandler matchesHandler = _administrator.matchesHandler;
        HashSet<Item> itemsToRemove = new HashSet<Item>();

        // Get 9 items, including me
        for (int i = _myColumn - Convert.ToInt32(Mathf.Floor(_explosionDiameter / 2f));
             i < _myColumn + Convert.ToInt32(Mathf.Floor(_explosionDiameter / 2f)) + 1;
             i++)
        {
            for (int j = _myRow - Convert.ToInt32(Mathf.Floor(_explosionDiameter / 2f));
                 j < _myRow + Convert.ToInt32(Mathf.Floor(_explosionDiameter / 2f)) + 1;
                 j++)
            {
                if (i >= 0 && i < board.width && j >= 0 && j < board.height)
                {
                    if (board._allItems[i, j] != null)
                    {
                        itemsToRemove.Add(board._allItems[i, j]);
                    }
                }
            }
        }

        matchesHandler.RemoveItems(itemsToRemove);
        matchesHandler.DropItems();
    }
}
