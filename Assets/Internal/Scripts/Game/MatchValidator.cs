public static class MatchValidator
{
    public static bool HasPossibleMoves(BoardData data)
    {
        return TryFindPossibleMove(data, out _, out _);
    }

    public static bool TryFindPossibleMove(BoardData data, out (int x, int y) first, out (int x, int y) second)
    {
        first = (-1, -1);
        second = (-1, -1);

        if (data == null || !data.IsStructurallyValid())
            return false;

        for (int column = 0; column < data.Width; column++)
        {
            for (int row = 0; row < data.Height; row++)
            {
                if (!IsSwappableCell(data, column, row))
                    continue;

                if (CanSwapAndMatch(data, column, row, column + 1, row))
                {
                    first = (column, row);
                    second = (column + 1, row);
                    return true;
                }

                if (CanSwapAndMatch(data, column, row, column, row + 1))
                {
                    first = (column, row);
                    second = (column, row + 1);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CanSwapAndMatch(
        BoardData data,
        int firstColumn,
        int firstRow,
        int secondColumn,
        int secondRow)
    {
        if (!data.IsValid(firstColumn, firstRow) || !data.IsValid(secondColumn, secondRow))
            return false;

        if (!IsSwappableCell(data, firstColumn, firstRow) ||
            !IsSwappableCell(data, secondColumn, secondRow))
            return false;

        int firstIndex = data.GetIndex(firstColumn, firstRow);
        int secondIndex = data.GetIndex(secondColumn, secondRow);

        string firstItemId = data.Items[firstIndex];
        string secondItemId = data.Items[secondIndex];

        data.Items[firstIndex] = secondItemId;
        data.Items[secondIndex] = firstItemId;

        bool createsMatch = MatchFinder.FindMatches(data).Count > 0;

        data.Items[firstIndex] = firstItemId;
        data.Items[secondIndex] = secondItemId;

        return createsMatch;
    }

    private static bool IsSwappableCell(BoardData data, int column, int row)
    {
        if (!data.IsValid(column, row))
            return false;

        int index = data.GetIndex(column, row);

        if (!data.ActiveCells[index] ||
            string.IsNullOrEmpty(data.Items[index]) ||
            !string.IsNullOrEmpty(data.SpecialItems[index]))
            return false;

        // A special cell is part of the board state, but its occupant cannot
        // participate in normal player matching unless the cell explicitly
        // allows player swapping.
        if (data.SpecialCells[index] > 0)
            return false;

        return true;
    }
}
