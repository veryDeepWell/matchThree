using System.Collections.Generic;

public static class MatchFinder
{
    public static HashSet<int> FindMatches(BoardData data)
    {
        var matches = new HashSet<int>();
        if (data == null || !data.IsStructurallyValid())
            return matches;

        FindHorizontalMatches(data, matches);
        FindVerticalMatches(data, matches);
        return matches;
    }

    private static void FindHorizontalMatches(BoardData data, HashSet<int> matches)
    {
        for (int row = 0; row < data.Height; row++)
        {
            for (int column = 0; column < data.Width; column++)
            {
                int startIndex = data.GetIndex(column, row);
                if (!data.ActiveCells[startIndex] || string.IsNullOrEmpty(data.Items[startIndex]))
                    continue;

                string itemId = data.Items[startIndex];
                int endColumn = column;
                while (endColumn + 1 < data.Width &&
                       data.ActiveCells[data.GetIndex(endColumn + 1, row)] &&
                       data.Items[data.GetIndex(endColumn + 1, row)] == itemId)
                {
                    endColumn++;
                }

                if (endColumn - column + 1 < 3) continue;

                for (int matchColumn = column; matchColumn <= endColumn; matchColumn++)
                    matches.Add(data.GetIndex(matchColumn, row));
            }
        }
    }

    private static void FindVerticalMatches(BoardData data, HashSet<int> matches)
    {
        for (int column = 0; column < data.Width; column++)
        {
            for (int row = 0; row < data.Height; row++)
            {
                int startIndex = data.GetIndex(column, row);
                if (!data.ActiveCells[startIndex] || string.IsNullOrEmpty(data.Items[startIndex]))
                    continue;

                string itemId = data.Items[startIndex];
                int endRow = row;
                while (endRow + 1 < data.Height &&
                       data.ActiveCells[data.GetIndex(column, endRow + 1)] &&
                       data.Items[data.GetIndex(column, endRow + 1)] == itemId)
                {
                    endRow++;
                }

                if (endRow - row + 1 < 3) continue;

                for (int matchRow = row; matchRow <= endRow; matchRow++)
                    matches.Add(data.GetIndex(column, matchRow));
            }
        }
    }
}
