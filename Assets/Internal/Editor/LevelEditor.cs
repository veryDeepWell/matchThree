using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _currentLevel;
    private Vector2 _gridScrollPosition;

    private string _selectedItemId = "";
    private string _selectedSpecialItemId = "";
    private string _selectedSpecialCellId = "";

    private List<string> _itemIds = new List<string>();
    private List<string> _specialItemIds = new List<string>();
    private List<string> _specialCellIds = new List<string>();

    private Dictionary<string, Sprite> _itemSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _specialItemSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _specialCellSprites = new Dictionary<string, Sprite>();

    private Texture2D _missingTexture;
    private ItemRegistry _registry;
    private SpecialCellHandler _cellHandler;

    private GUIStyle _paletteButtonStyle;
    private GUIStyle _gridCellStyle;

    private const int PaletteButtonSize = 48;
    private const float GridCellSize = 58f;

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnEnable()
    {
        LoadRegistry();
        LoadCellHandler();
        BuildItemLists();
        LoadSprites();
        LoadTextures();
    }

    private void EnsureStyles()
    {
        if (Event.current == null) return;

        if (_paletteButtonStyle == null)
        {
            _paletteButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(2, 2, 2, 2),
                fixedWidth = PaletteButtonSize,
                fixedHeight = PaletteButtonSize,
                fontSize = 8,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (_gridCellStyle == null)
        {
            _gridCellStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(1, 1, 1, 1),
                fixedWidth = GridCellSize,
                fixedHeight = GridCellSize
            };
        }
    }

    private void LoadRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);
        }

        if (_registry == null)
        {
            Debug.LogError("ItemRegistry not found!");
            return;
        }

        _registry.Initialize();
    }

    private void LoadCellHandler()
    {
        _cellHandler = FindFirstObjectByType<SpecialCellHandler>();
        if (_cellHandler != null) return;

        string[] guids = AssetDatabase.FindAssets("t:SpecialCellHandler");
        if (guids.Length == 0) return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
            _cellHandler = prefab.GetComponent<SpecialCellHandler>();
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

        _specialCellIds.Insert(0, "inactive");
        _specialCellIds.Insert(0, "");
    }

    private void LoadSprites()
    {
        _itemSprites.Clear();
        _specialItemSprites.Clear();
        _specialCellSprites.Clear();

        if (_registry == null) return;

        foreach (var def in _registry.GetNormalItems())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id) && def.Icon != null)
                _itemSprites[def.Id] = def.Icon;
        }

        foreach (var def in _registry.GetSpecialItems())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id) && def.Icon != null)
                _specialItemSprites[def.Id] = def.Icon;
        }

        foreach (var def in _registry.GetSpecialCells())
        {
            if (def != null && !string.IsNullOrEmpty(def.Id) && def.Icon != null)
                _specialCellSprites[def.Id] = def.Icon;
        }

        Debug.Log($"Loaded: Items={_itemSprites.Count}, Special={_specialItemSprites.Count}, Cells={_specialCellSprites.Count}");
    }

    private void LoadTextures()
    {
        _missingTexture = CreateFallbackTexture(new Color(1f, 0.2f, 0.2f));
    }

    private static Texture2D CreateFallbackTexture(Color color)
    {
        Texture2D tex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return tex;
    }

    private static void DrawSprite(Rect position, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;

        if (tex.width <= 0 || tex.height <= 0 || tr.width <= 0 || tr.height <= 0)
            return;

        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(position, tex, uv);
    }

    private void OnGUI()
    {
        try
        {
            EnsureStyles();

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
            EditorGUILayout.HelpBox("Select or create a Level Data asset", MessageType.Info);
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

        foreach (string id in _itemIds)
        {
            bool isSelected = (_selectedItemId == id);
            Sprite sprite = GetItemSprite(id);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = string.IsNullOrEmpty(_selectedSpecialItemId);

            if (DrawPaletteButton(sprite, isSelected))
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

        foreach (string id in _specialItemIds)
        {
            bool isSelected = (_selectedSpecialItemId == id);
            Sprite sprite = GetSpecialItemSprite(id);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = string.IsNullOrEmpty(_selectedItemId);

            if (DrawPaletteButton(sprite, isSelected))
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

        foreach (string id in _specialCellIds)
        {
            bool isSelected = (_selectedSpecialCellId == id);

            if (id == "inactive")
            {
                bool clicked = GUILayout.Button("", GUILayout.Width(PaletteButtonSize), GUILayout.Height(PaletteButtonSize));
                if (clicked)
                    _selectedSpecialCellId = id;

                Rect rect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                Handles.color = Color.red;
                Handles.DrawLine(new Vector3(rect.x + 4, rect.y + 4), new Vector3(rect.x + rect.width - 4, rect.y + rect.height - 4));
                Handles.DrawLine(new Vector3(rect.x + rect.width - 4, rect.y + 4), new Vector3(rect.x + 4, rect.y + rect.height - 4));

                if (isSelected)
                {
                    Rect borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
                    EditorGUI.DrawRect(borderRect, Color.white);
                }
                continue;
            }

            if (string.IsNullOrEmpty(id)) continue;

            Sprite sprite = GetSpecialCellSprite(id);
            if (DrawPaletteButton(sprite, isSelected))
                _selectedSpecialCellId = id;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }

    private Sprite GetItemSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _itemSprites.TryGetValue(id, out var sprite) ? sprite : null;
    }

    private Sprite GetSpecialItemSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _specialItemSprites.TryGetValue(id, out var sprite) ? sprite : null;
    }

    private Sprite GetSpecialCellSprite(string id)
    {
        if (string.IsNullOrEmpty(id) || id == "inactive") return null;
        return _specialCellSprites.TryGetValue(id, out var sprite) ? sprite : null;
    }

    private bool DrawPaletteButton(Sprite sprite, bool isSelected)
    {
        Rect rect = GUILayoutUtility.GetRect(PaletteButtonSize, PaletteButtonSize, _paletteButtonStyle);

        if (isSelected)
        {
            Rect borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
            EditorGUI.DrawRect(borderRect, Color.white);
        }

        if (sprite != null)
        {
            float padding = 4f;
            Rect spriteRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2,
                rect.height - padding * 2
            );
            DrawSprite(spriteRect, sprite);
        }
        else if (_missingTexture != null)
        {
            GUI.DrawTexture(rect, _missingTexture, ScaleMode.ScaleToFit);
        }

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            return true;
        }

        return false;
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

        if (_currentLevel == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_currentLevel.Items == null || _currentLevel.ActiveCells == null)
        {
            _currentLevel.Initialize(_currentLevel.Width, _currentLevel.Height);
            EditorGUILayout.EndScrollView();
            return;
        }

        int width = _currentLevel.Width;
        int height = _currentLevel.Height;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(30);
        for (int x = 0; x < width; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(GridCellSize));
        EditorGUILayout.EndHorizontal();

        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), GUILayout.Width(25));

            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                bool isActive = idx < _currentLevel.ActiveCells.Length && _currentLevel.ActiveCells[idx];
                string currentId = idx < _currentLevel.Items.Length ? _currentLevel.Items[idx] : "";
                int specialCellType = _currentLevel.SpecialCells != null && idx < _currentLevel.SpecialCells.Length
                    ? _currentLevel.SpecialCells[idx]
                    : 0;

                Color bgColor;
                if (!isActive)
                    bgColor = new Color(0.08f, 0.08f, 0.08f);
                else if (specialCellType > 0)
                    bgColor = new Color(0.2f, 0.3f, 0.5f, 0.6f);
                else if (string.IsNullOrEmpty(currentId))
                    bgColor = new Color(0.25f, 0.25f, 0.25f);
                else
                    bgColor = new Color(0.35f, 0.35f, 0.35f);

                Rect cellRect = GUILayoutUtility.GetRect(GridCellSize, GridCellSize, _gridCellStyle);
                EditorGUI.DrawRect(cellRect, bgColor);

                // Спец-ячейка - используем спрайт из _specialCellSprites
                if (isActive && specialCellType > 0)
                {
                    string cellId = GetSpecialCellIdByIndex(specialCellType);
                    if (!string.IsNullOrEmpty(cellId))
                    {
                        Sprite sprite = GetSpecialCellSprite(cellId);
                        if (sprite != null)
                        {
                            float padding = 2f;
                            Rect spriteRect = new Rect(
                                cellRect.x + padding,
                                cellRect.y + padding,
                                GridCellSize - padding * 2,
                                GridCellSize - padding * 2
                            );
                            DrawSprite(spriteRect, sprite);
                        }
                    }
                }

                // Обычный предмет
                if (isActive && !string.IsNullOrEmpty(currentId))
                {
                    Sprite sprite = GetItemSprite(currentId);
                    if (sprite != null)
                    {
                        float padding = 4f;
                        Rect spriteRect = new Rect(
                            cellRect.x + padding,
                            cellRect.y + padding,
                            GridCellSize - padding * 2,
                            GridCellSize - padding * 2
                        );
                        DrawSprite(spriteRect, sprite);
                    }
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

    private string GetSpecialCellIdByIndex(int index)
    {
        if (_cellHandler == null || _registry == null) return "";
        
        var cellData = _cellHandler.GetCellData(index);
        if (cellData == null) return "";
        
        // Ищем ItemDefinition, у которого CellData совпадает
        foreach (var def in _registry.GetSpecialCells())
        {
            if (def != null && def.CellData == cellData)
            {
                return def.Id;
            }
        }
        
        return "";
    }

    private SpecialCellData GetSpecialCellDataByIndex(int index)
    {
        if (_cellHandler == null) return null;
        return _cellHandler.GetCellData(index);
    }

    // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С LevelData ============

    private bool IsValidCell(int x, int y)
    {
        if (_currentLevel == null) return false;
        if (x < 0 || x >= _currentLevel.Width) return false;
        if (y < 0 || y >= _currentLevel.Height) return false;
        return true;
    }

    private int GetIndex(int x, int y)
    {
        return y * _currentLevel.Width + x;
    }

    private bool GetCellActive(int x, int y)
    {
        if (!IsValidCell(x, y)) return false;
        int idx = GetIndex(x, y);
        return idx < _currentLevel.ActiveCells.Length && _currentLevel.ActiveCells[idx];
    }

    private void SetCellActive(int x, int y, bool active)
    {
        if (!IsValidCell(x, y)) return;
        int idx = GetIndex(x, y);
        if (idx < _currentLevel.ActiveCells.Length)
            _currentLevel.ActiveCells[idx] = active;
    }

    private string GetCellItem(int x, int y)
    {
        if (!IsValidCell(x, y)) return "";
        int idx = GetIndex(x, y);
        if (idx < _currentLevel.Items.Length)
            return _currentLevel.Items[idx];
        return "";
    }

    private void SetCellItem(int x, int y, string id)
    {
        if (!IsValidCell(x, y)) return;
        int idx = GetIndex(x, y);
        if (idx < _currentLevel.Items.Length)
            _currentLevel.Items[idx] = id;
    }

    private int GetCellSpecialCell(int x, int y)
    {
        if (!IsValidCell(x, y)) return 0;
        int idx = GetIndex(x, y);
        if (_currentLevel.SpecialCells != null && idx < _currentLevel.SpecialCells.Length)
            return _currentLevel.SpecialCells[idx];
        return 0;
    }

    private void SetCellSpecialCell(int x, int y, int value)
    {
        if (!IsValidCell(x, y)) return;
        int idx = GetIndex(x, y);
        if (_currentLevel.SpecialCells != null && idx < _currentLevel.SpecialCells.Length)
            _currentLevel.SpecialCells[idx] = value;
    }

    // ============ ОБРАБОТЧИК КЛИКОВ ============

    private void HandleGridClick(int x, int y, bool isActive)
    {
        Event currentEvent = Event.current;

        if (currentEvent.button == 0 && !currentEvent.shift)
        {
            if (_selectedSpecialCellId == "inactive")
            {
                SetCellActive(x, y, false);
                SetCellItem(x, y, "");
                SetCellSpecialCell(x, y, 0);
                return;
            }

            if (!string.IsNullOrEmpty(_selectedSpecialCellId) && _selectedSpecialCellId != "inactive")
            {
                if (!isActive)
                    SetCellActive(x, y, true);

                var def = _registry.Get(_selectedSpecialCellId);
                if (def != null && def.Category == ItemCategory.SpecialCell && _cellHandler != null)
                {
                    var allData = _cellHandler.GetAllCellData();
                    int index = allData.IndexOf(def.CellData);
                    if (index >= 0)
                    {
                        SetCellSpecialCell(x, y, index + 1);
                        SetCellItem(x, y, "");
                    }
                }
                return;
            }

            if (isActive)
            {
                SetCellItem(x, y, string.IsNullOrEmpty(_selectedItemId) ? "" : _selectedItemId);
                SetCellSpecialCell(x, y, 0);
            }
        }
        else if (currentEvent.button == 1)
        {
            if (currentEvent.shift)
            {
                SetCellActive(x, y, true);
                SetCellItem(x, y, "");
                SetCellSpecialCell(x, y, 0);
            }
            else
            {
                SetCellItem(x, y, "");
                SetCellActive(x, y, true);
            }
        }
        else if (currentEvent.button == 2 && isActive)
        {
            string itemId = GetCellItem(x, y);
            if (!string.IsNullOrEmpty(itemId))
                _selectedItemId = itemId;

            int specialCellType = GetCellSpecialCell(x, y);
            if (specialCellType > 0)
            {
                var cellData = GetSpecialCellDataByIndex(specialCellType);
                if (cellData != null)
                {
                    foreach (var def in _registry.GetSpecialCells())
                    {
                        if (def != null && def.CellData == cellData)
                        {
                            _selectedSpecialCellId = def.Id;
                            break;
                        }
                    }
                }
            }
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
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level", "Level_01.asset", "asset", "Choose where to save the level data");
        if (string.IsNullOrEmpty(path)) return;

        LevelData newLevel = CreateInstance<LevelData>();
        newLevel.Initialize(8, 8);

        var normalIds = _registry?.GetNormalItems().Select(d => d.Id).ToList() ?? new List<string>();
        if (normalIds.Count > 0)
        {
            for (int x = 0; x < newLevel.Width; x++)
                for (int y = 0; y < newLevel.Height; y++)
                    SetCellItemForLevel(newLevel, x, y, normalIds[Random.Range(0, normalIds.Count)]);
        }

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();
        _currentLevel = newLevel;
        EditorGUIUtility.PingObject(newLevel);
    }

    private void SetCellItemForLevel(LevelData level, int x, int y, string id)
    {
        if (level == null) return;
        if (x < 0 || x >= level.Width || y < 0 || y >= level.Height) return;
        int idx = y * level.Width + x;
        if (level.Items != null && idx < level.Items.Length)
            level.Items[idx] = id;
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return;

        int oldWidth = _currentLevel.Width;
        int oldHeight = _currentLevel.Height;
        var oldItems = _currentLevel.Items;
        var oldActive = _currentLevel.ActiveCells;
        var oldSpecialCells = _currentLevel.SpecialCells;

        string[] newItems = new string[newWidth * newHeight];
        bool[] newActive = new bool[newWidth * newHeight];
        int[] newSpecialCells = new int[newWidth * newHeight];

        int minWidth = Mathf.Min(newWidth, oldWidth);
        int minHeight = Mathf.Min(newHeight, oldHeight);

        for (int x = 0; x < minWidth; x++)
        {
            for (int y = 0; y < minHeight; y++)
            {
                int oldIdx = y * oldWidth + x;
                int newIdx = y * newWidth + x;
                newActive[newIdx] = oldActive != null && oldIdx < oldActive.Length && oldActive[oldIdx];
                newItems[newIdx] = oldItems != null && oldIdx < oldItems.Length ? oldItems[oldIdx] : "";
                newSpecialCells[newIdx] = oldSpecialCells != null && oldIdx < oldSpecialCells.Length
                    ? oldSpecialCells[oldIdx]
                    : 0;
            }
        }

        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                if (x >= minWidth || y >= minHeight)
                {
                    int idx = y * newWidth + x;
                    newActive[idx] = true;
                    newItems[idx] = "";
                    newSpecialCells[idx] = 0;
                }
            }
        }

        _currentLevel.Width = newWidth;
        _currentLevel.Height = newHeight;
        _currentLevel.ActiveCells = newActive;
        _currentLevel.Items = newItems;
        _currentLevel.SpecialCells = newSpecialCells;
    }

    private void FillSelection()
    {
        if (_currentLevel == null) return;

        int w = _currentLevel.Width;
        int h = _currentLevel.Height;
        string fillId = string.IsNullOrEmpty(_selectedItemId) ? "" : _selectedItemId;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int idx = y * w + x;
                if (_currentLevel.ActiveCells != null &&
                    idx < _currentLevel.ActiveCells.Length &&
                    _currentLevel.ActiveCells[idx] &&
                    _currentLevel.Items != null &&
                    idx < _currentLevel.Items.Length &&
                    string.IsNullOrEmpty(_currentLevel.Items[idx]))
                {
                    _currentLevel.Items[idx] = fillId;
                }
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
        {
            for (int y = 0; y < h; y++)
            {
                int idx = y * w + x;
                if (_currentLevel.ActiveCells != null &&
                    idx < _currentLevel.ActiveCells.Length &&
                    _currentLevel.ActiveCells[idx] &&
                    _currentLevel.Items != null &&
                    idx < _currentLevel.Items.Length &&
                    string.IsNullOrEmpty(_currentLevel.Items[idx]))
                {
                    _currentLevel.Items[idx] = normalIds[Random.Range(0, normalIds.Count)];
                }
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
                    {
                        int itemIdx = y * w + x;
                        if (_currentLevel.Items != null && itemIdx < _currentLevel.Items.Length)
                            _currentLevel.Items[itemIdx] = normalIds[Random.Range(0, normalIds.Count)];
                    }
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
        {
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                int idx = y * _currentLevel.Width + x;
                if (_currentLevel.Items != null && idx < _currentLevel.Items.Length)
                    _currentLevel.Items[idx] = "";
                if (_currentLevel.ActiveCells != null && idx < _currentLevel.ActiveCells.Length)
                    _currentLevel.ActiveCells[idx] = true;
                if (_currentLevel.SpecialCells != null && idx < _currentLevel.SpecialCells.Length)
                    _currentLevel.SpecialCells[idx] = 0;
            }
        }
    }
}