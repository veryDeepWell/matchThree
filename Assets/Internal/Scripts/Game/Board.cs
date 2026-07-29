using UnityEngine;

[DefaultExecutionOrder(-70)]
public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private bool _useRandomLevel = false;
    [SerializeField] private LevelData _testLevel;
    [SerializeField] private ItemGenerator _itemGenerator;
    [SerializeField] private MatchesHandler _matchesHandler;

    public BoardData Data { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    // Теперь с сетерами
    public int Width { get; set; }
    public int Height { get; set; }
    public Item[,] Items { get; set; }

    private void Start()
    {
        if (_itemGenerator == null) _itemGenerator = FindObjectOfType<ItemGenerator>();
        if (_matchesHandler == null) _matchesHandler = FindObjectOfType<MatchesHandler>();

        if (_useRandomLevel)
        {
            Data = new BoardData(8, 8);
            Width = 8;
            Height = 8;
            Items = new Item[8, 8];
            _itemGenerator.GenerateItems(this);
            return;
        }

        if (_testLevel != null)
        {
            LoadLevel(_testLevel);
        }
        else
        {
            var fallback = ScriptableObject.CreateInstance<LevelData>();
            fallback.Initialize(8, 8);
            LoadLevel(fallback);
        }
    }

    public void LoadLevel(LevelData level)
    {
        CurrentLevel = level;
        Data = level.ToBoardData();
        Width = Data.Width;
        Height = Data.Height;
        Items = new Item[Width, Height];
        _itemGenerator?.GenerateItems(this);
    }

    public void LoadFromData(BoardData data)
    {
        if (data == null) return;
        
        Data = data;
        Width = data.Width;
        Height = data.Height;
        Items = new Item[Width, Height];
        _itemGenerator?.GenerateItems(this);
    }

    public bool IsCellActive(int x, int y) => Data?.IsActive(x, y) ?? false;
    public ItemTypes GetItemType(int x, int y) => Data?.GetItem(x, y) ?? ItemTypes.None;
    public void SetItemType(int x, int y, ItemTypes type) => Data?.SetItem(x, y, type);

    public void CheckMatches()
    {
        _matchesHandler?.ProcessMatches(this);
    }

    // ============================================================
    // SNAPSHOT SYSTEM - для сохранения состояния перед рекламой
    // ============================================================

    public BoardData CreateSnapshot()
    {
        if (Data == null)
        {
            Debug.LogWarning("Board Data is null, cannot create snapshot!");
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
            Debug.LogWarning("Snapshot is null, cannot restore!");
            return;
        }
        
        LoadFromData(snapshot);
    }
}