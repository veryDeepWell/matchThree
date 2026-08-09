using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int Width = 8;
    public int Height = 8;

    public string[] Items;
    public bool[] ActiveCells;
    public string[] SpecialItems;
    public int[] SpecialCells;
    public LevelGoalData GoalData;

    public BoardData ToBoardData()
    {
        var data = new BoardData(Width, Height);
        int cellCount = Width * Height;

        CopyIfValid(Items, data.Items, cellCount);
        CopyIfValid(ActiveCells, data.ActiveCells, cellCount);
        CopyIfValid(SpecialItems, data.SpecialItems, cellCount);
        CopyIfValid(SpecialCells, data.SpecialCells, cellCount);
        return data;
    }

    public void FromBoardData(BoardData data)
    {
        if (data == null || !data.IsStructurallyValid()) return;

        Width = data.Width;
        Height = data.Height;
        int cellCount = Width * Height;

        Items = new string[cellCount];
        ActiveCells = new bool[cellCount];
        SpecialItems = new string[cellCount];
        SpecialCells = new int[cellCount];

        System.Array.Copy(data.Items, Items, cellCount);
        System.Array.Copy(data.ActiveCells, ActiveCells, cellCount);
        System.Array.Copy(data.SpecialItems, SpecialItems, cellCount);
        System.Array.Copy(data.SpecialCells, SpecialCells, cellCount);
    }

    public void EnsureArrays()
    {
        int cellCount = Mathf.Max(1, Width * Height);
        Items = EnsureStringArray(Items, cellCount);
        ActiveCells = EnsureBoolArray(ActiveCells, cellCount);
        SpecialItems = EnsureStringArray(SpecialItems, cellCount);
        SpecialCells = EnsureIntArray(SpecialCells, cellCount);
    }

    public void Initialize(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);

        int cellCount = Width * Height;
        Items = new string[cellCount];
        ActiveCells = new bool[cellCount];
        SpecialItems = new string[cellCount];
        SpecialCells = new int[cellCount];

        for (int index = 0; index < cellCount; index++)
            ActiveCells[index] = true;
    }



    public bool IsActive(int column, int row)
    {
        if (!IsValid(column, row) || ActiveCells == null)
            return false;

        int index = row * Width + column;
        return index < ActiveCells.Length && ActiveCells[index];
    }

    public string GetItem(int column, int row)
    {
        if (!IsValid(column, row) || Items == null)
            return string.Empty;

        int index = row * Width + column;
        return index < Items.Length ? Items[index] ?? string.Empty : string.Empty;
    }

    public void SetItem(int column, int row, string itemId)
    {
        if (!IsValid(column, row) || Items == null)
            return;

        int index = row * Width + column;
        if (index < Items.Length)
            Items[index] = itemId ?? string.Empty;
    }

    public void SetActive(int column, int row, bool active)
    {
        if (!IsValid(column, row) || ActiveCells == null)
            return;

        int index = row * Width + column;
        if (index < ActiveCells.Length)
            ActiveCells[index] = active;
    }

    private static string[] EnsureStringArray(string[] source, int length)
    {
        var result = new string[length];
        if (source != null)
            System.Array.Copy(source, result, Mathf.Min(source.Length, length));
        return result;
    }

    private static bool[] EnsureBoolArray(bool[] source, int length)
    {
        var result = new bool[length];
        for (int index = 0; index < length; index++)
            result[index] = true;

        if (source != null)
            System.Array.Copy(source, result, Mathf.Min(source.Length, length));
        return result;
    }

    private static int[] EnsureIntArray(int[] source, int length)
    {
        var result = new int[length];
        if (source != null)
            System.Array.Copy(source, result, Mathf.Min(source.Length, length));
        return result;
    }

    private bool IsValid(int column, int row)
    {
        return column >= 0 && column < Width && row >= 0 && row < Height;
    }

    private static void CopyIfValid<T>(T[] source, T[] destination, int expectedLength)
    {
        if (source != null && source.Length == expectedLength)
            System.Array.Copy(source, destination, expectedLength);
    }
}
