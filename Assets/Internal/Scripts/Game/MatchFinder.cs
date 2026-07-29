using System.Collections.Generic;

public static class MatchFinder
{
    public static HashSet<int> FindMatches(BoardData data)
    {
        var matches = new HashSet<int>();
        int w = data.Width;
        int h = data.Height;

        // Горизонтальные
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w - 2; x++)
            {
                int idx = y * w + x;
                if (!data.ActiveCells[idx]) continue;      // ← ActiveCells[idx]
                var type = data.Items[idx];
                if (type == ItemTypes.None) continue;

                if (data.Items[idx + 1] == type && data.Items[idx + 2] == type)
                {
                    int endX = x + 2;
                    while (endX + 1 < w && data.Items[y * w + endX + 1] == type)
                        endX++;

                    for (int i = x; i <= endX; i++)
                        matches.Add(y * w + i);
                }
            }
        }

        // Вертикальные
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h - 2; y++)
            {
                int idx = y * w + x;
                if (!data.ActiveCells[idx]) continue;      // ← ActiveCells[idx]
                var type = data.Items[idx];
                if (type == ItemTypes.None) continue;

                if (data.Items[idx + w] == type && data.Items[idx + 2 * w] == type)
                {
                    int endY = y + 2;
                    while (endY + 1 < h && data.Items[(endY + 1) * w + x] == type)
                        endY++;

                    for (int i = y; i <= endY; i++)
                        matches.Add(i * w + x);
                }
            }
        }

        return matches;
    }
}