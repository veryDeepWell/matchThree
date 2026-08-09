using System.Collections.Generic;
using UnityEngine;

public enum SweeperMode
{
    Horizontal,
    Vertical,
    Cross
}

[CreateAssetMenu(fileName = "LineSweeperEffect", menuName = "Special Effects/Line Sweeper")]
public class LineSweeperEffect : SpecialItemEffect
{
    [Header("Sweeper Settings")]
    [SerializeField] private SweeperMode _mode = SweeperMode.Horizontal;
    [SerializeField] private int _width = 1; // 1 = normal line, 3 = wide (bomb+sweeper combo)

    public SweeperMode Mode => _mode;
    public int Width => _width;

    public static LineSweeperEffect Create(SweeperMode mode, int width = 1)
    {
        var effect = ScriptableObject.CreateInstance<LineSweeperEffect>();
        effect._mode = mode;
        effect._width = Mathf.Max(1, width);
        return effect;
    }

    public override void Execute(Board board, int column, int row)
    {
        if (board == null) return;

        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();

        if (_mode == SweeperMode.Horizontal || _mode == SweeperMode.Cross)
            ClearHorizontal(board, row, column, itemsToRemove, cellsToRemove);

        if (_mode == SweeperMode.Vertical || _mode == SweeperMode.Cross)
            ClearVertical(board, column, row, itemsToRemove, cellsToRemove);

        // Don't destroy the sweeper itself here — SpecialItem.TriggerRoutine does that.
        var self = board.Items[column, row];
        if (self != null)
            itemsToRemove.Remove(self);

        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }

    /// <summary>
    /// Runtime override used by combination logic (bomb+sweeper, dual sweepers, etc.).
    /// </summary>
    public void ExecuteWithMode(Board board, int column, int row, SweeperMode mode, int width)
    {
        if (board == null) return;

        var itemsToRemove = new HashSet<Item>();
        var cellsToRemove = new HashSet<SpecialCell>();

        int savedWidth = _width;
        _width = Mathf.Max(1, width);

        if (mode == SweeperMode.Horizontal || mode == SweeperMode.Cross)
            ClearHorizontal(board, row, column, itemsToRemove, cellsToRemove);

        if (mode == SweeperMode.Vertical || mode == SweeperMode.Cross)
            ClearVertical(board, column, row, itemsToRemove, cellsToRemove);

        _width = savedWidth;

        var self = board.Items[column, row];
        if (self != null)
            itemsToRemove.Remove(self);

        RemoveTargets(board, itemsToRemove, cellsToRemove);
    }

    private void ClearHorizontal(
        Board board,
        int centerRow,
        int originColumn,
        HashSet<Item> items,
        HashSet<SpecialCell> cells)
    {
        int half = _width / 2;
        for (int rowOffset = -half; rowOffset <= half; rowOffset++)
        {
            int y = centerRow + rowOffset;
            for (int x = 0; x < board.Width; x++)
            {
                if (x == originColumn && rowOffset == 0)
                    continue;
                AddTarget(board, x, y, items, cells);
            }
        }
    }

    private void ClearVertical(
        Board board,
        int centerColumn,
        int originRow,
        HashSet<Item> items,
        HashSet<SpecialCell> cells)
    {
        int half = _width / 2;
        for (int colOffset = -half; colOffset <= half; colOffset++)
        {
            int x = centerColumn + colOffset;
            for (int y = 0; y < board.Height; y++)
            {
                if (y == originRow && colOffset == 0)
                    continue;
                AddTarget(board, x, y, items, cells);
            }
        }
    }
}
