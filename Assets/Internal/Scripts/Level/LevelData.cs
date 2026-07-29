using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    
    [SerializeField] private bool[] activeCellsFlat;
    [SerializeField] private ItemTypes[] itemsFlat;
    
    public bool[,] ActiveCells
    {
        get
        {
            EnsureInitialized();
            bool[,] result = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = activeCellsFlat[y * width + x];
            return result;
        }
        set
        {
            activeCellsFlat = new bool[width * height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    activeCellsFlat[y * width + x] = value[x, y];
        }
    }
    
    public ItemTypes[,] Items
    {
        get
        {
            EnsureInitialized();
            ItemTypes[,] result = new ItemTypes[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = itemsFlat[y * width + x];
            return result;
        }
        set
        {
            itemsFlat = new ItemTypes[width * height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    itemsFlat[y * width + x] = value[x, y];
        }
    }
    
    public void Initialize(int w, int h)
    {
        width = w;
        height = h;
        activeCellsFlat = new bool[w * h];
        itemsFlat = new ItemTypes[w * h];
        
        for (int i = 0; i < w * h; i++)
        {
            activeCellsFlat[i] = true;
            itemsFlat[i] = ItemTypes.None;
        }
    }
    
    private void EnsureInitialized()
    {
        if (activeCellsFlat == null || itemsFlat == null || activeCellsFlat.Length == 0)
        {
            Initialize(width, height);
        }
    }
    
    public bool IsActive(int column, int row)
    {
        EnsureInitialized();
        if (column < 0 || column >= width || row < 0 || row >= height)
            return false;
        return activeCellsFlat[row * width + column];
    }
    
    public ItemTypes GetItem(int column, int row)
    {
        EnsureInitialized();
        if (!IsActive(column, row))
            return ItemTypes.None;
        return itemsFlat[row * width + column];
    }
    
    public void SetItem(int column, int row, ItemTypes type)
    {
        EnsureInitialized();
        if (IsActive(column, row))
            itemsFlat[row * width + column] = type;
    }
    
    public void SetActive(int column, int row, bool active)
    {
        EnsureInitialized();
        if (column >= 0 && column < width && row >= 0 && row < height)
            activeCellsFlat[row * width + column] = active;
    }
}