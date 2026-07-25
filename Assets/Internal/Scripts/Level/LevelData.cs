using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    public bool[,] activeCells;
    public ItemTypes[,] items;
    
    public void Initialize(int w, int h)
    {
        width = w;
        height = h;
        activeCells = new bool[w, h];
        items = new ItemTypes[w, h];
        
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            activeCells[x, y] = true;
    }
    
    private void EnsureInitialized()
    {
        if (items == null || activeCells == null)
            Initialize(width, height);
    }
    
    public bool IsActive(int column, int row)
    {
        EnsureInitialized();
        if (column < 0 || column >= width || row < 0 || row >= height)
            return false;
        return activeCells[column, row];
    }
    
    public ItemTypes GetItem(int column, int row)
    {
        EnsureInitialized();
        if (!IsActive(column, row))
            return ItemTypes.None;
        return items[column, row];
    }
    
    public void SetItem(int column, int row, ItemTypes type)
    {
        EnsureInitialized();
        if (IsActive(column, row))
            items[column, row] = type;
    }
}