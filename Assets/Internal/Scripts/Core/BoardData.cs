using System;

[Serializable]
public class BoardData
{
    public int Width;
    public int Height;
    public ItemTypes[] Items;
    public bool[] ActiveCells;
    public SpecialItemTypes[] SpecialItems;
    public int[] SpecialCells; // 0 = none, 1 = ice, 2 = chain и т.д.

    public BoardData(int width, int height)
    {
        Width = width;
        Height = height;
        int total = width * height;
        Items = new ItemTypes[total];
        ActiveCells = new bool[total];
        SpecialItems = new SpecialItemTypes[total];
        SpecialCells = new int[total];

        for (int i = 0; i < total; i++)
        {
            ActiveCells[i] = true;
            Items[i] = ItemTypes.None;
            SpecialItems[i] = SpecialItemTypes.None;
            SpecialCells[i] = 0;
        }
    }

    public int GetIndex(int x, int y) => y * Width + x;
    public bool IsValid(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    public bool IsActive(int x, int y) => IsValid(x, y) && ActiveCells[GetIndex(x, y)];
    public ItemTypes GetItem(int x, int y) => IsValid(x, y) ? Items[GetIndex(x, y)] : ItemTypes.None;
    public void SetItem(int x, int y, ItemTypes type) { if (IsValid(x, y)) Items[GetIndex(x, y)] = type; }
    public void SetActive(int x, int y, bool active) { if (IsValid(x, y)) ActiveCells[GetIndex(x, y)] = active; }

    public SpecialItemTypes GetSpecialItem(int x, int y) => IsValid(x, y) ? SpecialItems[GetIndex(x, y)] : SpecialItemTypes.None;
    public void SetSpecialItem(int x, int y, SpecialItemTypes type) { if (IsValid(x, y)) SpecialItems[GetIndex(x, y)] = type; }

    public int GetSpecialCell(int x, int y) => IsValid(x, y) ? SpecialCells[GetIndex(x, y)] : 0;
    public void SetSpecialCell(int x, int y, int value) { if (IsValid(x, y)) SpecialCells[GetIndex(x, y)] = value; }
}