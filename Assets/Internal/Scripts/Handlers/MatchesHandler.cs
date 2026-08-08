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

    private IEnumerator ProcessTurnCoroutine(Board board)
    {
        _isProcessing = true;

        try
        {
            var (swapColumn, swapRow) = board.GetLastSwapPosition();
            var (secondSwapColumn, secondSwapRow) = board.GetSecondSwapPosition();
            board.ClearLastSwapPosition();

            // Step 2: only the two items involved in the player's swap can
            // trigger immediately. Other special items are triggered only when
            // an effect explicitly queues them.
            QueueSpecialItemAt(board, swapColumn, swapRow);
            QueueSpecialItemAt(board, secondSwapColumn, secondSwapRow);

            bool initialSpecialTrigger = board.HasQueuedSpecialItems;
            yield return StartCoroutine(DrainSpecialItemQueue(board));

            // A special-item trigger always completes before gravity starts.
            if (initialSpecialTrigger)
                yield return StartCoroutine(DropItemsCoroutine(board));

            bool continueCycle = true;

            while (continueCycle)
            {
                var matches = FindMatches(board);

                if (matches.Count == 0)
                {
                    continueCycle = false;
                    continue;
                }

                CheckForSpecialItems(board, matches, swapColumn, swapRow);
                DamageSpecialCellsAroundMatches(board, matches);
                RemoveItems(board, matches);

                yield return new WaitForSeconds(_matchDelay);

                // Matched items disappear first; only then does gravity start.
                yield return StartCoroutine(DropItemsCoroutine(board));
                yield return new WaitForSeconds(_dropDelay + _postDropDelay);

                // Effects caused by this stage are resolved before the next
                // gravity pass and before checking the next set of matches.
                bool cascadeSpecialTrigger = board.HasQueuedSpecialItems;
                yield return StartCoroutine(DrainSpecialItemQueue(board));

                if (cascadeSpecialTrigger)
                {
                    yield return StartCoroutine(DropItemsCoroutine(board));
                    yield return new WaitForSeconds(_dropDelay + _postDropDelay);
                }

                // A bomb created by this match belongs to the resulting board
                // and must not be treated as another swap-created bomb.
                swapColumn = -1;
                swapRow = -1;
            }

            var generator = FindObjectOfType<ItemGenerator>();
            generator?.EnsurePlayableBoard(board);
        }
        finally
        {
            _isProcessing = false;
            board.ResetHintTimer();
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

            // Effects are allowed to queue more special items. They are drained
            // before gravity starts, so the screen never shows half-finished cascades.
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

        var animationStarts = new Dictionary<Item, Vector2>();

        CollapseColumns(board, animationStarts);
        ApplyDiagonalAvalanche(board, animationStarts);
        CollapseColumns(board, animationStarts);

        FillTopSegment(board, itemHandler, registry, animationStarts);

        float maxMoveDuration = 0f;

        foreach (var pair in animationStarts)
        {
            var item = pair.Key;
            if (item == null)
                continue;

            item.SetVisualPosition(pair.Value);

            var attachedCell = board.GetSpecialCell(item.Column, item.Row);
            if (attachedCell != null && attachedCell.Occupant == item)
                attachedCell.transform.position = pair.Value;

            maxMoveDuration = Mathf.Max(maxMoveDuration, item.MoveDuration);
        }

        foreach (var pair in animationStarts)
        {
            var item = pair.Key;
            if (item == null)
                continue;

            StartCoroutine(item.MoveToPosition(item.Column, item.Row));
        }

        if (maxMoveDuration > 0f)
            yield return new WaitForSeconds(maxMoveDuration);

        yield return null;
    }

    private void CollapseColumns(Board board, Dictionary<Item, Vector2> animationStarts)
    {
        for (int column = 0; column < board.Width; column++)
        {
            int writeRow = 0;

            for (int row = 0; row < board.Height; row++)
            {
                if (!board.IsCellActive(column, row))
                {
                    writeRow = row + 1;
                    continue;
                }

                var specialCell = board.GetSpecialCell(column, row);
                if (specialCell != null && specialCell.BlocksFalling())
                {
                    writeRow = row + 1;
                    continue;
                }

                var item = board.Items[column, row];
                if (item == null)
                    continue;

                if (writeRow != row && IsEmptyDestination(board, column, writeRow))
                {
                    MoveUnit(board, column, row, column, writeRow, animationStarts);
                }

                writeRow++;
            }
        }
    }

    private void ApplyDiagonalAvalanche(Board board, Dictionary<Item, Vector2> animationStarts)
    {
        for (int column = 0; column < board.Width; column++)
        {
            for (int row = 0; row < board.Height - 1; row++)
            {
                var barrier = board.GetSpecialCell(column, row);
                if (barrier == null || !barrier.BlocksFalling())
                    continue;

                var fallingItem = board.Items[column, row + 1];
                if (fallingItem == null)
                    continue;

                var sourceCell = board.GetSpecialCell(column, row + 1);
                if (sourceCell != null && sourceCell.BlocksFalling())
                    continue;

                int leftColumn = column - 1;
                int rightColumn = column + 1;

                bool canMoveLeft = IsEmptyDestination(board, leftColumn, row);
                bool canMoveRight = IsEmptyDestination(board, rightColumn, row);

                if (!canMoveLeft && !canMoveRight)
                    continue;

                int targetColumn;

                if (canMoveLeft && canMoveRight)
                    targetColumn = Random.value < 0.5f ? leftColumn : rightColumn;
                else
                    targetColumn = canMoveLeft ? leftColumn : rightColumn;

                MoveUnit(board, column, row + 1, targetColumn, row, animationStarts);
            }
        }
    }

    private bool IsEmptyDestination(Board board, int column, int row)
    {
        if (!board.IsCellActive(column, row))
            return false;

        if (board.Items[column, row] != null)
            return false;

        var specialCell = board.GetSpecialCell(column, row);
        return specialCell == null;
    }

    private void MoveUnit(
        Board board,
        int fromColumn,
        int fromRow,
        int toColumn,
        int toRow,
        Dictionary<Item, Vector2> animationStarts)
    {
        if (!board.IsCellActive(toColumn, toRow) ||
            board.Items[fromColumn, fromRow] == null ||
            !IsEmptyDestination(board, toColumn, toRow))
            return;

        var item = board.Items[fromColumn, fromRow];
        var cell = board.GetSpecialCell(fromColumn, fromRow);

        if (item != null && !animationStarts.ContainsKey(item))
            animationStarts[item] = item.transform.position;

        board.Items[fromColumn, fromRow] = null;
        board.SetItemId(fromColumn, fromRow, "");

        board.Items[toColumn, toRow] = item;
        board.SetItemId(toColumn, toRow, item.ItemId);

        item.Column = toColumn;
        item.Row = toRow;

        var targetTile = board.transform.Find($"Tile({toColumn},{toRow})");
        if (targetTile != null)
            item.transform.SetParent(targetTile, true);

        if (cell != null)
        {
            cell.SetGridPosition(toColumn, toRow);
            cell.AttachItem(item);
        }

        if (item != null)
        {
            var specialItem = item.GetComponent<SpecialItem>();
            specialItem?.SetGridPosition(toColumn, toRow);
        }
    }

    private void FillTopSegment(
        Board board,
        ItemHandler itemHandler,
        ItemRegistry registry,
        Dictionary<Item, Vector2> animationStarts)
    {
        for (int column = 0; column < board.Width; column++)
        {
            int topRow = FindTopEntryRow(board, column);
            if (topRow < 0)
                continue;

            int spawnIndex = 0;

            for (int row = topRow; row >= 0; row--)
            {
                if (!board.IsCellActive(column, row) ||
                    board.Items[column, row] != null)
                    continue;

                var specialCell = board.GetSpecialCell(column, row);
                if (specialCell != null && specialCell.BlocksFalling())
                    continue;

                string itemId = registry.GetRandomNormalId();
                if (string.IsNullOrEmpty(itemId))
                    continue;

                var tile = board.transform.Find($"Tile({column},{row})");
                if (tile == null)
                    continue;

                var itemObject = itemHandler.CreateItem(
                    itemId,
                    board.GetWorldPosition(column, topRow + 1 + spawnIndex),
                    tile);

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
                board.GetSpecialCell(column, row)?.AttachItem(item);

                animationStarts[item] = board.GetWorldPosition(column, topRow + 1 + spawnIndex);
                spawnIndex++;
            }
        }
    }

    private int FindTopEntryRow(Board board, int column)
    {
        for (int row = board.Height - 1; row >= 0; row--)
        {
            if (!board.IsCellActive(column, row))
                return row + 1;

            var cell = board.GetSpecialCell(column, row);
            if (cell != null && cell.BlocksFalling())
                return row + 1;
        }

        return 0;
    }

    private void RemoveItems(Board board, HashSet<int> matches)
    {
        foreach (int index in matches)
        {
            int column = index % board.Width;
            int row = index / board.Width;

            var item = board.Items[column, row];
            if (item == null || !string.IsNullOrEmpty(item.SpecialItemId))
                continue;

            board.GetSpecialCell(column, row)?.ClearOccupant(item);
            board.SetItemId(column, row, "");
            board.Items[column, row] = null;
            Destroy(item.gameObject);
        }
    }

    private void DamageSpecialCellsAroundMatches(Board board, HashSet<int> matches)
    {
        FindObjectOfType<SpecialCellHandler>()?.DamageAround(board, matches, 1);
    }

    private void CheckForSpecialItems(
        Board board,
        HashSet<int> matches,
        int swapColumn,
        int swapRow)
    {
        if (matches == null || matches.Count == 0)
            return;

        var horizontalLines = new List<(List<int> Coordinates, int Row)>();
        var verticalLines = new List<(List<int> Coordinates, int Column)>();

        for (int row = 0; row < board.Height; row++)
        {
            var columns = new List<int>();

            for (int column = 0; column < board.Width; column++)
            {
                if (matches.Contains(board.Data.GetIndex(column, row)))
                    columns.Add(column);
            }

            AddConsecutiveHorizontalLines(columns, row, horizontalLines);
        }

        for (int column = 0; column < board.Width; column++)
        {
            var rows = new List<int>();

            for (int row = 0; row < board.Height; row++)
            {
                if (matches.Contains(board.Data.GetIndex(column, row)))
                    rows.Add(row);
            }

            AddConsecutiveVerticalLines(rows, column, verticalLines);
        }

        // A line of four or more creates a bomb. Two independent three-matches
        // produced by one swap therefore never create a special item.
        foreach (var line in horizontalLines)
        {
            if (line.Coordinates.Count >= 4)
                CreateBombAtBestPosition(
                    board,
                    line.Coordinates,
                    true,
                    swapColumn,
                    swapRow,
                    line.Row);
        }

        foreach (var line in verticalLines)
        {
            if (line.Coordinates.Count >= 4)
                CreateBombAtBestPosition(
                    board,
                    line.Coordinates,
                    false,
                    swapColumn,
                    swapRow,
                    line.Column);
        }
    }

    private void AddConsecutiveHorizontalLines(
        List<int> coordinates,
        int row,
        List<(List<int> Coordinates, int Row)> lines)
    {
        if (coordinates.Count < 4)
            return;

        int start = 0;

        for (int index = 1; index <= coordinates.Count; index++)
        {
            bool lineEnded = index == coordinates.Count ||
                             coordinates[index] != coordinates[index - 1] + 1;

            if (!lineEnded)
                continue;

            int count = index - start;
            if (count >= 4)
                lines.Add((coordinates.GetRange(start, count), row));

            start = index;
        }
    }

    private void AddConsecutiveVerticalLines(
        List<int> coordinates,
        int column,
        List<(List<int> Coordinates, int Column)> lines)
    {
        if (coordinates.Count < 4)
            return;

        int start = 0;

        for (int index = 1; index <= coordinates.Count; index++)
        {
            bool lineEnded = index == coordinates.Count ||
                             coordinates[index] != coordinates[index - 1] + 1;

            if (!lineEnded)
                continue;

            int count = index - start;
            if (count >= 4)
                lines.Add((coordinates.GetRange(start, count), column));

            start = index;
        }
    }

    private void CreateBombAtBestPosition(
        Board board,
        List<int> line,
        bool horizontal,
        int swapColumn,
        int swapRow,
        int lineCoordinate)
    {
        int column = horizontal ? line[line.Count / 2] : lineCoordinate;
        int row = horizontal ? lineCoordinate : line[line.Count / 2];

        if (swapColumn >= 0 && swapRow >= 0 &&
            ((horizontal && line.Contains(swapColumn) && swapRow == row) ||
             (!horizontal && line.Contains(swapRow) && swapColumn == column)))
        {
            column = swapColumn;
            row = swapRow;
        }

        if (board.IsCellActive(column, row) &&
            board.GetSpecialCell(column, row) == null)
        {
            FindObjectOfType<ItemGenerator>()?.ReplaceWithSpecial(board, column, row, "bomb");
        }
    }

}