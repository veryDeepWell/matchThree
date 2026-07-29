using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int Width = 8;
    public int Height = 8;

    // Публичные поля для прямой сериализации
    public ItemTypes[] Items;
    public bool[] ActiveCells;
    public SpecialItemTypes[] SpecialItems;
    public int[] SpecialCells;

    public BoardData ToBoardData()
    {
        var data = new BoardData(Width, Height);
        int total = Width * Height;

        if (Items != null && Items.Length == total)
            System.Array.Copy(Items, data.Items, total);
        if (ActiveCells != null && ActiveCells.Length == total)
            System.Array.Copy(ActiveCells, data.ActiveCells, total);
        if (SpecialItems != null && SpecialItems.Length == total)
            System.Array.Copy(SpecialItems, data.SpecialItems, total);
        if (SpecialCells != null && SpecialCells.Length == total)
            System.Array.Copy(SpecialCells, data.SpecialCells, total);

        return data;
    }

    public void FromBoardData(BoardData data)
    {
        Width = data.Width;
        Height = data.Height;
        int total = Width * Height;

        Items = new ItemTypes[total];
        ActiveCells = new bool[total];
        SpecialItems = new SpecialItemTypes[total];
        SpecialCells = new int[total];

        System.Array.Copy(data.Items, Items, total);
        System.Array.Copy(data.ActiveCells, ActiveCells, total);
        System.Array.Copy(data.SpecialItems, SpecialItems, total);
        System.Array.Copy(data.SpecialCells, SpecialCells, total);
    }

    public void Initialize(int w, int h)
    {
        Width = w;
        Height = h;
        int total = w * h;
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

    public bool IsActive(int x, int y)
    {
        if (ActiveCells == null) return false;
        int idx = y * Width + x;
        if (idx < 0 || idx >= ActiveCells.Length) return false;
        return ActiveCells[idx];
    }

    public ItemTypes GetItem(int x, int y)
    {
        if (Items == null) return ItemTypes.None;
        int idx = y * Width + x;
        if (idx < 0 || idx >= Items.Length) return ItemTypes.None;
        return Items[idx];
    }

    public void SetItem(int x, int y, ItemTypes type)
    {
        if (Items == null) return;
        int idx = y * Width + x;
        if (idx >= 0 && idx < Items.Length)
            Items[idx] = type;
    }

    public void SetActive(int x, int y, bool active)
    {
        if (ActiveCells == null) return;
        int idx = y * Width + x;
        if (idx >= 0 && idx < ActiveCells.Length)
            ActiveCells[idx] = active;
    }
}