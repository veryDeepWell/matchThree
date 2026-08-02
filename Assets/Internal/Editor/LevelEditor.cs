using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class LevelEditorWindow : EditorWindow
{
    // ============ ДАННЫЕ ============
    private LevelData _currentLevel;
    private Vector2 _scrollPosition;
    private Vector2 _gridScrollPosition;

    // Выбранные ID
    private string _selectedItemId = "";
    private string _selectedSpecialItemId = "";
    private string _selectedSpecialCellId = "";

    // Списки для UI
    private List<string> _itemIds = new List<string>();
    private List<string> _specialItemIds = new List<string>();
    private List<string> _specialCellIds = new List<string>();

    // Спрайты (кешируются) — теперь храним спрайты, а не текстуры
    private Dictionary<string, Sprite> _itemSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _specialItemSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _specialCellSprites = new Dictionary<string, Sprite>();

    private Texture2D _missingTexture;
    private Texture2D _inactiveTexture;
    private Dictionary<string, Texture2D> _gridTextures = new Dictionary<string, Texture2D>();

    // Реестр
    private ItemRegistry _registry;

    // ============ MENU ============
    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    // ============ INIT ============
    private void OnEnable()
    {
        LoadRegistry();
        BuildItemLists();
        LoadSprites();
        LoadTextures();
    }

    private void LoadRegistry()
    {
        // Ищем ItemRegistry через AssetDatabase
        string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);
        }

        if (_registry == null)
        {
            Debug.LogError("ItemRegistry not found! Please create one.");
            return;
        }

        _registry.Initialize();
    }

    private void BuildItemLists()
    {
        _itemIds.Clear();
        _specialItemIds.Clear();
        _specialCellIds.Clear();

        if (_registry == null) return;

        foreach (var def in _registry.GetNormalItems())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id))
                _itemIds.Add(def.Id);
        }

        foreach (var def in _registry.GetSpecialItems())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id))
                _specialItemIds.Add(def.Id);
        }

        foreach (var def in _registry.GetSpecialCells())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id))
                _specialCellIds.Add(def.Id);
        }

        if (_itemIds.Count == 0) _itemIds.Add("");
        if (_specialItemIds.Count == 0) _specialItemIds.Add("");
        if (_specialCellIds.Count == 0) _specialCellIds.Add("");
    }

    // ============ ЗАГРУЗКА СПРАЙТОВ ============
    private void LoadSprites()
    {
        _itemSprites.Clear();
        foreach (string id in _itemIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var def = _registry?.Get(id);
            if (def != null && def.Icon != null)
                _itemSprites[id] = def.Icon;
        }

        _specialItemSprites.Clear();
        foreach (string id in _specialItemIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var def = _registry?.Get(id);
            if (def != null && def.Icon != null)
                _specialItemSprites[id] = def.Icon;
        }

        _specialCellSprites.Clear();
        foreach (string id in _specialCellIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var def = _registry?.Get(id);
            if (def != null && def.Icon != null)
                _specialCellSprites[id] = def.Icon;
        }
    }

    // ============ ТЕКСТУРЫ (для фона) ============
    private void LoadTextures()
    {
        _missingTexture = LoadTexture("missing", new Color(1f, 0.2f, 0.2f));
        _inactiveTexture = LoadTexture("inactive", new Color(0.1f, 0.1f, 0.1f));
    }

    private Texture2D LoadTexture(string path, Color fallbackColor)
    {
        string fullPath = $"Assets/Internal/Textures/{path}.psd";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
        if (tex != null) return tex;

        tex = Resources.Load<Texture2D>($"Textures/{path}");
        if (tex != null) return tex;

        return CreateFallbackTexture(fallbackColor);
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

    // ============ GUI ============
    private void OnGUI()
    {
        try
        {
            DrawLevelSelector();
            if (_currentLevel == null) return;

            EnsureLevelInitialized();
            DrawLevelSettings();
            DrawSelectedInfo();
            DrawPalette();
            DrawHelpBox();
            DrawGrid();
            DrawControls();
            DrawSaveButton();
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

    private void DrawLevelSelector()
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
        }
    }

    private void EnsureLevelInitialized()
    {
        if (_currentLevel.Items == null || _currentLevel.ActiveCells == null)
            _currentLevel.Initialize(_currentLevel.Width, _currentLevel.Height);
    }

    private void DrawLevelSettings()
    {
        EditorGUILayout.Space();
        int newWidth = EditorGUILayout.IntField("Width", _currentLevel.Width);
        int newHeight = EditorGUILayout.IntField("Height", _currentLevel.Height);
        if (newWidth != _currentLevel.Width || newHeight != _currentLevel.Height)
            ResizeGrid(newWidth, newHeight);
        EditorGUILayout.Space();
    }

    private void DrawSelectedInfo()
    {
        string itemName = string.IsNullOrEmpty(_selectedItemId) ? "None" : _selectedItemId;
        string specialName = string.IsNullOrEmpty(_selectedSpecialItemId) ? "None" : _selectedSpecialItemId;
        string cellName = string.IsNullOrEmpty(_selectedSpecialCellId) ? "None" : _selectedSpecialCellId;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected:", GUILayout.Width(60));
        EditorGUILayout.LabelField($"Item: {itemName}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Special: {specialName}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Cell: {cellName}");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawPalette()
    {
        DrawItemButtons();
        DrawSpecialItemButtons();
        DrawSpecialCellButtons();
    }

    private void DrawItemButtons()
    {
        EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        int buttonSize = 48;
        foreach (string id in _itemIds)
        {
            bool isSelected = (_selectedItemId == id);
            Sprite sprite = GetItemSprite(id);
            string label = string.IsNullOrEmpty(id) ? "" : id;
            if (label.Length > 4) label = label.Substring(0, 4);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = string.IsNullOrEmpty(_selectedSpecialItemId);

            if (DrawPaletteButton(sprite, label, isSelected, buttonSize))
            {
                _selectedItemId = id;
                if (!string.IsNullOrEmpty(id))
                    _selectedSpecialItemId = "";
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
        foreach (string id in _specialItemIds)
        {
            bool isSelected = (_selectedSpecialItemId == id);
            Sprite sprite = GetSpecialItemSprite(id);
            string label = string.IsNullOrEmpty(id) ? "None" : id;
            if (label.Length > 5) label = label.Substring(0, 5);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = string.IsNullOrEmpty(_selectedItemId);

            if (DrawPaletteButton(sprite, label, isSelected, buttonSize))
            {
                _selectedSpecialItemId = id;
                if (!string.IsNullOrEmpty(id))
                    _selectedItemId = "";
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
        foreach (string id in _specialCellIds)
        {
            bool isSelected = (_selectedSpecialCellId == id);
            Sprite sprite = GetSpecialCellSprite(id);
            string label = string.IsNullOrEmpty(id) ? "None" : id;
            if (label.Length > 6) label = label.Substring(0, 6);

            if (DrawPaletteButton(sprite, label, isSelected, buttonSize))
            {
                _selectedSpecialCellId = id;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }

    private Sprite GetItemSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _itemSprites.ContainsKey(id) ? _itemSprites[id] : null;
    }

    private Sprite GetSpecialItemSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _specialItemSprites.ContainsKey(id) ? _specialItemSprites[id] : null;
    }

    private Sprite GetSpecialCellSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _specialCellSprites.ContainsKey(id) ? _specialCellSprites[id] : null;
    }

    private bool DrawPaletteButton(Sprite sprite, string label, bool isSelected, int size)
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

        if (isSelected)
        {
            Rect borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
            EditorGUI.DrawRect(borderRect, Color.white);
        }

        // Используем спрайт напрямую как текстуру
        Texture2D tex = sprite != null ? sprite.texture : _missingTexture;
        GUIContent content = new GUIContent(tex);
        if (GUI.Button(rect, content, style))
            clicked = true;

        return clicked;
    }

    private void DrawHelpBox()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Left Click: Place selected | Right Click: Clear cell | Shift+Click: Full clear\n" +
            "Middle Click: Copy item | Shift+Middle Click: Copy full cell",
            MessageType.Info
        );
        EditorGUILayout.Space();
    }

    private void DrawGrid()
    {
        _gridScrollPosition = EditorGUILayout.BeginScrollView(_gridScrollPosition, GUILayout.ExpandHeight(true));

        if (_currentLevel == null) return;
        if (_currentLevel.Items == null || _currentLevel.ActiveCells == null)
        {
            _currentLevel.Initialize(_currentLevel.Width, _currentLevel.Height);
            return;
        }

        int width = _currentLevel.Width;
        int height = _currentLevel.Height;
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
                int idx = y * width + x;
                bool isActive = _currentLevel.ActiveCells != null && idx < _currentLevel.ActiveCells.Length && _currentLevel.ActiveCells[idx];
                string currentId = _currentLevel.Items != null && idx < _currentLevel.Items.Length ? _currentLevel.Items[idx] : "";

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
                else if (string.IsNullOrEmpty(currentId))
                    bgColor = new Color(0.25f, 0.25f, 0.25f);
                else
                    bgColor = new Color(0.35f, 0.35f, 0.35f);

                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize, style);
                EditorGUI.DrawRect(cellRect, bgColor);

                // Рисуем спрайт если есть
                Sprite sprite = GetItemSprite(currentId);
                Texture2D tex = sprite != null ? sprite.texture : _missingTexture;
                if (isActive && !string.IsNullOrEmpty(currentId) && tex != null && tex != _missingTexture)
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

        EditorGUILayout.EndScrollView();
    }

    private void HandleGridClick(int x, int y, bool isActive)
    {
        Event currentEvent = Event.current;

        if (currentEvent.button == 0 && !currentEvent.shift)
        {
            if (_selectedSpecialCellId == "inactive")
            {
                _currentLevel.SetActive(x, y, false);
                _currentLevel.SetItem(x, y, "");
                return;
            }

            if (isActive)
            {
                if (string.IsNullOrEmpty(_selectedItemId))
                    _currentLevel.SetItem(x, y, "");
                else
                    _currentLevel.SetItem(x, y, _selectedItemId);
            }
        }
        else if (currentEvent.button == 1)
        {
            _currentLevel.SetItem(x, y, "");
            _currentLevel.SetActive(x, y, true);
        }
        else if (currentEvent.button == 0 && currentEvent.shift)
        {
            _currentLevel.SetItem(x, y, "");
            _currentLevel.SetActive(x, y, true);
        }
        else if (currentEvent.button == 2 && !currentEvent.shift)
        {
            int idx = y * _currentLevel.Width + x;
            string id = _currentLevel.Items != null && idx < _currentLevel.Items.Length ? _currentLevel.Items[idx] : "";
            if (!string.IsNullOrEmpty(id))
            {
                _selectedItemId = id;
                _selectedSpecialItemId = "";
                _selectedSpecialCellId = "";
                Debug.Log($"Copied item '{id}' from ({x},{y})");
            }
        }
        else if (currentEvent.button == 2 && currentEvent.shift)
        {
            Debug.Log($"Full copy of cell ({x},{y})");
        }
    }

    private void DrawControls()
    {
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
    }

    private void DrawSaveButton()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SAVE", GUILayout.Height(40)))
            SaveLevel();
        GUI.backgroundColor = Color.white;
    }

    // ============ ОПЕРАЦИИ С УРОВНЕМ ============

    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Level", "Level_01.asset", "asset", "Choose where to save the level data");
        if (string.IsNullOrEmpty(path)) return;

        LevelData newLevel = CreateInstance<LevelData>();
        newLevel.Initialize(8, 8);

        var normalIds = _registry?.GetNormalItems().Select(d => d.Id).ToList() ?? new List<string>();
        if (normalIds.Count > 0)
        {
            for (int x = 0; x < newLevel.Width; x++)
                for (int y = 0; y < newLevel.Height; y++)
                    newLevel.SetItem(x, y, normalIds[Random.Range(0, normalIds.Count)]);
        }

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();
        _currentLevel = newLevel;
        EditorGUIUtility.PingObject(newLevel);
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return;

        int oldWidth = _currentLevel.Width;
        int oldHeight = _currentLevel.Height;
        var oldItems = _currentLevel.Items;
        var oldActive = _currentLevel.ActiveCells;

        string[] newItems = new string[newWidth * newHeight];
        bool[] newActive = new bool[newWidth * newHeight];

        int minWidth = Mathf.Min(newWidth, oldWidth);
        int minHeight = Mathf.Min(newHeight, oldHeight);

        for (int x = 0; x < minWidth; x++)
            for (int y = 0; y < minHeight; y++)
            {
                int oldIdx = y * oldWidth + x;
                int newIdx = y * newWidth + x;
                newActive[newIdx] = oldActive != null && oldIdx < oldActive.Length && oldActive[oldIdx];
                newItems[newIdx] = oldItems != null && oldIdx < oldItems.Length ? oldItems[oldIdx] : "";
            }

        for (int x = 0; x < newWidth; x++)
            for (int y = 0; y < newHeight; y++)
                if (x >= minWidth || y >= minHeight)
                {
                    int idx = y * newWidth + x;
                    newActive[idx] = true;
                    newItems[idx] = "";
                }

        _currentLevel.Width = newWidth;
        _currentLevel.Height = newHeight;
        _currentLevel.ActiveCells = newActive;
        _currentLevel.Items = newItems;
    }

    private void FillSelection()
    {
        if (_currentLevel == null) return;
        int w = _currentLevel.Width;
        int h = _currentLevel.Height;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int idx = y * w + x;
                if (_currentLevel.ActiveCells != null && idx < _currentLevel.ActiveCells.Length && _currentLevel.ActiveCells[idx])
                {
                    if (_currentLevel.Items != null && idx < _currentLevel.Items.Length && string.IsNullOrEmpty(_currentLevel.Items[idx]))
                        _currentLevel.SetItem(x, y, string.IsNullOrEmpty(_selectedItemId) ? "" : _selectedItemId);
                }
            }
    }

    private void FillRandom()
    {
        if (_currentLevel == null) return;
        var normalIds = _registry?.GetNormalItems().Select(d => d.Id).ToList() ?? new List<string>();
        if (normalIds.Count == 0) return;

        int w = _currentLevel.Width;
        int h = _currentLevel.Height;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int idx = y * w + x;
                if (_currentLevel.ActiveCells != null && idx < _currentLevel.ActiveCells.Length && _currentLevel.ActiveCells[idx])
                {
                    if (_currentLevel.Items != null && idx < _currentLevel.Items.Length && string.IsNullOrEmpty(_currentLevel.Items[idx]))
                        _currentLevel.SetItem(x, y, normalIds[Random.Range(0, normalIds.Count)]);
                }
            }
    }

    private void RemoveMatches()
    {
        if (_currentLevel == null) return;

        bool hasMatches = true;
        int maxAttempts = 100;
        int attempts = 0;
        var normalIds = _registry?.GetNormalItems().Select(d => d.Id).ToList() ?? new List<string>();

        while (hasMatches && attempts < maxAttempts)
        {
            attempts++;
            hasMatches = false;

            var tempData = _currentLevel.ToBoardData();
            var matches = MatchFinder.FindMatches(tempData);

            if (matches.Count > 0)
            {
                hasMatches = true;
                int w = _currentLevel.Width;
                foreach (int idx in matches)
                {
                    int x = idx % w;
                    int y = idx / w;
                    if (normalIds.Count > 0)
                        _currentLevel.SetItem(x, y, normalIds[Random.Range(0, normalIds.Count)]);
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
        for (int x = 0; x < _currentLevel.Width; x++)
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                _currentLevel.SetItem(x, y, "");
                _currentLevel.SetActive(x, y, true);
            }
    }
}