using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _currentLevel;
    private Vector2 _scrollPosition;
    private Vector2 _gridScrollPosition;
    
    // Выбранные индексы
    private int _selectedItemIndex = 0;
    private int _selectedSpecialItemIndex = 0;
    private int _selectedSpecialCellIndex = 0;
    
    // Данные для UI
    private List<ItemTypes> _itemTypes = new List<ItemTypes>();
    private List<SpecialItemTypes> _specialItemTypes = new List<SpecialItemTypes>();
    private List<string> _specialCellNames = new List<string>();
    
    // Текстуры
    private Dictionary<ItemTypes, Texture2D> _itemTextures = new Dictionary<ItemTypes, Texture2D>();
    private Dictionary<SpecialItemTypes, Texture2D> _specialItemTextures = new Dictionary<SpecialItemTypes, Texture2D>();
    private Dictionary<int, Texture2D> _specialCellTextures = new Dictionary<int, Texture2D>();
    private Texture2D _missingTexture;
    private Texture2D _inactiveTexture;
    
    // Кэш текстур для отображения в сетке
    private Dictionary<string, Texture2D> _gridTextures = new Dictionary<string, Texture2D>();
    
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }
    
    private void OnEnable()
    {
        BuildItemLists();
        LoadSprites();
    }
    
    private void LoadSprites()
    {
        string texturesPath = "Assets/Internal/Textures/";
        
        string missingPath = texturesPath + "missing.psd";
        if (File.Exists(missingPath))
            _missingTexture = LoadTextureFromAsset(missingPath);
        else
            _missingTexture = CreateFallbackTexture(new Color(1f, 0.2f, 0.2f));
        
        string inactivePath = texturesPath + "inactive.psd";
        if (File.Exists(inactivePath))
            _inactiveTexture = LoadTextureFromAsset(inactivePath);
        else
            _inactiveTexture = CreateFallbackTexture(new Color(0.1f, 0.1f, 0.1f));
        
        string itemFolder = texturesPath + "Items/";
        foreach (ItemTypes type in _itemTypes)
        {
            string path = itemFolder + type.ToString() + ".psd";
            _itemTextures[type] = File.Exists(path) ? LoadTextureFromAsset(path) : _missingTexture;
        }
        
        string specialFolder = texturesPath + "SpecialItems/";
        foreach (SpecialItemTypes type in _specialItemTypes)
        {
            string path = specialFolder + type.ToString() + ".psd";
            _specialItemTextures[type] = File.Exists(path) ? LoadTextureFromAsset(path) : _missingTexture;
        }
        
        string cellFolder = texturesPath + "SpecialCells/";
        for (int i = 0; i < _specialCellNames.Count; i++)
        {
            string path = cellFolder + _specialCellNames[i] + ".psd";
            _specialCellTextures[i] = File.Exists(path) ? LoadTextureFromAsset(path) : _missingTexture;
        }
    }
    
    private Texture2D LoadTextureFromAsset(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;
        
        string fileName = Path.GetFileNameWithoutExtension(path);
        tex = Resources.Load<Texture2D>("Textures/" + fileName);
        if (tex != null) return tex;
        
        return _missingTexture;
    }
    
    private Texture2D CreateFallbackTexture(Color color)
    {
        Texture2D tex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return tex;
    }
    
    private void BuildItemLists()
    {
        _itemTypes.Clear();
        _itemTypes.Add(ItemTypes.None);
        foreach (ItemTypes type in System.Enum.GetValues(typeof(ItemTypes)))
            if (type != ItemTypes.None && type != ItemTypes.Special)
                _itemTypes.Add(type);
        
        _specialItemTypes.Clear();
        _specialItemTypes.Add(SpecialItemTypes.None);
        foreach (SpecialItemTypes type in System.Enum.GetValues(typeof(SpecialItemTypes)))
            if (type != SpecialItemTypes.None)
                _specialItemTypes.Add(type);
        
        _specialCellNames.Clear();
        _specialCellNames.Add("None");
        _specialCellNames.Add("Inactive");
        _specialCellNames.Add("Ice");
        _specialCellNames.Add("Chain");
        _specialCellNames.Add("Stone");
        _specialCellNames.Add("Teleport");
        _specialCellNames.Add("Bonus");
    }
    
    private void OnGUI()
    {
        try
        {
            EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            _currentLevel = (LevelData)EditorGUILayout.ObjectField("Level Data", _currentLevel, typeof(LevelData), false);
            if (GUILayout.Button("Create New", GUILayout.Width(100)))
                CreateNewLevel();
            EditorGUILayout.EndHorizontal();
            
            if (_currentLevel == null)
            {
                EditorGUILayout.HelpBox("Select or create a Level Data asset", MessageType.Info);
                return;
            }
            
            if (_currentLevel.items == null || _currentLevel.activeCells == null)
                _currentLevel.Initialize(_currentLevel.width, _currentLevel.height);
            
            EditorGUILayout.Space();
            
            int newWidth = EditorGUILayout.IntField("Width", _currentLevel.width);
            int newHeight = EditorGUILayout.IntField("Height", _currentLevel.height);
            if (newWidth != _currentLevel.width || newHeight != _currentLevel.height)
                ResizeGrid(newWidth, newHeight);
            
            EditorGUILayout.Space();
            
            // ============ ТЕКСТ ВЫБРАННОГО ============
            DrawSelectedInfo();
            
            // ============ ТРИ РЯДА КНОПОК ============
            DrawItemButtons();
            DrawSpecialItemButtons();
            DrawSpecialCellButtons();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Left Click: Place selected | Right Click: Clear cell | Shift+Click: Full clear\n" +
                "Middle Click: Copy item | Shift+Middle Click: Copy full cell",
                MessageType.Info
            );
            EditorGUILayout.Space();
            
            _gridScrollPosition = EditorGUILayout.BeginScrollView(_gridScrollPosition, GUILayout.ExpandHeight(true));
            DrawGrid();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Are you sure you want to clear all cells?", "Yes", "Cancel"))
                    ClearGrid();
            }
            
            if (GUILayout.Button("Fill Selection", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Fill Selection", "Fill all empty active cells with selected item?", "Yes", "Cancel"))
                    FillSelection();
            }
            
            if (GUILayout.Button("Fill Random", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Fill Random", "Fill all empty active cells with random items?", "Yes", "Cancel"))
                    FillRandom();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Matches", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Remove Matches", "Remove all initial matches from the grid?", "Yes", "Cancel"))
                    RemoveMatches();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("SAVE", GUILayout.Height(40)))
                SaveLevel();
            GUI.backgroundColor = Color.white;
        }
        catch (ExitGUIException)
        {
            throw;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Level Editor Error: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void DrawSelectedInfo()
    {
        string itemName = _selectedItemIndex == 0 ? "None" : _itemTypes[_selectedItemIndex].ToString();
        string specialName = _selectedSpecialItemIndex == 0 ? "None" : _specialItemTypes[_selectedSpecialItemIndex].ToString();
        string cellName = _specialCellNames[_selectedSpecialCellIndex];
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected:", GUILayout.Width(60));
        EditorGUILayout.LabelField($"Item: {itemName}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Special: {specialName}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Cell: {cellName}");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }
    
    private void DrawItemButtons()
    {
        EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        int buttonSize = 48;
        foreach (ItemTypes type in _itemTypes)
        {
            bool isSelected = (_selectedItemIndex == _itemTypes.IndexOf(type));
            Texture2D tex = _itemTextures.ContainsKey(type) ? _itemTextures[type] : _missingTexture;
            string label = type == ItemTypes.None ? "" : type.ToString().Replace("Dot", "");
            if (label.Length > 4) label = label.Substring(0, 4);
            
            bool wasEnabled = GUI.enabled;
            // Если выбран спец предмет (не None) - блокируем обычные предметы
            GUI.enabled = (_selectedSpecialItemIndex == 0);
            
            if (DrawPaletteButton(tex, label, isSelected, buttonSize))
            {
                _selectedItemIndex = _itemTypes.IndexOf(type);
                // Если выбрали None - разблокируем спец предметы, иначе сбрасываем спец предмет
                if (type != ItemTypes.None)
                    _selectedSpecialItemIndex = 0;
            }
            
            GUI.enabled = wasEnabled;
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }
    
    private void DrawSpecialItemButtons()
    {
        EditorGUILayout.LabelField("Special Items", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        int buttonSize = 48;
        for (int i = 0; i < _specialItemTypes.Count; i++)
        {
            SpecialItemTypes type = _specialItemTypes[i];
            bool isSelected = (_selectedSpecialItemIndex == i);
            bool isNone = (type == SpecialItemTypes.None);
            
            Texture2D tex;
            if (isNone)
                tex = _itemTextures[ItemTypes.None];
            else if (_specialItemTextures.ContainsKey(type))
                tex = _specialItemTextures[type];
            else
                tex = _missingTexture;
            
            string label = type == SpecialItemTypes.None ? "None" : type.ToString();
            if (label.Length > 5) label = label.Substring(0, 5);
            
            bool wasEnabled = GUI.enabled;
            // Special Items доступны ТОЛЬКО если выбран None в Items
            GUI.enabled = (_selectedItemIndex == 0);
            
            if (DrawPaletteButton(tex, label, isSelected, buttonSize))
            {
                _selectedSpecialItemIndex = i;
                // Если выбрали спец предмет (не None) - сбрасываем обычный предмет
                if (type != SpecialItemTypes.None)
                    _selectedItemIndex = 0;
            }
            
            GUI.enabled = wasEnabled;
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }
    
    private void DrawSpecialCellButtons()
    {
        EditorGUILayout.LabelField("Special Cells", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        int buttonSize = 48;
        for (int i = 0; i < _specialCellNames.Count; i++)
        {
            bool isSelected = (_selectedSpecialCellIndex == i);
            Texture2D tex;
            if (i == 0)
                tex = _itemTextures[ItemTypes.None];
            else if (i == 1)
                tex = _inactiveTexture;
            else if (_specialCellTextures.ContainsKey(i))
                tex = _specialCellTextures[i];
            else
                tex = _missingTexture;
            
            string label = _specialCellNames[i];
            if (label.Length > 6) label = label.Substring(0, 6);
            
            if (DrawPaletteButton(tex, label, isSelected, buttonSize))
            {
                _selectedSpecialCellIndex = i;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }
    
    private bool DrawPaletteButton(Texture2D texture, string label, bool isSelected, int size)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.padding = new RectOffset(2, 2, 2, 2);
        style.margin = new RectOffset(2, 2, 2, 2);
        style.fixedWidth = size;
        style.fixedHeight = size;
        style.fontSize = 8;
        style.alignment = TextAnchor.MiddleCenter;
    
        bool clicked = false;
        Rect rect = GUILayoutUtility.GetRect(size, size, style);
    
        // Рисуем рамку если выделено
        if (isSelected)
        {
            Rect borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
            EditorGUI.DrawRect(borderRect, Color.white);
        }
    
        // Рисуем кнопку
        GUIContent content = new GUIContent(texture);
        if (GUI.Button(rect, content, style))
            clicked = true;
    
        return clicked;
    }
    
    private void DrawGrid()
    {
        if (_currentLevel == null) return;
        if (_currentLevel.activeCells == null || _currentLevel.items == null)
        {
            _currentLevel.Initialize(_currentLevel.width, _currentLevel.height);
            return;
        }
        
        int width = _currentLevel.width;
        int height = _currentLevel.height;
        float cellSize = 58f;
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        for (int x = 0; x < width; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(cellSize));
        EditorGUILayout.EndHorizontal();
        
        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), GUILayout.Width(25));
            
            for (int x = 0; x < width; x++)
            {
                bool isActive = _currentLevel.IsActive(x, y);
                ItemTypes currentType = _currentLevel.GetItem(x, y);
                
                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.padding = new RectOffset(0, 0, 0, 0);
                style.margin = new RectOffset(1, 1, 1, 1);
                style.fixedWidth = cellSize;
                style.fixedHeight = cellSize;
                style.fontSize = 8;
                style.alignment = TextAnchor.MiddleCenter;
                
                Color bgColor;
                if (!isActive)
                    bgColor = new Color(0.08f, 0.08f, 0.08f);
                else if (currentType == ItemTypes.None)
                    bgColor = new Color(0.25f, 0.25f, 0.25f);
                else
                    bgColor = new Color(0.35f, 0.35f, 0.35f);
                
                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize, style);
                EditorGUI.DrawRect(cellRect, bgColor);
                
                Texture2D tex = GetGridTexture(currentType);
                if (isActive && currentType != ItemTypes.None && tex != null && tex != _missingTexture)
                {
                    float padding = 4f;
                    GUI.DrawTexture(new Rect(cellRect.x + padding, cellRect.y + padding, cellSize - padding * 2, cellSize - padding * 2), tex, ScaleMode.ScaleToFit);
                }
                
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    HandleGridClick(x, y, isActive);
                    Event.current.Use();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private Texture2D GetGridTexture(ItemTypes type)
    {
        string key = type.ToString();
        if (_gridTextures.ContainsKey(key))
            return _gridTextures[key];
        
        Texture2D tex = null;
        if (_itemTextures.ContainsKey(type))
            tex = _itemTextures[type];
        
        _gridTextures[key] = tex;
        return tex;
    }
    
    private void HandleGridClick(int x, int y, bool isActive)
    {
        Event currentEvent = Event.current;
        
        if (currentEvent.button == 0 && !currentEvent.shift)
        {
            // Если выбрана Inactive ячейка - отключаем ячейку
            if (_selectedSpecialCellIndex == 1) // Inactive
            {
                _currentLevel.activeCells[x, y] = false;
                _currentLevel.SetItem(x, y, ItemTypes.None);
                return;
            }
            
            if (isActive)
            {
                // Ставим предмет
                if (_selectedItemIndex == 0)
                    _currentLevel.SetItem(x, y, ItemTypes.None);
                else
                    _currentLevel.SetItem(x, y, _itemTypes[_selectedItemIndex]);
            }
        }
        else if (currentEvent.button == 1)
        {
            _currentLevel.SetItem(x, y, ItemTypes.None);
            _currentLevel.activeCells[x, y] = true;
        }
        else if (currentEvent.button == 0 && currentEvent.shift)
        {
            _currentLevel.SetItem(x, y, ItemTypes.None);
            _currentLevel.activeCells[x, y] = true;
        }
        else if (currentEvent.button == 2 && !currentEvent.shift)
        {
            ItemTypes type = _currentLevel.GetItem(x, y);
            int index = _itemTypes.IndexOf(type);
            if (index >= 0)
            {
                _selectedItemIndex = index;
                _selectedSpecialItemIndex = 0;
                _selectedSpecialCellIndex = 0;
                Debug.Log($"Copied item from ({x},{y})");
            }
        }
        else if (currentEvent.button == 2 && currentEvent.shift)
        {
            Debug.Log($"Full copy of cell ({x},{y})");
        }
    }
    
    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Level", "Level_01.asset", "asset", "Choose where to save the level data");
        if (string.IsNullOrEmpty(path)) return;
        
        LevelData newLevel = CreateInstance<LevelData>();
        newLevel.Initialize(8, 8);
        for (int x = 0; x < newLevel.width; x++)
            for (int y = 0; y < newLevel.height; y++)
                newLevel.SetItem(x, y, (ItemTypes)Random.Range(1, 7));
        
        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();
        _currentLevel = newLevel;
        EditorGUIUtility.PingObject(newLevel);
    }
    
    private void ResizeGrid(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return;
        
        int oldWidth = _currentLevel.width;
        int oldHeight = _currentLevel.height;
        bool[,] oldActive = _currentLevel.activeCells;
        ItemTypes[,] oldItems = _currentLevel.items;
        
        bool[,] newActive = new bool[newWidth, newHeight];
        ItemTypes[,] newItems = new ItemTypes[newWidth, newHeight];
        
        int minWidth = Mathf.Min(newWidth, oldWidth);
        int minHeight = Mathf.Min(newHeight, oldHeight);
        
        for (int x = 0; x < minWidth; x++)
            for (int y = 0; y < minHeight; y++)
            {
                newActive[x, y] = oldActive[x, y];
                newItems[x, y] = oldItems[x, y];
            }
        
        for (int x = 0; x < newWidth; x++)
            for (int y = 0; y < newHeight; y++)
                if (x >= minWidth || y >= minHeight)
                {
                    newActive[x, y] = true;
                    newItems[x, y] = (ItemTypes)Random.Range(1, 7);
                }
        
        _currentLevel.width = newWidth;
        _currentLevel.height = newHeight;
        _currentLevel.activeCells = newActive;
        _currentLevel.items = newItems;
    }
    
    private void FillSelection()
    {
        if (_currentLevel == null) return;
        for (int x = 0; x < _currentLevel.width; x++)
            for (int y = 0; y < _currentLevel.height; y++)
                if (_currentLevel.IsActive(x, y) && _currentLevel.GetItem(x, y) == ItemTypes.None)
                    _currentLevel.SetItem(x, y, _selectedItemIndex == 0 ? ItemTypes.None : _itemTypes[_selectedItemIndex]);
    }
    
    private void FillRandom()
    {
        if (_currentLevel == null) return;
        for (int x = 0; x < _currentLevel.width; x++)
            for (int y = 0; y < _currentLevel.height; y++)
                if (_currentLevel.IsActive(x, y) && _currentLevel.GetItem(x, y) == ItemTypes.None)
                    _currentLevel.SetItem(x, y, (ItemTypes)Random.Range(1, 7));
    }
    
    private void RemoveMatches()
    {
        if (_currentLevel == null) return;
        
        bool hasMatches = true;
        int maxAttempts = 100;
        int attempts = 0;
        
        while (hasMatches && attempts < maxAttempts)
        {
            attempts++;
            hasMatches = false;
            
            for (int x = 0; x < _currentLevel.width - 2; x++)
                for (int y = 0; y < _currentLevel.height; y++)
                {
                    if (!_currentLevel.IsActive(x, y)) continue;
                    ItemTypes type = _currentLevel.GetItem(x, y);
                    if (type == ItemTypes.None) continue;
                    
                    if (_currentLevel.IsActive(x + 1, y) && _currentLevel.IsActive(x + 2, y) &&
                        _currentLevel.GetItem(x + 1, y) == type && _currentLevel.GetItem(x + 2, y) == type)
                    {
                        hasMatches = true;
                        _currentLevel.SetItem(x, y, (ItemTypes)Random.Range(1, 7));
                        _currentLevel.SetItem(x + 1, y, (ItemTypes)Random.Range(1, 7));
                        _currentLevel.SetItem(x + 2, y, (ItemTypes)Random.Range(1, 7));
                    }
                }
            
            for (int x = 0; x < _currentLevel.width; x++)
                for (int y = 0; y < _currentLevel.height - 2; y++)
                {
                    if (!_currentLevel.IsActive(x, y)) continue;
                    ItemTypes type = _currentLevel.GetItem(x, y);
                    if (type == ItemTypes.None) continue;
                    
                    if (_currentLevel.IsActive(x, y + 1) && _currentLevel.IsActive(x, y + 2) &&
                        _currentLevel.GetItem(x, y + 1) == type && _currentLevel.GetItem(x, y + 2) == type)
                    {
                        hasMatches = true;
                        _currentLevel.SetItem(x, y, (ItemTypes)Random.Range(1, 7));
                        _currentLevel.SetItem(x, y + 1, (ItemTypes)Random.Range(1, 7));
                        _currentLevel.SetItem(x, y + 2, (ItemTypes)Random.Range(1, 7));
                    }
                }
        }
        
        Debug.Log($"Matches removed after {attempts} attempts");
    }
    
    private void SaveLevel()
    {
        if (_currentLevel == null) return;
        EditorUtility.SetDirty(_currentLevel);
        AssetDatabase.SaveAssets();
        Debug.Log($"Level saved: {_currentLevel.name}");
    }
    
    private void ClearGrid()
    {
        if (_currentLevel == null) return;
        for (int x = 0; x < _currentLevel.width; x++)
            for (int y = 0; y < _currentLevel.height; y++)
            {
                _currentLevel.SetItem(x, y, ItemTypes.None);
                _currentLevel.activeCells[x, y] = true;
            }
    }
}