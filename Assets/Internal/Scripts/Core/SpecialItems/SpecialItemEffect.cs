using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialItemEffect : ScriptableObject
{
    [Header("Effects")]
    public GameObject ActivationEffect;
    public AudioClip ActivationSound;

    [Header("Settings")]
    public bool TriggerOtherSpecialItems = true;

    public abstract void Execute(Board board, int column, int row);

    protected void AddTarget(
        Board board,
        int column,
        int row,
        HashSet<Item> items,
        HashSet<SpecialCell> cells,
        bool ignoreSpecialCells = false)
    {
        if (board == null || !board.IsCellActive(column, row))
            return;

        if (!ignoreSpecialCells)
        {
            var cell = board.GetSpecialCell(column, row);
            if (cell != null && cell.IsDestroyableBySpecial())
            {
                cells.Add(cell);
                return;
            }
        }

        var item = board.Items[column, row];
        if (item != null)
            items.Add(item);
    }

    protected void RemoveTargets(
        Board board,
        HashSet<Item> items,
        HashSet<SpecialCell> cells)
    {
        if (board == null || board.Data == null)
            return;

        DamageNearbyCells(board, items, cells);

        foreach (var item in items)
        {
            if (item == null)
                continue;

            int column = item.Column;
            int row = item.Row;
            var cell = board.GetSpecialCell(column, row);

            if (cell != null && cell.IsDestroyableBySpecial())
            {
                cell.ClearOccupant(item);
                cell.TakeDamage(1);
            }

            if (TriggerOtherSpecialItems && !string.IsNullOrEmpty(item.SpecialItemId))
            {
                var specialItem = item.GetComponent<SpecialItem>();
                if (specialItem != null)
                {
                    board.QueueSpecialItem(specialItem);
                    continue;
                }
            }

            board.SetItemId(column, row, "");
            board.SetSpecialItemId(column, row, "");
            board.Items[column, row] = null;
            Object.Destroy(item.gameObject);
        }

        foreach (var cell in cells)
        {
            if (cell != null && cell.IsDestroyableBySpecial())
                cell.TakeDamage(cell.MaxHealth);
        }
    }

    private void DamageNearbyCells(
        Board board,
        HashSet<Item> items,
        HashSet<SpecialCell> cells)
    {
        var affectedIndices = new HashSet<int>();

        foreach (var item in items)
        {
            if (item != null)
                affectedIndices.Add(board.Data.GetIndex(item.Column, item.Row));
        }

        foreach (var cell in cells)
        {
            if (cell != null)
                affectedIndices.Add(board.Data.GetIndex(cell.Column, cell.Row));
        }

        FindObjectOfType<SpecialCellHandler>()?.DamageAround(board, affectedIndices);
    }
}
