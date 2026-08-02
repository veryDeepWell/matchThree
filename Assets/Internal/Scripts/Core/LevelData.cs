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

        Items = new string[total];
        ActiveCells = new bool[total];
        SpecialItems = new string[total];
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

    public bool IsActive(int x, int y)
    {
        if (ActiveCells == null) return false;
        int idx = y * Width + x;
        if (idx < 0 || idx >= ActiveCells.Length) return false;
        return ActiveCells[idx];
    }

    public string GetItem(int x, int y)
    {
        if (Items == null) return "";
        int idx = y * Width + x;
        if (idx < 0 || idx >= Items.Length) return "";
        return Items[idx];
    }

    public void SetItem(int x, int y, string id)
    {
        if (Items == null) return;
        int idx = y * Width + x;
        if (idx >= 0 && idx < Items.Length)
            Items[idx] = id;
    }

    public void SetActive(int x, int y, bool active)
    {
        if (ActiveCells == null) return;
        int idx = y * Width + x;
        if (idx >= 0 && idx < ActiveCells.Length)
            ActiveCells[idx] = active;
    }
}