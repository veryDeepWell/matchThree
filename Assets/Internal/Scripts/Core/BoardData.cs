using System;

[Serializable]
public class BoardData
{
    public int Width;
    public int Height;
    public string[] Items;          // ID предметов
    public bool[] ActiveCells;
    public string[] SpecialItems;   // ID спец-предметов
    public int[] SpecialCells;      // индексы спец-ячеек

    public BoardData(int width, int height)
    {
        Width = width;
        Height = height;
        int total = width * height;
        Items = new string[total];
        ActiveCells = new bool[total];
        SpecialItems = new string[total];
        SpecialCells = new int[total];

        for (int i = 0; i < total; i++)
        {
            ActiveCells[i] = true;
            Items[i] = "";
            SpecialItems[i] = "";
            SpecialCells[i] = 0;
        }
    }

    public int GetIndex(int x, int y) => y * Width + x;
    public bool IsValid(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    public bool IsActive(int x, int y) => IsValid(x, y) && ActiveCells[GetIndex(x, y)];
    
    public string GetItem(int x, int y) => IsValid(x, y) ? Items[GetIndex(x, y)] : "";
    public void SetItem(int x, int y, string id) { if (IsValid(x, y)) Items[GetIndex(x, y)] = id; }
    
    public void SetActive(int x, int y, bool active) { if (IsValid(x, y)) ActiveCells[GetIndex(x, y)] = active; }

    public string GetSpecialItem(int x, int y) => IsValid(x, y) ? SpecialItems[GetIndex(x, y)] : "";
    public void SetSpecialItem(int x, int y, string id) { if (IsValid(x, y)) SpecialItems[GetIndex(x, y)] = id; }

    public int GetSpecialCell(int x, int y) => IsValid(x, y) ? SpecialCells[GetIndex(x, y)] : 0;
    public void SetSpecialCell(int x, int y, int value) { if (IsValid(x, y)) SpecialCells[GetIndex(x, y)] = value; }

    public bool IsStructurallyValid()
    {
        if (Width <= 0 || Height <= 0)
            return false;

        int total = Width * Height;
        return Items != null && Items.Length == total &&
               ActiveCells != null && ActiveCells.Length == total &&
               SpecialItems != null && SpecialItems.Length == total &&
               SpecialCells != null && SpecialCells.Length == total;
    }

    public BoardData Clone()
    {
        if (!IsStructurallyValid())
            return null;

        var clone = new BoardData(Width, Height);
        Array.Copy(Items, clone.Items, Items.Length);
        Array.Copy(ActiveCells, clone.ActiveCells, ActiveCells.Length);
        Array.Copy(SpecialItems, clone.SpecialItems, SpecialItems.Length);
        Array.Copy(SpecialCells, clone.SpecialCells, SpecialCells.Length);
        return clone;
    }
}
