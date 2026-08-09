using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagnetEffect", menuName = "Special Effects/Magnet")]
public class MagnetEffect : SpecialItemEffect
{
    public override void Execute(Board board, int column, int row)
    {
        if (board == null || board.Data == null) return;

        // Magnet alone does nothing useful without a colour — it is meant to be
        // activated by swapping with a normal coloured item.  When triggered in
        // isolation (e.g. cascade) pick a random normal colour present on the board.
        string targetColor = FindRandomNormalColor(board);
        if (string.IsNullOrEmpty(targetColor))
            return;

        ClearColor(board, column, row, targetColor);
    }

    /// <summary>
    /// Called when the magnet is swapped with a coloured item.
    /// </summary>
    public void ExecuteWithColor(Board board, int column, int row, string colorId)
    {
        if (board == null || string.IsNullOrEmpty(colorId)) return;
        ClearColor(board, column, row, colorId);
    }

    private void ClearColor(Board board, int originColumn, int originRow, string colorId)
    {
        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (x == originColumn && y == originRow)
                    continue;

                var item = board.Items[x, y];
                if (item == null)
                    continue;

                // Only normal items of the target colour (not other specials).
                if (!string.IsNullOrEmpty(item.SpecialItemId))
                    continue;

                if (item.ItemId == colorId)
                    AddTarget(board, x, y, itemsToRemove, cellsToRemove);
            }
        }

        var self = board.Items[originColumn, originRow];
        if (self != null)
            itemsToRemove.Remove(self);

        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }

    private static string FindRandomNormalColor(Board board)
    {
        var colors = new List<string>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var item = board.Items[x, y];
                if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                    continue;
                if (!string.IsNullOrEmpty(item.ItemId) && !colors.Contains(item.ItemId))
                    colors.Add(item.ItemId);
            }
        }

        if (colors.Count == 0)
            return null;

        return colors[Random.Range(0, colors.Count)];
    }
}
