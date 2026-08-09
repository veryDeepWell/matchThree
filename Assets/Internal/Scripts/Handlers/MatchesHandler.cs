using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    [Header("Animation Delays")]
    [SerializeField] private float _matchDelay = 0.15f;
    [SerializeField] private float _dropDelay = 0.08f;
    [SerializeField] private float _postDropDelay = 0.12f;
    [SerializeField] private float _specialItemTriggerDelay = 0.3f;
    [SerializeField] private float _bombExplosionDelay = 0.3f;

    private bool _isProcessing;

    public bool IsProcessing => _isProcessing;
    public float GetBombExplosionDelay() => _bombExplosionDelay;
    public float GetSpecialItemTriggerDelay() => _specialItemTriggerDelay;

    private class AnimationStart
    {
        public Item Item;
        public Vector2 StartPosition;

        public AnimationStart(Item item, Vector2 startPosition)
        {
            Item = item;
            StartPosition = startPosition;
        }
    }

    public HashSet<int> FindMatches(Board board)
    {
        if (board?.Data == null)
            return new HashSet<int>();

        return MatchFinder.FindMatches(board.Data);
    }

    public void ProcessMatches(Board board)
    {
        if (board == null || _isProcessing)
            return;

        StartCoroutine(ProcessTurnCoroutine(board));
    }

    public void DropItems(Board board)
    {
        if (board == null || _isProcessing)
        {
            if (board != null && !_isProcessing)
                StartCoroutine(DropItemsCoroutine(board));
            return;
        }

        StartCoroutine(DropItemsCoroutine(board));
    }

    /// <summary>
    /// Called after a special combination has already cleared cells synchronously
    /// (magnet, dual-bomb, wide sweeper, etc.). Forces gravity + refill, then runs
    /// the normal match/cascade loop so the board doesn't stay with holes.
    /// </summary>
    public void ProcessAfterClear(Board board)
    {
        if (board == null || _isProcessing)
            return;

        StartCoroutine(ProcessAfterClearCoroutine(board));
    }

    private IEnumerator ProcessAfterClearCoroutine(Board board)
    {
        _isProcessing = true;

        try
        {
            yield return StartCoroutine(DrainSpecialItemQueue(board));
            yield return StartCoroutine(DropItemsCoroutine(board));
            yield return new WaitForSeconds(_dropDelay + _postDropDelay);

            yield return StartCoroutine(CascadeLoop(board, -1, -1));

            var generator = FindObjectOfType<ItemGenerator>();
            generator?.EnsurePlayableBoard(board);
        }
        finally
        {
            _isProcessing = false;
            board.ResetHintTimer();
        }
    }

    private IEnumerator ProcessTurnCoroutine(Board board)
    {
        _isProcessing = true;

        try
        {
            var (swapColumn, swapRow) = board.GetLastSwapPosition();
            var (secondSwapColumn, secondSwapRow) = board.GetSecondSwapPosition();
            board.ClearLastSwapPosition();

            QueueSpecialItemAt(board, swapColumn, swapRow);
            QueueSpecialItemAt(board, secondSwapColumn, secondSwapRow);

            bool initialSpecialTrigger = board.HasQueuedSpecialItems;
            yield return StartCoroutine(DrainSpecialItemQueue(board));

            if (initialSpecialTrigger)
                yield return StartCoroutine(DropItemsCoroutine(board));

            yield return StartCoroutine(CascadeLoop(board, swapColumn, swapRow));

            var generator = FindObjectOfType<ItemGenerator>();
            generator?.EnsurePlayableBoard(board);
        }
        finally
        {
            _isProcessing = false;
            board.ResetHintTimer();
        }
    }

    private IEnumerator CascadeLoop(Board board, int swapColumn, int swapRow)
    {
        bool continueCycle = true;

        while (continueCycle)
        {
            var matches = FindMatches(board);

            if (matches.Count == 0)
            {
                continueCycle = false;
                continue;
            }

            try
            {
                CheckForSpecialItems(board, matches, swapColumn, swapRow);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MatchesHandler] CheckForSpecialItems failed: {e.Message}");
            }

            DamageSpecialCellsAroundMatches(board, matches);
            RemoveItems(board, matches);

            yield return new WaitForSeconds(_matchDelay);

            yield return StartCoroutine(DropItemsCoroutine(board));
            yield return new WaitForSeconds(_dropDelay + _postDropDelay);

            bool cascadeSpecialTrigger = board.HasQueuedSpecialItems;
            yield return StartCoroutine(DrainSpecialItemQueue(board));

            if (cascadeSpecialTrigger)
            {
                yield return StartCoroutine(DropItemsCoroutine(board));
                yield return new WaitForSeconds(_dropDelay + _postDropDelay);
            }

            swapColumn = -1;
            swapRow = -1;
        }
    }

    private IEnumerator DrainSpecialItemQueue(Board board)
    {
        while (true)
        {
            var queuedItems = board.ConsumeQueuedSpecialItems();
            if (queuedItems.Count == 0)
                yield break;

            foreach (var specialItem in queuedItems)
            {
                if (specialItem == null)
                    continue;

                yield return StartCoroutine(specialItem.TriggerRoutine());
            }

            yield return null;
        }
    }

    private void QueueSpecialItemAt(Board board, int column, int row)
    {
        if (board == null || !board.IsCellActive(column, row))
            return;

        var item = board.Items[column, row];
        if (item == null || string.IsNullOrEmpty(item.SpecialItemId))
            return;

        board.QueueSpecialItem(item.GetComponent<SpecialItem>());
    }

    private IEnumerator DropItemsCoroutine(Board board)
    {
        if (board?.Data == null)
            yield break;

        var itemHandler = FindObjectOfType<ItemHandler>();
        var registry = itemHandler?.GetRegistry();

        if (itemHandler == null || registry == null)
        {
            Debug.LogError("[MatchesHandler] ItemHandler or ItemRegistry is missing.");
            yield break;
        }

        var animationStarts = new List<AnimationStart>();

        // ============ ГЛАВНОЕ ИЗМЕНЕНИЕ: МНОГОПРОХОДНАЯ ГРАВИТАЦИЯ ============
        // Делаем несколько проходов, чтобы все предметы упали до конца
        bool movedAny = true;
        int maxPasses = 20; // Защита от бесконечного цикла
        
        while (movedAny && maxPasses > 0)
        {
            movedAny = false;
            maxPasses--;
            
            // Проходим СНИЗУ ВВЕРХ, чтобы предметы падали последовательно
            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    if (!board.IsCellActive(column, row))
                        continue;

                    var specialCell = board.GetSpecialCell(column, row);
                    if (specialCell != null && specialCell.BlocksFalling())
                        continue;

                    var item = board.Items[column, row];
                    if (item == null)
                        continue;

                    // Пытаемся упасть вниз
                    if (CanMoveDown(board, column, row))
                    {
                        MoveItemDown(board, column, row, animationStarts);
                        movedAny = true;
                        continue;
                    }

                    // Если заблокирован неподвижной спец-ячейкой - пробуем диагональ
                    if (IsBlockedBySpecialCell(board, column, row))
                    {
                        if (TryDiagonalAvalanche(board, column, row, animationStarts))
                        {
                            movedAny = true;
                            continue;
                        }
                    }
                }
            }
        }

        // Заполнение пустот сверху
        FillTopSegment(board, itemHandler, registry, animationStarts);

        // Запуск анимаций
        float maxMoveDuration = 0f;

        foreach (var animation in animationStarts)
        {
            var item = animation.Item;
            if (item == null)
                continue;

            item.SetVisualPosition(animation.StartPosition);

            var attachedCell = board.GetSpecialCell(item.Column, item.Row);
            if (attachedCell != null && attachedCell.Occupant == item)
                attachedCell.transform.position = animation.StartPosition;

            maxMoveDuration = Mathf.Max(maxMoveDuration, item.MoveDuration);
        }

        foreach (var animation in animationStarts)
        {
            var item = animation.Item;
            if (item == null)
                continue;

            StartCoroutine(item.MoveToPosition(item.Column, item.Row));
        }

        if (maxMoveDuration > 0f)
            yield return new WaitForSeconds(maxMoveDuration);

        yield return null;
    }

    // ============ ЛОГИКА ПАДЕНИЯ ============

    private bool CanMoveDown(Board board, int column, int row)
    {
        int targetRow = row - 1;

        if (!IsInsideBoard(board, column, targetRow))
            return false;

        if (!board.IsCellActive(column, targetRow))
            return false;

        if (board.Items[column, targetRow] != null)
            return false;

        // Non-falling special cells (ice, vine) act as barriers only while they
        // still have an occupant. After the occupant is matched away the cell
        // becomes temporarily free: the first item that falls into it sticks
        // (AttachItem) and the barrier behaviour resumes. Therefore we do NOT
        // block entry into an empty non-falling special cell.
        // (Generation into such cells is still prevented in FillTopSegment.)

        return true;
    }

    private bool IsBlockedBySpecialCell(Board board, int column, int row)
    {
        int targetRow = row - 1;

        if (!IsInsideBoard(board, column, targetRow))
            return false;

        var specialCell = board.GetSpecialCell(column, targetRow);
        return specialCell != null && !specialCell.CanFall();
    }

    private void MoveItemDown(Board board, int fromColumn, int fromRow, List<AnimationStart> animationStarts)
    {
        int toRow = fromRow - 1;
        
        var item = board.Items[fromColumn, fromRow];
        if (item == null)
            return;

        // Сохраняем начальную позицию для анимации
        animationStarts.Add(new AnimationStart(item, item.transform.position));

        // If the source has a falling special cell (stone, chain), move it
        // together with the item so the overlay stays attached.
        var sourceCell = board.GetSpecialCell(fromColumn, fromRow);
        if (sourceCell != null && sourceCell.CanFall())
        {
            sourceCell.SetGridPosition(fromColumn, toRow);
        }

        // Перемещаем в Board
        board.Items[fromColumn, fromRow] = null;
        board.Items[fromColumn, toRow] = item;
        board.SetItemId(fromColumn, fromRow, "");
        board.SetItemId(fromColumn, toRow, item.ItemId ?? "");
        board.SetSpecialItemId(fromColumn, fromRow, "");
        board.SetSpecialItemId(fromColumn, toRow, item.SpecialItemId ?? "");

        // Обновляем координаты предмета
        item.Column = fromColumn;
        item.Row = toRow;

        // Обновляем SpecialItem если есть
        var specialItem = item.GetComponent<SpecialItem>();
        if (specialItem != null)
            specialItem.SetGridPosition(fromColumn, toRow);

        // Обновляем родительский Tile
        var targetTile = board.transform.Find($"Tile({fromColumn},{toRow})");
        if (targetTile != null)
            item.transform.SetParent(targetTile, true);

        // Attach to the special cell that is now at the target (either the one
        // we just moved, or a non-falling barrier that was temporarily free).
        var cell = board.GetSpecialCell(fromColumn, toRow);
        if (cell != null)
        {
            cell.AttachItem(item);
        }
        else if (sourceCell != null && !sourceCell.CanFall())
        {
            // Non-falling cell stays behind; clear its occupant reference.
            sourceCell.ClearOccupant(item);
        }
    }

    private bool TryDiagonalAvalanche(Board board, int column, int row, List<AnimationStart> animationStarts)
    {
        int targetRow = row - 1;

        // Сначала пробуем влево-вниз
        if (CanMoveDiagonally(board, column, targetRow, -1))
        {
            return MoveDiagonally(board, column, row, column - 1, targetRow, animationStarts);
        }

        // Затем вправо-вниз
        if (CanMoveDiagonally(board, column, targetRow, 1))
        {
            return MoveDiagonally(board, column, row, column + 1, targetRow, animationStarts);
        }

        return false;
    }

    private bool CanMoveDiagonally(Board board, int column, int targetRow, int direction)
    {
        int targetColumn = column + direction;

        if (!IsInsideBoard(board, targetColumn, targetRow))
            return false;

        if (!board.IsCellActive(targetColumn, targetRow))
            return false;

        if (board.Items[targetColumn, targetRow] != null)
            return false;

        // Same rule as CanMoveDown: empty non-falling special cells are allowed
        // so that a freed barrier can receive the next falling item.

        return true;
    }

    private bool MoveDiagonally(Board board, int fromColumn, int fromRow, int toColumn, int toRow, List<AnimationStart> animationStarts)
    {
        var item = board.Items[fromColumn, fromRow];
        if (item == null)
            return false;

        animationStarts.Add(new AnimationStart(item, item.transform.position));

        // Move falling special cell together with the item.
        var sourceCell = board.GetSpecialCell(fromColumn, fromRow);
        if (sourceCell != null && sourceCell.CanFall())
        {
            sourceCell.SetGridPosition(toColumn, toRow);
        }

        board.Items[fromColumn, fromRow] = null;
        board.Items[toColumn, toRow] = item;
        board.SetItemId(fromColumn, fromRow, "");
        board.SetItemId(toColumn, toRow, item.ItemId ?? "");
        board.SetSpecialItemId(fromColumn, fromRow, "");
        board.SetSpecialItemId(toColumn, toRow, item.SpecialItemId ?? "");

        item.Column = toColumn;
        item.Row = toRow;

        var specialItem = item.GetComponent<SpecialItem>();
        if (specialItem != null)
            specialItem.SetGridPosition(toColumn, toRow);

        var targetTile = board.transform.Find($"Tile({toColumn},{toRow})");
        if (targetTile != null)
            item.transform.SetParent(targetTile, true);

        var cell = board.GetSpecialCell(toColumn, toRow);
        if (cell != null)
        {
            cell.AttachItem(item);
        }
        else if (sourceCell != null && !sourceCell.CanFall())
        {
            sourceCell.ClearOccupant(item);
        }

        return true;
    }

    private bool IsInsideBoard(Board board, int column, int row)
    {
        return column >= 0 && column < board.Width && row >= 0 && row < board.Height;
    }

    // ============ ЗАПОЛНЕНИЕ ПУСТОТ СВЕРХУ ============

    private void FillTopSegment(
        Board board,
        ItemHandler itemHandler,
        ItemRegistry registry,
        List<AnimationStart> animationStarts)
    {
        for (int column = 0; column < board.Width; column++)
        {
            // Non-falling special cells (ice, vine) are hard barriers.
            // New items may only be spawned in the open segment that reaches
            // the top of the board. As soon as we hit a BlocksFalling cell
            // we stop — nothing is generated underneath the barrier.
            int spawnIndex = 0;

            for (int row = board.Height - 1; row >= 0; row--)
            {
                if (!board.IsCellActive(column, row))
                    continue;

                var specialCell = board.GetSpecialCell(column, row);

                // Barrier stops generation for the entire rest of the column.
                if (specialCell != null && specialCell.BlocksFalling())
                    break;

                if (board.Items[column, row] != null)
                    continue;

                string itemId = registry.GetRandomNormalId();
                if (string.IsNullOrEmpty(itemId))
                    continue;

                var tile = board.transform.Find($"Tile({column},{row})");
                if (tile == null)
                    continue;

                // Spawn above the board so the item falls down visually.
                Vector2 startPosition = board.GetWorldPosition(column, board.Height + spawnIndex);
                var itemObject = itemHandler.CreateItem(itemId, startPosition, tile);

                var item = itemObject?.GetComponent<Item>();
                if (item == null)
                    continue;

                item.Column = column;
                item.Row = row;
                item.Board = board;
                item.ItemId = itemId;
                item.SpecialItemId = "";

                board.Items[column, row] = item;
                board.SetItemId(column, row, itemId);

                // If we somehow land on a (non-blocking) special cell, attach.
                specialCell?.AttachItem(item);

                animationStarts.Add(new AnimationStart(item, startPosition));
                spawnIndex++;
            }
        }
    }

    // ============ УДАЛЕНИЕ ПРЕДМЕТОВ ============

    private void RemoveItems(Board board, HashSet<int> matches)
    {
        foreach (int index in matches)
        {
            int column = index % board.Width;
            int row = index / board.Width;

            var item = board.Items[column, row];
            // Keep newly-created specials that replaced a matched cell.
            if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                continue;

            board.GetSpecialCell(column, row)?.ClearOccupant(item);
            board.SetItemId(column, row, "");
            board.SetSpecialItemId(column, row, "");
            board.Items[column, row] = null;
            Destroy(item.gameObject);
        }
    }

    private void DamageSpecialCellsAroundMatches(Board board, HashSet<int> matches)
    {
        FindObjectOfType<SpecialCellHandler>()?.DamageAround(board, matches, 1);
    }

    // ============ СОЗДАНИЕ СПЕЦ-ПРЕДМЕТОВ ============

    private void CheckForSpecialItems(
        Board board,
        HashSet<int> matches,
        int swapColumn,
        int swapRow)
    {
        if (matches == null || matches.Count == 0)
            return;

        // Tuple: (run coordinates along the free axis, fixed coordinate of the line)
        var horizontalLines = new List<(List<int> Coords, int Fixed)>();
        var verticalLines = new List<(List<int> Coords, int Fixed)>();

        for (int row = 0; row < board.Height; row++)
        {
            var columns = new List<int>();
            for (int column = 0; column < board.Width; column++)
            {
                if (matches.Contains(board.Data.GetIndex(column, row)))
                    columns.Add(column);
            }
            CollectConsecutiveRuns(columns, row, horizontalLines);
        }

        for (int column = 0; column < board.Width; column++)
        {
            var rows = new List<int>();
            for (int row = 0; row < board.Height; row++)
            {
                if (matches.Contains(board.Data.GetIndex(column, row)))
                    rows.Add(row);
            }
            CollectConsecutiveRuns(rows, column, verticalLines);
        }

        // Cells already used for a higher-priority special so we don't create two
        // specials from overlapping match geometry.
        var claimed = new HashSet<int>();

        // Priority 1: 5+ in a row → horizontal sweeper
        foreach (var line in horizontalLines)
        {
            if (line.Coords.Count < 5) continue;
            if (TryClaimLine(board, line.Coords, line.Fixed, true, claimed,
                    out int col, out int row, swapColumn, swapRow))
            {
                CreateSpecialAt(board, col, row, "sweeper_h");
            }
        }

        // Priority 2: 5+ in a column → vertical sweeper
        foreach (var line in verticalLines)
        {
            if (line.Coords.Count < 5) continue;
            if (TryClaimLine(board, line.Coords, line.Fixed, false, claimed,
                    out int col, out int row, swapColumn, swapRow))
            {
                CreateSpecialAt(board, col, row, "sweeper_v");
            }
        }

        // Priority 3: plus/cross of 5 (3+3 sharing centre) → cross sweeper
        if (TryFindCross(board, matches, claimed, swapColumn, swapRow,
                out int crossCol, out int crossRow))
        {
            CreateSpecialAt(board, crossCol, crossRow, "sweeper_cross");
        }

        // Priority 4: any other match of size >= 5 → magnet
        if (matches.Count >= 5)
        {
            // Only if we haven't already claimed enough cells for a higher special.
            int unclaimed = 0;
            int bestCol = -1, bestRow = -1;
            foreach (int index in matches)
            {
                if (claimed.Contains(index)) continue;
                unclaimed++;
                int c = index % board.Width;
                int r = index / board.Width;
                if (swapColumn == c && swapRow == r)
                {
                    bestCol = c;
                    bestRow = r;
                }
                else if (bestCol < 0)
                {
                    bestCol = c;
                    bestRow = r;
                }
            }

            if (unclaimed >= 5 && bestCol >= 0 &&
                board.GetSpecialCell(bestCol, bestRow) == null &&
                !claimed.Contains(board.Data.GetIndex(bestCol, bestRow)))
            {
                claimed.Add(board.Data.GetIndex(bestCol, bestRow));
                CreateSpecialAt(board, bestCol, bestRow, "magnet");
            }
        }

        // Priority 5: 4-in-a-row/col → bomb
        foreach (var line in horizontalLines)
        {
            if (line.Coords.Count < 4) continue;
            if (TryClaimLine(board, line.Coords, line.Fixed, true, claimed,
                    out int col, out int row, swapColumn, swapRow))
            {
                CreateSpecialAt(board, col, row, "bomb");
            }
        }

        foreach (var line in verticalLines)
        {
            if (line.Coords.Count < 4) continue;
            if (TryClaimLine(board, line.Coords, line.Fixed, false, claimed,
                    out int col, out int row, swapColumn, swapRow))
            {
                CreateSpecialAt(board, col, row, "bomb");
            }
        }
    }

    private static void CollectConsecutiveRuns(
        List<int> coordinates,
        int fixedCoord,
        List<(List<int> Coords, int Fixed)> lines)
    {
        if (coordinates.Count < 4)
            return;

        int start = 0;
        for (int index = 1; index <= coordinates.Count; index++)
        {
            bool lineEnded = index == coordinates.Count ||
                             coordinates[index] != coordinates[index - 1] + 1;
            if (!lineEnded) continue;

            int count = index - start;
            if (count >= 4)
                lines.Add((coordinates.GetRange(start, count), fixedCoord));

            start = index;
        }
    }

    private bool TryClaimLine(
        Board board,
        List<int> line,
        int fixedCoord,
        bool horizontal,
        HashSet<int> claimed,
        out int column,
        out int row,
        int swapColumn,
        int swapRow)
    {
        column = horizontal ? line[line.Count / 2] : fixedCoord;
        row = horizontal ? fixedCoord : line[line.Count / 2];

        if (swapColumn >= 0 && swapRow >= 0 &&
            ((horizontal && line.Contains(swapColumn) && swapRow == row) ||
             (!horizontal && line.Contains(swapRow) && swapColumn == column)))
        {
            column = swapColumn;
            row = swapRow;
        }

        int index = board.Data.GetIndex(column, row);
        if (claimed.Contains(index))
            return false;

        if (!board.IsCellActive(column, row) || board.GetSpecialCell(column, row) != null)
            return false;

        // Claim every cell of the line so overlapping lower-priority specials
        // cannot reuse them.
        if (horizontal)
        {
            foreach (int c in line)
                claimed.Add(board.Data.GetIndex(c, row));
        }
        else
        {
            foreach (int r in line)
                claimed.Add(board.Data.GetIndex(column, r));
        }

        return true;
    }

    private bool TryFindCross(
        Board board,
        HashSet<int> matches,
        HashSet<int> claimed,
        int swapColumn,
        int swapRow,
        out int column,
        out int row)
    {
        column = -1;
        row = -1;

        // A cross is a cell that has matched neighbours in both axes forming
        // at least a plus of total unique cells == 5 (centre + 2 H + 2 V).
        for (int c = 0; c < board.Width; c++)
        {
            for (int r = 0; r < board.Height; r++)
            {
                int centre = board.Data.GetIndex(c, r);
                if (!matches.Contains(centre) || claimed.Contains(centre))
                    continue;

                int left = 0, right = 0, up = 0, down = 0;
                for (int x = c - 1; x >= 0 && matches.Contains(board.Data.GetIndex(x, r)); x--) left++;
                for (int x = c + 1; x < board.Width && matches.Contains(board.Data.GetIndex(x, r)); x++) right++;
                for (int y = r - 1; y >= 0 && matches.Contains(board.Data.GetIndex(c, y)); y--) down++;
                for (int y = r + 1; y < board.Height && matches.Contains(board.Data.GetIndex(c, y)); y++) up++;

                int hSpan = left + right + 1;
                int vSpan = up + down + 1;
                int total = hSpan + vSpan - 1; // centre counted twice

                if (hSpan >= 3 && vSpan >= 3 && total >= 5)
                {
                    column = (swapColumn == c && swapRow == r) ? swapColumn : c;
                    row = (swapColumn == c && swapRow == r) ? swapRow : r;

                    if (board.GetSpecialCell(column, row) != null)
                        continue;

                    // Claim the plus arms
                    claimed.Add(board.Data.GetIndex(c, r));
                    for (int x = c - left; x <= c + right; x++)
                        claimed.Add(board.Data.GetIndex(x, r));
                    for (int y = r - down; y <= r + up; y++)
                        claimed.Add(board.Data.GetIndex(c, y));

                    return true;
                }
            }
        }

        return false;
    }

    private void CreateSpecialAt(Board board, int column, int row, string specialId)
    {
        try
        {
            var generator = FindObjectOfType<ItemGenerator>();
            if (generator == null) return;

            generator.ReplaceWithSpecial(board, column, row, specialId);

            // If the requested special failed to spawn (missing definition/effect),
            // fall back to a bomb so the match still produces something.
            var item = board.Items[column, row];
            if (item == null || string.IsNullOrEmpty(item.SpecialItemId))
            {
                if (specialId != "bomb")
                    generator.ReplaceWithSpecial(board, column, row, "bomb");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MatchesHandler] Failed to create special '{specialId}' at ({column},{row}): {e.Message}");
        }
    }
}