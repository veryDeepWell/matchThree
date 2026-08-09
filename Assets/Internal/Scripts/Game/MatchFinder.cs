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

                if (!IsMatchableCell(data, startIndex))
                    continue;

                string itemId = data.Items[startIndex];
                int endColumn = column;

                while (endColumn + 1 < data.Width)
                {
                    int nextIndex = data.GetIndex(endColumn + 1, row);
                    if (!IsMatchableCell(data, nextIndex) ||
                        data.Items[nextIndex] != itemId)
                    {
                        break;
                    }

                    endColumn++;
                }

                if (endColumn - column + 1 < 3)
                    continue;

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

                if (!IsMatchableCell(data, startIndex))
                    continue;

                string itemId = data.Items[startIndex];
                int endRow = row;

                while (endRow + 1 < data.Height)
                {
                    int nextIndex = data.GetIndex(column, endRow + 1);
                    if (!IsMatchableCell(data, nextIndex) ||
                        data.Items[nextIndex] != itemId)
                    {
                        break;
                    }

                    endRow++;
                }

                if (endRow - row + 1 < 3)
                    continue;

                for (int matchRow = row; matchRow <= endRow; matchRow++)
                    matches.Add(data.GetIndex(column, matchRow));
            }
        }
    }

    private static bool IsMatchableCell(BoardData data, int index)
    {
        // Items under special cells (ice, vine, etc.) must be matchable so that
        // matching them can free the cell temporarily / damage its health.
        // Special cells themselves are not swapped by the player (see MatchValidator
        // and Item.TrySwipe), but their occupants still participate in matches.
        return data.ActiveCells[index] &&
               !string.IsNullOrEmpty(data.Items[index]);
    }
}
