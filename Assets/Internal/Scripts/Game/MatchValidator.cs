using System.Collections.Generic;

public static class MatchValidator
{
    public static bool HasPossibleMoves(BoardData data)
    {
        int w = data.Width;
        int h = data.Height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = data.GetIndex(x, y);
                if (!data.ActiveCells[idx]) continue;
                if (string.IsNullOrEmpty(data.Items[idx])) continue;

                if (CanSwapAndMatch(data, x, y, x + 1, y)) return true;
                if (CanSwapAndMatch(data, x, y, x - 1, y)) return true;
                if (CanSwapAndMatch(data, x, y, x, y + 1)) return true;
                if (CanSwapAndMatch(data, x, y, x, y - 1)) return true;
            }
        }
        return false;
    }

    private static bool CanSwapAndMatch(BoardData data, int x1, int y1, int x2, int y2)
    {
        if (!data.IsValid(x1, y1) || !data.IsValid(x2, y2)) return false;
        if (!data.ActiveCells[data.GetIndex(x1, y1)]) return false;
        if (!data.ActiveCells[data.GetIndex(x2, y2)]) return false;

        int idx1 = data.GetIndex(x1, y1);
        int idx2 = data.GetIndex(x2, y2);
        string temp = data.Items[idx1];
        data.Items[idx1] = data.Items[idx2];
        data.Items[idx2] = temp;

        var matches = MatchFinder.FindMatches(data);

        data.Items[idx1] = data.Items[idx2];
        data.Items[idx2] = temp;

        return matches.Count > 0;
    }
}