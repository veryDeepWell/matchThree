using UnityEngine;

[DefaultExecutionOrder(-70)]
public class Board : MonoBehaviour
{
    [Header("=== Board Settings ===")]
    [SerializeField] private bool useRandomLevel;
    [SerializeField] private LevelData testLevel;

    [Header("=== Grid Info (Read Only) ===")]
    public int width;
    public int height;
    public bool[,] ActiveCells;
    public Item[,] AllItems;
    
    private Administrator _administrator;
    public LevelData currentLevel;

    void Start()
    {
        _administrator = FindAnyObjectByType<Administrator>();
        if (_administrator == null)
        {
            Debug.LogError("Administrator not found!");
            return;
        }
        
        // Создаем массив
        AllItems = new Item[width, height];
        
        // Инициализируем генератор
        if (_administrator.itemGenerator != null)
        {
            _administrator.itemGenerator.Initialization();
        }
        
        // Определяем какой уровень загружать
        LevelData levelToLoad = null;
        
        if (useRandomLevel)
        {
            Debug.Log("Random mode enabled");
            CreateDefaultBoard();
            // Генерируем предметы через генератор
            if (_administrator?.itemGenerator != null)
            {
                _administrator.itemGenerator.GetItems();
            }
            return;
        }
        else if (testLevel != null)
        {
            levelToLoad = testLevel;
            Debug.Log($"Loading test level: {testLevel.name}");
        }
        else if (_administrator.levelManager != null)
        {
            levelToLoad = _administrator.levelManager.LoadLevel(0);
        }
        
        if (levelToLoad != null)
        {
            LoadLevel(levelToLoad);
        }
        else
        {
            Debug.LogWarning("No level to load, creating default board!");
            CreateDefaultBoard();
            if (_administrator?.itemGenerator != null)
            {
                _administrator.itemGenerator.GetItems();
            }
        }
    }

    private void CreateDefaultBoard()
    {
        width = 8;
        height = 8;
        ActiveCells = new bool[width, height];
        AllItems = new Item[width, height];
        
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            ActiveCells[x, y] = true;
    }

    public void LoadLevel(LevelData level)
    {
        if (level == null) 
        {
            Debug.LogError("Cannot load null level!");
            return;
        }
        
        currentLevel = level;
        width = level.width;
        height = level.height;
        
        ActiveCells = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                ActiveCells[x, y] = level.IsActive(x, y);
            }
        }
        
        AllItems = new Item[width, height];
        
        if (_administrator?.itemGenerator != null)
        {
            _administrator.itemGenerator.Initialization();
            _administrator.itemGenerator.GetItems();
        }
        else
        {
            Debug.LogError("ItemGenerator is null in Board.LoadLevel!");
        }
    }

    public bool IsActiveCell(int column, int row)
    {
        if (ActiveCells == null)
        {
            if (currentLevel != null)
                LoadLevel(currentLevel);
            else
                return false;
        }
        
        if (column < 0 || column >= width || row < 0 || row >= height)
            return false;
        
        return ActiveCells[column, row];
    }

    public void CheckMatches()
    {
        if (_administrator?.matchesHandler != null)
            _administrator.matchesHandler.ProcessMatches();
    }
}