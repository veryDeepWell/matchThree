using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    
    [SerializeField] private bool[] _activeCellsFlat;
    [SerializeField] private ItemTypes[] _itemsFlat;
    
    public bool[,] activeCells
    {
        get
        {
            EnsureInitialized();
            bool[,] result = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = _activeCellsFlat[y * width + x];
            return result;
        }
        set
        {
            _activeCellsFlat = new bool[width * height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _activeCellsFlat[y * width + x] = value[x, y];
        }
    }
    
    public ItemTypes[,] items
    {
        get
        {
            EnsureInitialized();
            ItemTypes[,] result = new ItemTypes[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    result[x, y] = _itemsFlat[y * width + x];
            return result;
        }
        set
        {
            _itemsFlat = new ItemTypes[width * height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _itemsFlat[y * width + x] = value[x, y];
        }
    }
    
    public void Initialize(int w, int h)
    {
        width = w;
        height = h;
        _activeCellsFlat = new bool[w * h];
        _itemsFlat = new ItemTypes[w * h];
        
        for (int i = 0; i < w * h; i++)
        {
            _activeCellsFlat[i] = true;
            _itemsFlat[i] = ItemTypes.None;
        }
    }
    
    private void EnsureInitialized()
    {
        if (_activeCellsFlat == null || _itemsFlat == null || _activeCellsFlat.Length == 0)
        {
            Initialize(width, height);
        }
    }
    
    public bool IsActive(int column, int row)
    {
        EnsureInitialized();
        if (column < 0 || column >= width || row < 0 || row >= height)
            return false;
        return _activeCellsFlat[row * width + column];
    }
    
    public ItemTypes GetItem(int column, int row)
    {
        EnsureInitialized();
        if (!IsActive(column, row))
            return ItemTypes.None;
        return _itemsFlat[row * width + column];
    }
    
    public void SetItem(int column, int row, ItemTypes type)
    {
        EnsureInitialized();
        if (IsActive(column, row))
            _itemsFlat[row * width + column] = type;
    }
    
    public void SetActive(int column, int row, bool active)
    {
        EnsureInitialized();
        if (column >= 0 && column < width && row >= 0 && row < height)
            _activeCellsFlat[row * width + column] = active;
    }
}