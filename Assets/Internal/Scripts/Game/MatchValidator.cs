public static class MatchValidator
{
    public static bool HasPossibleMoves(BoardData data)
    {
        if (data == null || !data.IsStructurallyValid())
            return false;

        for (int column = 0; column < data.Width; column++)
        {
            for (int row = 0; row < data.Height; row++)
            {
                int cellIndex = data.GetIndex(column, row);
                if (!data.ActiveCells[cellIndex] ||
                    !string.IsNullOrEmpty(data.SpecialItems[cellIndex]) ||
                    string.IsNullOrEmpty(data.Items[cellIndex]))
                    continue;

                if (CanSwapAndMatch(data, column, row, column + 1, row) ||
                    CanSwapAndMatch(data, column, row, column, row + 1))
                    return true;
            }
        }

        return false;
    }

    private static bool CanSwapAndMatch(BoardData data, int firstColumn, int firstRow, int secondColumn, int secondRow)
    {
        if (!data.IsValid(firstColumn, firstRow) || !data.IsValid(secondColumn, secondRow))
            return false;

        int firstIndex = data.GetIndex(firstColumn, firstRow);
        int secondIndex = data.GetIndex(secondColumn, secondRow);
        if (!data.ActiveCells[firstIndex] || !data.ActiveCells[secondIndex])
            return false;
        if (!string.IsNullOrEmpty(data.SpecialItems[firstIndex]) ||
            !string.IsNullOrEmpty(data.SpecialItems[secondIndex]) ||
            string.IsNullOrEmpty(data.Items[secondIndex]))
            return false;

        string firstItemId = data.Items[firstIndex];
        string secondItemId = data.Items[secondIndex];
        data.Items[firstIndex] = secondItemId;
        data.Items[secondIndex] = firstItemId;

        bool createsMatch = MatchFinder.FindMatches(data).Count > 0;

        data.Items[firstIndex] = firstItemId;
        data.Items[secondIndex] = secondItemId;
        return createsMatch;
    }
}
