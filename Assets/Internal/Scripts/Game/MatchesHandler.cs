using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchesHandler : MonoBehaviour
{
    [SerializeField] private float _moveDuration = 0.15f;

    public HashSet<int> FindMatches(Board board)
    {
        if (board?.Data == null) return new HashSet<int>();

        var data = board.Data;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var item = board.Items[x, y];
                if (item != null && !string.IsNullOrEmpty(item.ItemId))
                    data.SetItem(x, y, item.ItemId);
                else
                    data.SetItem(x, y, "");
            }
        }

        return MatchFinder.FindMatches(data);
    }

    public void ProcessMatches(Board board)
    {
        if (board == null) return;
        StartCoroutine(ProcessMatchesCoroutine(board));
    }

    private IEnumerator ProcessMatchesCoroutine(Board board)
    {
        var matches = FindMatches(board);

        if (matches.Count == 0) yield break;

        CheckForSpecialItems(board, matches);

        RemoveItems(board, matches);
        yield return new WaitForSeconds(0.05f);

        DropItems(board);
        yield return new WaitForSeconds(_moveDuration + 0.1f);

        board.CheckMatches();
    }

    private void RemoveItems(Board board, HashSet<int> matches)
    {
        var data = board.Data;
        int w = data.Width;

        foreach (int idx in matches)
        {
            int x = idx % w;
            int y = idx / w;
            var item = board.Items[x, y];
            if (item)
            {
                data.SetItem(x, y, "");
                board.Items[x, y] = null;
                Destroy(item.gameObject);
            }
        }
    }

    public void DropItems(Board board)
    {
        var data = board.Data;
        int w = data.Width;
        int h = data.Height;
        var generator = FindObjectOfType<ItemGenerator>();
        var handler = FindObjectOfType<ItemHandler>();

        // Получаем реестр через ItemHandler
        ItemRegistry registry = null;
        if (handler != null)
        {
            registry = handler.GetRegistry();
        }

        for (int x = 0; x < w; x++)
        {
            int empty = 0;
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);

                if (!data.ActiveCells[idx])
                {
                    empty = 0;
                    continue;
                }

                if (string.IsNullOrEmpty(data.Items[idx]))
                {
                    empty++;
                }
                else if (empty > 0)
                {
                    int newY = y - empty;
                    int newIdx = data.GetIndex(x, newY);

                    if (data.ActiveCells[newIdx])
                    {
                        var item = board.Items[x, y];
                        board.Items[x, y] = null;
                        board.Items[x, newY] = item;
                        item.Row = newY;
                        board.StartCoroutine(item.MoveToPosition(x, newY));

                        data.Items[newIdx] = data.Items[idx];
                        data.Items[idx] = "";
                    }
                    else
                    {
                        empty = 0;
                    }
                }
            }

            // Респавн новых предметов сверху
            int emptyCount = 0;
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;
                if (string.IsNullOrEmpty(data.Items[idx])) emptyCount++;
            }

            if (emptyCount > 0 && generator != null && handler != null && registry != null)
            {
                for (int y = h - emptyCount; y < h; y++)
                {
                    int idx = data.GetIndex(x, y);
                    if (!data.ActiveCells[idx]) continue;
                    if (!string.IsNullOrEmpty(data.Items[idx])) continue;

                    string type = registry.GetRandomNormalId();
                    if (string.IsNullOrEmpty(type)) continue;

                    Vector2 startPos = board.GetWorldPosition(x, h + 1);
                    Vector2 targetPos = board.GetWorldPosition(x, y);

                    var go = handler.CreateItem(type, startPos, board.transform);
                    if (go == null) continue;
                    
                    var item = go.GetComponent<Item>();
                    if (item == null) continue;
                    
                    item.Column = x;
                    item.Row = y;
                    item.Board = board;
                    item.ItemId = type;

                    board.StartCoroutine(item.MoveToPosition(x, y));

                    board.Items[x, y] = item;
                    data.SetItem(x, y, type);
                }
            }
        }
    }

    private void CheckForSpecialItems(Board board, HashSet<int> matches)
    {
        if (board?.Data == null || matches == null) return;

        var data = board.Data;
        int w = data.Width;

        var horizontalMatches = new Dictionary<int, List<int>>();
        var verticalMatches = new Dictionary<int, List<int>>();

        foreach (int idx in matches)
        {
            int x = idx % w;
            int y = idx / w;

            if (!horizontalMatches.ContainsKey(y))
                horizontalMatches[y] = new List<int>();
            horizontalMatches[y].Add(x);

            if (!verticalMatches.ContainsKey(x))
                verticalMatches[x] = new List<int>();
            verticalMatches[x].Add(y);
        }

        foreach (var kvp in horizontalMatches)
        {
            int y = kvp.Key;
            var xs = kvp.Value;
            xs.Sort();

            int consecutive = 1;
            int startX = xs[0];

            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] == xs[i - 1] + 1)
                {
                    consecutive++;
                    if (consecutive >= 4)
                    {
                        int centerX = startX + consecutive / 2;
                        CreateBomb(board, centerX, y);
                        consecutive = 1;
                        startX = xs[i];
                    }
                }
                else
                {
                    consecutive = 1;
                    startX = xs[i];
                }
            }
        }

        foreach (var kvp in verticalMatches)
        {
            int x = kvp.Key;
            var ys = kvp.Value;
            ys.Sort();

            int consecutive = 1;
            int startY = ys[0];

            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == ys[i - 1] + 1)
                {
                    consecutive++;
                    if (consecutive >= 4)
                    {
                        int centerY = startY + consecutive / 2;
                        CreateBomb(board, x, centerY);
                        consecutive = 1;
                        startY = ys[i];
                    }
                }
                else
                {
                    consecutive = 1;
                    startY = ys[i];
                }
            }
        }
    }

    private void CreateBomb(Board board, int x, int y)
    {
        if (!board.IsCellActive(x, y)) return;

        var oldItem = board.Items[x, y];
        if (oldItem != null)
        {
            if (!string.IsNullOrEmpty(oldItem.SpecialItemId))
                return;

            board.SetItemId(x, y, "bomb");
            board.Items[x, y] = null;
            Destroy(oldItem.gameObject);
        }

        var handler = FindObjectOfType<SpecialItemHandler>();
        if (handler == null) return;

        Vector2 pos = board.GetWorldPosition(x, y);
        GameObject go = handler.CreateSpecialItem("bomb", pos, board.transform);

        if (go == null) return;
        
        var item = go.GetComponent<Item>();
        if (item != null)
        {
            item.Column = x;
            item.Row = y;
            item.Board = board;
            item.ItemId = "";
            item.SpecialItemId = "bomb";
            board.Items[x, y] = item;
        }
    }
}