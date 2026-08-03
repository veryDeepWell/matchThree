using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private bool _useRandomLevel = false;
    [SerializeField] private LevelData _testLevel;
    [SerializeField] private ItemGenerator _itemGenerator;
    [SerializeField] private MatchesHandler _matchesHandler;

    [Header("Position")]
    [SerializeField] private Vector2 _offset = Vector2.zero;
    [SerializeField] private float _cellSize = 1f;

    public BoardData Data { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public Item[,] Items { get; set; }
    public SpecialCell[,] SpecialCells { get; set; }

    private int _lastSwapX = -1;
    private int _lastSwapY = -1;
    
    private int _bombTriggerX = -1;
    private int _bombTriggerY = -1;

    public void SetBombTriggerPosition(int x, int y)
    {
        _bombTriggerX = x;
        _bombTriggerY = y;
    }

    public (int x, int y) GetBombTriggerPosition()
    {
        return (_bombTriggerX, _bombTriggerY);
    }

    public void ClearBombTriggerPosition()
    {
        _bombTriggerX = -1;
        _bombTriggerY = -1;
    }

    public void ForceLoadLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[Board] Level is null!");
            return;
        }
        LoadLevel(level);
    }

    public Vector2 GetWorldPosition(int column, int row)
    {
        return new Vector2(column * _cellSize, row * _cellSize) + _offset;
    }

    public void GetGridPosition(Vector2 worldPos, out int column, out int row)
    {
        Vector2 local = worldPos - _offset;
        column = Mathf.RoundToInt(local.x / _cellSize);
        row = Mathf.RoundToInt(local.y / _cellSize);
    }

    public Vector2 GetCellSize() => Vector2.one * _cellSize;

    public void LoadLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[Board] Cannot load null level!");
            return;
        }

        CurrentLevel = level;
        Data = level.ToBoardData();
        Width = Data.Width;
        Height = Data.Height;
        Items = new Item[Width, Height];
        SpecialCells = new SpecialCell[Width, Height];

        if (_itemGenerator == null)
            _itemGenerator = FindObjectOfType<ItemGenerator>();

        if (_itemGenerator != null)
            _itemGenerator.GenerateItems(this);
        else
            Debug.LogError("[Board] ItemGenerator is null!");
    }

    public void LoadFromData(BoardData data)
    {
        if (data == null) return;

        Data = data;
        Width = data.Width;
        Height = data.Height;
        Items = new Item[Width, Height];
        SpecialCells = new SpecialCell[Width, Height];
        _itemGenerator?.GenerateItems(this);
    }

    public bool IsCellActive(int x, int y)
    {
        if (Data == null) return false;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return Data.IsActive(x, y);
    }

    public string GetItemId(int x, int y)
    {
        if (Data == null) return "";
        if (x < 0 || x >= Width || y < 0 || y >= Height) return "";
        return Data.GetItem(x, y);
    }

    public void SetItemId(int x, int y, string id)
    {
        if (Data == null) return;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        Data.SetItem(x, y, id);
    }

    public SpecialCell GetSpecialCell(int x, int y)
    {
        if (SpecialCells == null) return null;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        return SpecialCells[x, y];
    }

    public void SetSpecialCell(int x, int y, SpecialCell cell)
    {
        if (SpecialCells == null) return;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        SpecialCells[x, y] = cell;
    }

    public void CheckMatches(int swapX, int swapY)
    {
        _lastSwapX = swapX;
        _lastSwapY = swapY;
        _matchesHandler?.ProcessMatches(this);
    }

    public void CheckMatches()
    {
        _matchesHandler?.ProcessMatches(this);
    }

    public (int x, int y) GetLastSwapPosition()
    {
        return (_lastSwapX, _lastSwapY);
    }

    public void ClearLastSwapPosition()
    {
        _lastSwapX = -1;
        _lastSwapY = -1;
    }

    public BoardData CreateSnapshot()
    {
        if (Data == null)
        {
            Debug.LogWarning("[Board] Data is null, cannot create snapshot!");
            return null;
        }

        var data = Data;
        var snapshot = new BoardData(data.Width, data.Height);

        System.Array.Copy(data.Items, snapshot.Items, data.Items.Length);
        System.Array.Copy(data.ActiveCells, snapshot.ActiveCells, data.ActiveCells.Length);
        System.Array.Copy(data.SpecialItems, snapshot.SpecialItems, data.SpecialItems.Length);
        System.Array.Copy(data.SpecialCells, snapshot.SpecialCells, data.SpecialCells.Length);

        return snapshot;
    }

    public void RestoreSnapshot(BoardData snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("[Board] Snapshot is null, cannot restore!");
            return;
        }
        LoadFromData(snapshot);
    }

    private void OnDrawGizmosSelected()
    {
        if (Data == null) return;

        Gizmos.color = Color.green;
        for (int x = 0; x < Data.Width; x++)
        {
            for (int y = 0; y < Data.Height; y++)
            {
                Vector2 pos = GetWorldPosition(x, y);
                Gizmos.DrawWireCube(pos, Vector3.one * _cellSize * 0.9f);
            }
        }
    }
}