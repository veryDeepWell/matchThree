using System;

[Serializable]
public class BoardData
{
    public int Width;
    public int Height;
    public string[] Items;
    public bool[] ActiveCells;
    public string[] SpecialItems;
    public int[] SpecialCells;

    public BoardData(int width, int height)
    {
        Width = width;
        Height = height;

        int cellCount = Math.Max(0, width * height);
        Items = new string[cellCount];
        ActiveCells = new bool[cellCount];
        SpecialItems = new string[cellCount];
        SpecialCells = new int[cellCount];

        for (int index = 0; index < cellCount; index++)
            ActiveCells[index] = true;
    }

    public int GetIndex(int column, int row) => row * Width + column;

    public bool IsValid(int column, int row)
    {
        return column >= 0 && column < Width && row >= 0 && row < Height;
    }

    public bool IsActive(int column, int row)
    {
        return IsValid(column, row) && ActiveCells[GetIndex(column, row)];
    }

    public string GetItem(int column, int row)
    {
        return IsValid(column, row) ? Items[GetIndex(column, row)] ?? "" : "";
    }

    public void SetItem(int column, int row, string itemId)
    {
        if (IsValid(column, row))
            Items[GetIndex(column, row)] = itemId ?? "";
    }

    public void SetActive(int column, int row, bool active)
    {
        if (IsValid(column, row))
            ActiveCells[GetIndex(column, row)] = active;
    }

    public string GetSpecialItem(int column, int row)
    {
        return IsValid(column, row) ? SpecialItems[GetIndex(column, row)] ?? "" : "";
    }

    public void SetSpecialItem(int column, int row, string specialItemId)
    {
        if (IsValid(column, row))
            SpecialItems[GetIndex(column, row)] = specialItemId ?? "";
    }

    public int GetSpecialCell(int column, int row)
    {
        return IsValid(column, row) ? SpecialCells[GetIndex(column, row)] : 0;
    }

    public void SetSpecialCell(int column, int row, int typeIndex)
    {
        if (IsValid(column, row))
            SpecialCells[GetIndex(column, row)] = Math.Max(0, typeIndex);
    }

    public bool IsStructurallyValid()
    {
        if (Width <= 0 || Height <= 0) return false;

        int cellCount = Width * Height;
        return Items != null && Items.Length == cellCount &&
               ActiveCells != null && ActiveCells.Length == cellCount &&
               SpecialItems != null && SpecialItems.Length == cellCount &&
               SpecialCells != null && SpecialCells.Length == cellCount;
    }

    public BoardData Clone()
    {
        if (!IsStructurallyValid()) return null;

        var clone = new BoardData(Width, Height);
        Array.Copy(Items, clone.Items, Items.Length);
        Array.Copy(ActiveCells, clone.ActiveCells, ActiveCells.Length);
        Array.Copy(SpecialItems, clone.SpecialItems, SpecialItems.Length);
        Array.Copy(SpecialCells, clone.SpecialCells, SpecialCells.Length);
        return clone;
    }
}
