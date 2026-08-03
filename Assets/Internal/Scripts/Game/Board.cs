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

    public void CheckMatches()
    {
        if (_matchesHandler == null)
            _matchesHandler = FindObjectOfType<MatchesHandler>();
        _matchesHandler?.ProcessMatches(this);
    }

    public BoardData CreateSnapshot()
    {
        if (Data == null)
        {
            Debug.LogWarning("[Board] Data is null, cannot create snapshot!");
            return null;
        }

        return Data.Clone();
    }

    public void RestoreSnapshot(BoardData snapshot, LevelData sourceLevel = null)
    {
        if (snapshot == null || !snapshot.IsStructurallyValid())
        {
            Debug.LogWarning("[Board] Snapshot is null or invalid, cannot restore!");
            return;
        }

        CurrentLevel = sourceLevel;
        LoadFromData(snapshot.Clone());
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
