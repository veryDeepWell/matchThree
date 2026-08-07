using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    [Header("Animation Delays")]
    [SerializeField] private float _matchDelay = 0.15f;
    [SerializeField] private float _dropDelay = 0.1f;
    [SerializeField] private float _postDropDelay = 0.15f;
    [SerializeField] private float _bombExplosionDelay = 0.3f;

    public float GetBombExplosionDelay() => _bombExplosionDelay;

    public HashSet<int> FindMatches(Board board)
    {
        if (board?.Data == null)
            return new HashSet<int>();

        for (int column = 0; column < board.Width; column++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                var item = board.Items[column, row];
                string itemId = item != null && string.IsNullOrEmpty(item.SpecialItemId) ? item.ItemId : "";
                board.Data.SetItem(column, row, itemId);
            }
        }

        return MatchFinder.FindMatches(board.Data);
    }

    public void ProcessMatches(Board board)
    {
        if (board == null) return;
        StartCoroutine(ProcessMatchesCoroutine(board));
    }

    public void DropItems(Board board)
    {
        if (board?.Data == null) return;

        var generator = FindObjectOfType<ItemGenerator>();
        var itemHandler = FindObjectOfType<ItemHandler>();
        var registry = itemHandler != null ? itemHandler.GetRegistry() : null;
        if (generator == null || registry == null)
        {
            Debug.LogError("[MatchesHandler] ItemGenerator or ItemRegistry is missing.");
            return;
        }

        CollapseColumns(board);
        FillEmptyCells(board, registry);
    }

    private IEnumerator ProcessMatchesCoroutine(Board board)
    {
        var matches = FindMatches(board);
        if (matches.Count == 0)
            yield break;

        CheckForSpecialItems(board, matches);
        DamageSpecialCellsAroundMatches(board, matches);
        RemoveItems(board, matches);

        yield return new WaitForSeconds(_matchDelay);

        DropItems(board);
        yield return new WaitForSeconds(_dropDelay + _postDropDelay);

        board.CheckMatches();
    }

    private void RemoveItems(Board board, HashSet<int> matches)
    {
        foreach (int index in matches)
        {
            int column = index % board.Width;
            int row = index / board.Width;
            var item = board.Items[column, row];
            if (item == null || !string.IsNullOrEmpty(item.SpecialItemId)) continue;

            board.GetSpecialCell(column, row)?.ClearOccupant(item);
            board.SetItemId(column, row, "");
            board.Items[column, row] = null;
            Destroy(item.gameObject);
        }
    }

    private void CollapseColumns(Board board)
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
                    specialCell.AttachItem(board.Items[column, row]);
                    writeRow = row + 1;
                    continue;
                }

                var item = board.Items[column, row];
                bool hasObject = item != null || specialCell != null;
                if (!hasObject) continue;

                if (writeRow != row)
                {
                    if (item != null)
                        MoveItem(board, item, column, row, column, writeRow);
                    else if (specialCell != null && specialCell.CanFall())
                        MoveSpecialCell(board, specialCell, column, row, column, writeRow);
                }

                writeRow++;
            }
        }
    }

    private void FillEmptyCells(Board board, ItemRegistry registry)
    {
        var generator = FindObjectOfType<ItemGenerator>();
        if (generator == null) return;

        for (int column = 0; column < board.Width; column++)
        {
            for (int row = 0; row < board.Height; row++)
            {
                if (!board.IsCellActive(column, row) || board.Items[column, row] != null)
                    continue;

                string itemId = registry.GetRandomNormalId();
                if (string.IsNullOrEmpty(itemId)) continue;

                var tile = board.transform.Find($"Tile({column},{row})");
                if (tile == null)
                {
                    var tileObject = Instantiate(generator.GetTilePrefab(), board.GetWorldPosition(column, row), Quaternion.identity, board.transform);
                    tileObject.name = $"Tile({column},{row})";
                    tile = tileObject.transform;
                }

                var itemObject = FindObjectOfType<ItemHandler>()?.CreateItem(itemId, board.GetWorldPosition(column, row), tile);
                var item = itemObject != null ? itemObject.GetComponent<Item>() : null;
                if (item == null) continue;

                item.Column = column;
                item.Row = row;
                item.Board = board;
                item.ItemId = itemId;
                board.Items[column, row] = item;
                board.SetItemId(column, row, itemId);
                board.GetSpecialCell(column, row)?.AttachItem(item);
            }
        }
    }

    private void MoveItem(Board board, Item item, int fromColumn, int fromRow, int toColumn, int toRow)
    {
        if (item == null || (fromColumn == toColumn && fromRow == toRow)) return;

        var oldCell = board.GetSpecialCell(fromColumn, fromRow);
        oldCell?.ClearOccupant(item);

        board.Items[fromColumn, fromRow] = null;
        board.SetItemId(fromColumn, fromRow, "");
        board.Items[toColumn, toRow] = item;
        board.SetItemId(toColumn, toRow, item.ItemId);

        item.Column = toColumn;
        item.Row = toRow;
        item.Board = board;

        var targetCell = board.GetSpecialCell(toColumn, toRow);
        targetCell?.AttachItem(item);

        var targetTile = board.transform.Find($"Tile({toColumn},{toRow})");
        if (targetTile != null)
            item.transform.SetParent(targetTile, true);

        if (oldCell != null && oldCell.CanFall())
        {
            oldCell.SetGridPosition(toColumn, toRow);
            oldCell.AttachItem(item);
        }

        board.StartCoroutine(item.MoveToPosition(toColumn, toRow));
    }

    private void MoveSpecialCell(Board board, SpecialCell cell, int fromColumn, int fromRow, int toColumn, int toRow)
    {
        if (cell == null) return;
        cell.SetGridPosition(toColumn, toRow);

        var item = board.Items[fromColumn, fromRow];
        if (item != null)
            cell.AttachItem(item);
    }

    private void DamageSpecialCellsAroundMatches(Board board, HashSet<int> matches)
    {
        var handler = FindObjectOfType<SpecialCellHandler>();
        handler?.DamageAround(board, matches, 1);
    }

    private void CheckForSpecialItems(Board board, HashSet<int> matches)
    {
        if (board?.Data == null || matches == null || matches.Count == 0) return;

        var (swapColumn, swapRow) = board.GetLastSwapPosition();
        board.ClearLastSwapPosition();

        var horizontalMatches = new Dictionary<int, List<int>>();
        var verticalMatches = new Dictionary<int, List<int>>();

        foreach (int index in matches)
        {
            int column = index % board.Width;
            int row = index / board.Width;

            if (!horizontalMatches.TryGetValue(row, out var horizontal))
            {
                horizontal = new List<int>();
                horizontalMatches[row] = horizontal;
            }
            horizontal.Add(column);

            if (!verticalMatches.TryGetValue(column, out var vertical))
            {
                vertical = new List<int>();
                verticalMatches[column] = vertical;
            }
            vertical.Add(row);
        }

        foreach (var pair in horizontalMatches)
            CreateBombsFromLine(board, pair.Value, pair.Key, true, swapColumn, swapRow);

        foreach (var pair in verticalMatches)
            CreateBombsFromLine(board, pair.Value, pair.Key, false, swapColumn, swapRow);
    }

    private void CreateBombsFromLine(Board board, List<int> coordinates, int fixedCoordinate, bool horizontal, int swapColumn, int swapRow)
    {
        coordinates.Sort();
        int consecutive = 1;
        int start = coordinates[0];

        for (int index = 1; index < coordinates.Count; index++)
        {
            if (coordinates[index] == coordinates[index - 1] + 1)
            {
                consecutive++;
                if (consecutive >= 4)
                {
                    int column = horizontal ? start + consecutive / 2 : fixedCoordinate;
                    int row = horizontal ? fixedCoordinate : start + consecutive / 2;

                    if (horizontal && swapRow == fixedCoordinate && coordinates.Contains(swapColumn))
                        column = swapColumn;
                    else if (!horizontal && swapColumn == fixedCoordinate && coordinates.Contains(swapRow))
                        row = swapRow;

                    CreateBomb(board, column, row);
                    consecutive = 1;
                    start = coordinates[index];
                }
            }
            else
            {
                consecutive = 1;
                start = coordinates[index];
            }
        }
    }

    private void CreateBomb(Board board, int column, int row)
    {
        FindObjectOfType<ItemGenerator>()?.ReplaceWithSpecial(board, column, row, "bomb");
    }
}
