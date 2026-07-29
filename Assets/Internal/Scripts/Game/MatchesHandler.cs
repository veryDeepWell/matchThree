using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class MatchesHandler : MonoBehaviour
{
    [SerializeField] private float _moveDuration = 0.15f;

    public HashSet<int> FindMatches(Board board)
    {
        if (board?.Data == null) return new HashSet<int>();
        
        // Синхронизируем данные перед поиском
        var data = board.Data;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var item = board.Items[x, y];
                if (item != null)
                    data.SetItem(x, y, item.ItemType);
                else
                    data.SetItem(x, y, ItemTypes.None);
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
        var data = board.Data;
        var matches = FindMatches(board);

        if (matches.Count == 0) yield break;

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
                data.SetItem(x, y, ItemTypes.None);
                board.Items[x, y] = null;
                Destroy(item.gameObject);
            }
        }
    }

    private void DropItems(Board board)
    {
        var data = board.Data;
        int w = data.Width;
        int h = data.Height;

        for (int x = 0; x < w; x++)
        {
            int empty = 0;
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                
                if (!data.ActiveCells[idx])
                {
                    empty++;
                    continue;
                }

                if (data.Items[idx] == ItemTypes.None)
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
                        data.Items[idx] = ItemTypes.None;
                    }
                }
            }
        }
    }
}