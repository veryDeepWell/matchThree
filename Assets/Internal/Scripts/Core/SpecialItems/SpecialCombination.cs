using UnityEngine;

/// <summary>
/// Resolves the result of swapping two special items (or a special + normal colour).
/// Returns true if a combination was handled (caller should not fall back to
/// independent triggers).
/// </summary>
public static class SpecialCombination
{
    public static bool TryResolve(
        Board board,
        Item itemA,
        Item itemB,
        int colA, int rowA,
        int colB, int rowB)
    {
        if (board == null || itemA == null || itemB == null)
            return false;

        string idA = itemA.SpecialItemId ?? "";
        string idB = itemB.SpecialItemId ?? "";

        bool aSpecial = !string.IsNullOrEmpty(idA);
        bool bSpecial = !string.IsNullOrEmpty(idB);

        // Magnet + normal colour
        if (idA == "magnet" && !bSpecial && !string.IsNullOrEmpty(itemB.ItemId))
        {
            TriggerMagnetWithColor(board, itemA, colA, rowA, itemB.ItemId);
            DestroyItem(board, itemB, colB, rowB);
            return true;
        }

        if (idB == "magnet" && !aSpecial && !string.IsNullOrEmpty(itemA.ItemId))
        {
            TriggerMagnetWithColor(board, itemB, colB, rowB, itemA.ItemId);
            DestroyItem(board, itemA, colA, rowA);
            return true;
        }

        // Two specials
        if (!aSpecial || !bSpecial)
            return false;

        // Bomb + Bomb → bigger explosion
        if (idA == "bomb" && idB == "bomb")
        {
            TriggerDoubleBomb(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        // Bomb + Sweeper → wide sweeper of that type
        if (IsSweeper(idA) && idB == "bomb")
        {
            TriggerWideSweeper(board, itemA, colA, rowA, idA, itemB, colB, rowB);
            return true;
        }

        if (IsSweeper(idB) && idA == "bomb")
        {
            TriggerWideSweeper(board, itemB, colB, rowB, idB, itemA, colA, rowA);
            return true;
        }

        // Horizontal + Vertical sweeper → cross
        if ((idA == "sweeper_h" && idB == "sweeper_v") ||
            (idA == "sweeper_v" && idB == "sweeper_h"))
        {
            TriggerCrossAt(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        // Horizontal + Horizontal → wide horizontal
        if (idA == "sweeper_h" && idB == "sweeper_h")
        {
            TriggerWideSweeper(board, itemA, colA, rowA, "sweeper_h", itemB, colB, rowB);
            return true;
        }

        // Vertical + Vertical → wide vertical
        if (idA == "sweeper_v" && idB == "sweeper_v")
        {
            TriggerWideSweeper(board, itemA, colA, rowA, "sweeper_v", itemB, colB, rowB);
            return true;
        }

        // Cross + Horizontal → wide H + normal V
        if ((idA == "sweeper_cross" && idB == "sweeper_h") ||
            (idA == "sweeper_h" && idB == "sweeper_cross"))
        {
            TriggerCrossWideHorizontal(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        // Cross + Vertical → wide V + normal H
        if ((idA == "sweeper_cross" && idB == "sweeper_v") ||
            (idA == "sweeper_v" && idB == "sweeper_cross"))
        {
            TriggerCrossWideVertical(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        // Cross + Cross → wide cross
        if (idA == "sweeper_cross" && idB == "sweeper_cross")
        {
            TriggerWideCross(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        // Magnet + Bomb → pick random colour, explode bomb on a random item of that colour? 
        // Spec: "выбирает случайный обычный предмет и триггерит на нем бомбу"
        if (idA == "magnet" && idB == "bomb")
        {
            TriggerMagnetBomb(board, itemA, colA, rowA, itemB, colB, rowB);
            return true;
        }

        if (idB == "magnet" && idA == "bomb")
        {
            TriggerMagnetBomb(board, itemB, colB, rowB, itemA, colA, rowA);
            return true;
        }

        // Magnet + Sweeper → pick random normal item and trigger that sweeper type on it
        if (idA == "magnet" && IsSweeper(idB))
        {
            TriggerMagnetSweeper(board, itemA, colA, rowA, itemB, colB, rowB, idB);
            return true;
        }

        if (idB == "magnet" && IsSweeper(idA))
        {
            TriggerMagnetSweeper(board, itemB, colB, rowB, itemA, colA, rowA, idA);
            return true;
        }

        // Fallback: trigger both independently
        return false;
    }

    private static bool IsSweeper(string id) =>
        id == "sweeper_h" || id == "sweeper_v" || id == "sweeper_cross";

    private static SweeperMode ModeFromId(string id)
    {
        switch (id)
        {
            case "sweeper_h": return SweeperMode.Horizontal;
            case "sweeper_v": return SweeperMode.Vertical;
            default: return SweeperMode.Cross;
        }
    }

    private static void DestroyItem(Board board, Item item, int column, int row)
    {
        if (item == null) return;
        board.GetSpecialCell(column, row)?.ClearOccupant(item);
        board.SetItemId(column, row, "");
        board.SetSpecialItemId(column, row, "");
        board.Items[column, row] = null;
        Object.Destroy(item.gameObject);
    }

    private static void TriggerMagnetWithColor(
        Board board, Item magnet, int col, int row, string colorId)
    {
        var effect = magnet.GetComponent<SpecialItem>()?.Effect as MagnetEffect;
        if (effect != null)
            effect.ExecuteWithColor(board, col, row, colorId);
        else
            magnet.GetComponent<ISpecialItem>()?.TriggerSpecialItem();

        DestroyItem(board, magnet, col, row);
    }

    private static void TriggerDoubleBomb(
        Board board, Item a, int colA, int rowA, Item b, int colB, int rowB)
    {
        // Bigger radius explosion centred on the midpoint / first bomb.
        var items = new System.Collections.Generic.HashSet<Item>();
        var cells = new System.Collections.Generic.HashSet<SpecialCell>();
        int radius = 2;

        for (int x = colA - radius; x <= colA + radius; x++)
        {
            for (int y = rowA - radius; y <= rowA + radius; y++)
            {
                if (!board.IsCellActive(x, y)) continue;
                var cell = board.GetSpecialCell(x, y);
                if (cell != null && cell.IsDestroyableBySpecial())
                    cells.Add(cell);
                else if (board.Items[x, y] != null)
                    items.Add(board.Items[x, y]);
            }
        }

        items.Remove(a);
        items.Remove(b);

        // Reuse BombEffect's RemoveTargets via a temporary helper
        ExecuteRemove(board, items, cells);
        DestroyItem(board, a, colA, rowA);
        DestroyItem(board, b, colB, rowB);
    }

    private static void TriggerWideSweeper(
        Board board, Item sweeper, int col, int row, string sweeperId,
        Item other, int otherCol, int otherRow)
    {
        var effect = sweeper.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        if (effect != null)
            effect.ExecuteWithMode(board, col, row, ModeFromId(sweeperId), width: 3);
        else
            sweeper.GetComponent<ISpecialItem>()?.TriggerSpecialItem();

        DestroyItem(board, sweeper, col, row);
        DestroyItem(board, other, otherCol, otherRow);
    }

    private static void TriggerCrossAt(
        Board board, Item a, int colA, int rowA, Item b, int colB, int rowB)
    {
        var effect = a.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        if (effect != null)
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Cross, width: 1);
        else
            a.GetComponent<ISpecialItem>()?.TriggerSpecialItem();

        DestroyItem(board, a, colA, rowA);
        DestroyItem(board, b, colB, rowB);
    }

    private static void TriggerCrossWideHorizontal(
        Board board, Item a, int colA, int rowA, Item b, int colB, int rowB)
    {
        // Wide horizontal + normal vertical
        var effect = a.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        if (effect != null)
        {
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Horizontal, width: 3);
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Vertical, width: 1);
        }

        DestroyItem(board, a, colA, rowA);
        DestroyItem(board, b, colB, rowB);
    }

    private static void TriggerCrossWideVertical(
        Board board, Item a, int colA, int rowA, Item b, int colB, int rowB)
    {
        var effect = a.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        if (effect != null)
        {
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Vertical, width: 3);
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Horizontal, width: 1);
        }

        DestroyItem(board, a, colA, rowA);
        DestroyItem(board, b, colB, rowB);
    }

    private static void TriggerWideCross(
        Board board, Item a, int colA, int rowA, Item b, int colB, int rowB)
    {
        var effect = a.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        if (effect != null)
            effect.ExecuteWithMode(board, colA, rowA, SweeperMode.Cross, width: 3);

        DestroyItem(board, a, colA, rowA);
        DestroyItem(board, b, colB, rowB);
    }

    private static void TriggerMagnetBomb(
        Board board, Item magnet, int mCol, int mRow,
        Item bomb, int bCol, int bRow)
    {
        // Pick a random normal item and run a bomb explosion there.
        var target = FindRandomNormalItem(board, magnet, bomb);
        DestroyItem(board, magnet, mCol, mRow);
        DestroyItem(board, bomb, bCol, bRow);

        if (target == null) return;

        int tc = target.Column;
        int tr = target.Row;
        // Simulate bomb at that position
        var items = new System.Collections.Generic.HashSet<Item>();
        var cells = new System.Collections.Generic.HashSet<SpecialCell>();
        int radius = 1;
        for (int x = tc - radius; x <= tc + radius; x++)
        {
            for (int y = tr - radius; y <= tr + radius; y++)
            {
                if (!board.IsCellActive(x, y)) continue;
                var cell = board.GetSpecialCell(x, y);
                if (cell != null && cell.IsDestroyableBySpecial())
                    cells.Add(cell);
                else if (board.Items[x, y] != null)
                    items.Add(board.Items[x, y]);
            }
        }
        ExecuteRemove(board, items, cells);
    }

    private static void TriggerMagnetSweeper(
        Board board, Item magnet, int mCol, int mRow,
        Item sweeper, int sCol, int sRow, string sweeperId)
    {
        var target = FindRandomNormalItem(board, magnet, sweeper);
        DestroyItem(board, magnet, mCol, mRow);
        DestroyItem(board, sweeper, sCol, sRow);

        if (target == null) return;

        var effect = sweeper.GetComponent<SpecialItem>()?.Effect as LineSweeperEffect;
        // Effect may already be destroyed with the sweeper object; create logic inline.
        var items = new System.Collections.Generic.HashSet<Item>();
        var cells = new System.Collections.Generic.HashSet<SpecialCell>();
        int tc = target.Column;
        int tr = target.Row;
        var mode = ModeFromId(sweeperId);

        if (mode == SweeperMode.Horizontal || mode == SweeperMode.Cross)
        {
            for (int x = 0; x < board.Width; x++)
            {
                if (!board.IsCellActive(x, tr)) continue;
                var cell = board.GetSpecialCell(x, tr);
                if (cell != null && cell.IsDestroyableBySpecial()) cells.Add(cell);
                else if (board.Items[x, tr] != null) items.Add(board.Items[x, tr]);
            }
        }

        if (mode == SweeperMode.Vertical || mode == SweeperMode.Cross)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (!board.IsCellActive(tc, y)) continue;
                var cell = board.GetSpecialCell(tc, y);
                if (cell != null && cell.IsDestroyableBySpecial()) cells.Add(cell);
                else if (board.Items[tc, y] != null) items.Add(board.Items[tc, y]);
            }
        }

        ExecuteRemove(board, items, cells);
    }

    private static Item FindRandomNormalItem(Board board, params Item[] exclude)
    {
        var list = new System.Collections.Generic.List<Item>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                var item = board.Items[x, y];
                if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                    continue;
                bool skip = false;
                foreach (var e in exclude)
                {
                    if (e == item) { skip = true; break; }
                }
                if (!skip) list.Add(item);
            }
        }

        if (list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    private static void ExecuteRemove(
        Board board,
        System.Collections.Generic.HashSet<Item> items,
        System.Collections.Generic.HashSet<SpecialCell> cells)
    {
        // Damage nearby special cells (orthogonal) then remove.
        var indices = new System.Collections.Generic.HashSet<int>();
        foreach (var item in items)
        {
            if (item != null)
                indices.Add(board.Data.GetIndex(item.Column, item.Row));
        }
        foreach (var cell in cells)
        {
            if (cell != null)
                indices.Add(board.Data.GetIndex(cell.Column, cell.Row));
        }
        Object.FindObjectOfType<SpecialCellHandler>()?.DamageAround(board, indices);

        foreach (var item in items)
        {
            if (item == null) continue;
            int c = item.Column;
            int r = item.Row;

            var cell = board.GetSpecialCell(c, r);
            if (cell != null && cell.IsDestroyableBySpecial())
            {
                cell.ClearOccupant(item);
                cell.TakeDamage(1);
            }

            if (!string.IsNullOrEmpty(item.SpecialItemId))
            {
                var special = item.GetComponent<SpecialItem>();
                if (special != null)
                {
                    board.QueueSpecialItem(special);
                    continue;
                }
            }

            board.SetItemId(c, r, "");
            board.SetSpecialItemId(c, r, "");
            board.Items[c, r] = null;
            Object.Destroy(item.gameObject);
        }

        foreach (var cell in cells)
        {
            if (cell != null && cell.IsDestroyableBySpecial())
                cell.TakeDamage(cell.MaxHealth);
        }
    }
}
