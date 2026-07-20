using UnityEngine;
using UnityEditor;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _currentLevel;
    private int _selectedItemIndex = 0;
    private Vector2 _scrollPosition;
    
    private readonly string[] _itemNames = { "🔴 Red", "🔵 Blue", "🟢 Green", "🩵 Cyan", "🟣 Magenta", "🟡 Yellow" };
    private readonly Color[] _itemColors = { Color.red, Color.blue, Color.green, Color.cyan, Color.magenta, Color.yellow };
    
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Выбор или создание уровня
        EditorGUILayout.BeginHorizontal();
        _currentLevel = (LevelData)EditorGUILayout.ObjectField("Level Data", _currentLevel, typeof(LevelData), false);
        
        if (GUILayout.Button("Create New", GUILayout.Width(100)))
        {
            CreateNewLevel();
        }
        EditorGUILayout.EndHorizontal();
        
        if (_currentLevel == null)
        {
            EditorGUILayout.HelpBox("Select or create a Level Data asset", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space();
        
        // Параметры уровня
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Width", _currentLevel.width);
        int newHeight = EditorGUILayout.IntField("Height", _currentLevel.height);
        
        if (EditorGUI.EndChangeCheck())
        {
            ResizeGrid(newWidth, newHeight);
        }
        
        EditorGUILayout.Space();
        
        // Выбор предмета для рисования
        EditorGUILayout.LabelField("Select Item", EditorStyles.boldLabel);
        _selectedItemIndex = EditorGUILayout.Popup(_selectedItemIndex, _itemNames);
        EditorGUILayout.Space();
        
        // Отрисовка сетки
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawGrid();
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // Кнопки сохранения
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            SaveLevel();
        }
        
        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            ClearGrid();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level",
            "Level_01.asset",
            "asset",
            "Choose where to save the level data"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            LevelData newLevel = CreateInstance<LevelData>();
            newLevel.width = 8;
            newLevel.height = 8;
            newLevel.items = new ItemTypes[64];
            
            // Заполняем случайными
            for (int i = 0; i < newLevel.items.Length; i++)
            {
                newLevel.items[i] = (ItemTypes)Random.Range(0, 6);
            }
            
            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();
            _currentLevel = newLevel;
            EditorGUIUtility.PingObject(newLevel);
        }
    }
    
    private void ResizeGrid(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return;
        
        ItemTypes[] newItems = new ItemTypes[newWidth * newHeight];
        
        // Копируем старые данные
        int minWidth = Mathf.Min(newWidth, _currentLevel.width);
        int minHeight = Mathf.Min(newHeight, _currentLevel.height);
        
        for (int y = 0; y < minHeight; y++)
        {
            for (int x = 0; x < minWidth; x++)
            {
                int oldIndex = y * _currentLevel.width + x;
                int newIndex = y * newWidth + x;
                newItems[newIndex] = _currentLevel.items[oldIndex];
            }
        }
        
        // Заполняем новые ячейки
        for (int i = 0; i < newItems.Length; i++)
        {
            if (newItems[i] == ItemTypes.Special)
                newItems[i] = (ItemTypes)Random.Range(0, 6);
        }
        
        _currentLevel.width = newWidth;
        _currentLevel.height = newHeight;
        _currentLevel.items = newItems;
    }
    
    private void DrawGrid()
    {
        int width = _currentLevel.width;
        int height = _currentLevel.height;
        
        float cellSize = 50f;
        float spacing = 4f;
        
        // Заголовки колонок
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        for (int x = 0; x < width; x++)
        {
            GUILayout.Label(x.ToString(), GUILayout.Width(cellSize));
        }
        EditorGUILayout.EndHorizontal();
        
        // Сетка
        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Заголовок строки
            GUILayout.Label(y.ToString(), GUILayout.Width(25));
            
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                ItemTypes currentType = _currentLevel.items[index];
                Color bgColor = _itemColors[(int)currentType % _itemColors.Length];
                
                // Рисуем кнопку-ячейку
                GUI.backgroundColor = bgColor;
                
                string label = currentType.ToString().Replace("Dot", "");
                if (label.Length > 3) label = label.Substring(0, 3);
                
                if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    // По клику меняем предмет
                    if (Event.current.button == 0) // ЛКМ
                    {
                        _currentLevel.items[index] = (ItemTypes)_selectedItemIndex;
                    }
                    else if (Event.current.button == 1) // ПКМ - стираем
                    {
                        _currentLevel.items[index] = (ItemTypes)0;
                    }
                }
                
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void SaveLevel()
    {
        EditorUtility.SetDirty(_currentLevel);
        AssetDatabase.SaveAssets();
        Debug.Log($"Level saved: {_currentLevel.name}");
    }
    
    private void ClearGrid()
    {
        for (int i = 0; i < _currentLevel.items.Length; i++)
        {
            _currentLevel.items[i] = (ItemTypes)0;
        }
    }
}