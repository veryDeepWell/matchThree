using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CosmeticsEditor : EditorWindow
{
    private const string DefaultFolder = "Assets/Internal/Cosmetics";
    private CosmeticCatalog _catalog;
    private int _locationIndex;
    private int _furnitureIndex = -1;
    private Vector2 _scroll;
    private bool _dragging;

    [MenuItem("Tools/Cosmetics Editor")]
    private static void Open() => GetWindow<CosmeticsEditor>("Cosmetics Editor");

    private void OnEnable()
    {
        if (_catalog == null)
            _catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/Internal/Resources/CosmeticCatalog.asset");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Редактор косметических локаций", EditorStyles.boldLabel);
        _catalog = (CosmeticCatalog)EditorGUILayout.ObjectField("Каталог", _catalog, typeof(CosmeticCatalog), false);

        if (_catalog == null)
        {
            EditorGUILayout.HelpBox("Выбери каталог или создай первый набор данных.", MessageType.Info);
            if (GUILayout.Button("Создать каталог и первую локацию"))
                CreateInitialCatalog();
            return;
        }

        _catalog.Locations ??= new List<CosmeticLocationDefinition>();
        DrawLocationToolbar();
        CosmeticLocationDefinition location = CurrentLocation;
        if (location == null)
            return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUI.BeginChangeCheck();
        DrawLocationFields(location);
        DrawRoomPreview(location);
        DrawFurnitureEditor(location);
        DrawShopPreview(location);
        DrawValidation(location);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(location);
            EditorUtility.SetDirty(_catalog);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Сохранить assets"))
            AssetDatabase.SaveAssets();
    }

    private void DrawLocationToolbar()
    {
        string[] names = _catalog.Locations.ConvertAll(location => location != null ? location.DisplayName : "<пусто>").ToArray();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (names.Length > 0)
                _locationIndex = EditorGUILayout.Popup("Локация", Mathf.Clamp(_locationIndex, 0, names.Length - 1), names);
            if (GUILayout.Button("+", GUILayout.Width(32)))
                CreateLocation();
            using (new EditorGUI.DisabledScope(CurrentLocation == null))
            {
                if (GUILayout.Button("−", GUILayout.Width(32)) && EditorUtility.DisplayDialog(
                        "Удалить локацию?", "Asset останется в проекте, но пропадёт из каталога.", "Удалить", "Отмена"))
                {
                    Undo.RecordObject(_catalog, "Remove cosmetic location");
                    _catalog.Locations.RemoveAt(_locationIndex);
                    _locationIndex = Mathf.Max(0, _locationIndex - 1);
                    _furnitureIndex = -1;
                }
            }
        }
    }

    private void DrawLocationFields(CosmeticLocationDefinition location)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Локация", EditorStyles.boldLabel);
        location.LocationId = EditorGUILayout.TextField("ID", location.LocationId);
        location.DisplayName = EditorGUILayout.TextField("Название", location.DisplayName);
        location.Background = (Sprite)EditorGUILayout.ObjectField("Фон", location.Background, typeof(Sprite), false);
        location.ReferenceSize = EditorGUILayout.Vector2Field("Размер макета", location.ReferenceSize);

        EditorGUILayout.LabelField("Награда за всю мебель", EditorStyles.boldLabel);
        location.CompletionReward ??= new CosmeticLocationReward();
        location.CompletionReward.Gold = EditorGUILayout.IntField("Золото", location.CompletionReward.Gold);
        SerializedObject serialized = new SerializedObject(location);
        EditorGUILayout.PropertyField(serialized.FindProperty("CompletionReward.Bonuses"), new GUIContent("Бонусы"), true);
        serialized.ApplyModifiedProperties();
    }

    private void DrawRoomPreview(CosmeticLocationDefinition location)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Предпросмотр комнаты (мебель можно двигать мышью)", EditorStyles.boldLabel);
        float width = Mathf.Max(300f, position.width - 45f);
        float aspect = location.ReferenceSize.x > 0f ? location.ReferenceSize.y / location.ReferenceSize.x : 0.5625f;
        Rect preview = GUILayoutUtility.GetRect(width, Mathf.Clamp(width * aspect, 260f, 620f));
        EditorGUI.DrawRect(preview, new Color(0.13f, 0.13f, 0.13f));
        DrawSprite(location.Background, preview, Color.white);

        location.Furniture ??= new List<CosmeticFurnitureDefinition>();
        for (int index = 0; index < location.Furniture.Count; index++)
        {
            CosmeticFurnitureDefinition item = location.Furniture[index];
            if (item == null)
                continue;
            Rect itemRect = ToPreviewRect(item, location.ReferenceSize, preview);
            DrawSprite(item.LocationSprite != null ? item.LocationSprite : item.ShopIcon, itemRect,
                item.LocationSprite != null || item.ShopIcon != null ? Color.white : new Color(0.4f, 0.7f, 1f, 0.7f));
            if (item.LocationSprite == null && item.ShopIcon == null)
                GUI.Label(itemRect, item.DisplayName, EditorStyles.centeredGreyMiniLabel);
            if (index == _furnitureIndex)
                Handles.DrawSolidRectangleWithOutline(itemRect, Color.clear, Color.yellow);
        }
        HandleRoomInput(location, preview);
    }

    private void HandleRoomInput(CosmeticLocationDefinition location, Rect preview)
    {
        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && preview.Contains(current.mousePosition))
        {
            _furnitureIndex = -1;
            for (int index = location.Furniture.Count - 1; index >= 0; index--)
            {
                if (ToPreviewRect(location.Furniture[index], location.ReferenceSize, preview).Contains(current.mousePosition))
                {
                    _furnitureIndex = index;
                    _dragging = true;
                    current.Use();
                    break;
                }
            }
        }
        else if (current.type == EventType.MouseDrag && _dragging && _furnitureIndex >= 0)
        {
            Undo.RecordObject(location, "Move cosmetic furniture");
            Vector2 scale = new Vector2(location.ReferenceSize.x / preview.width, location.ReferenceSize.y / preview.height);
            Vector2 delta = current.delta;
            location.Furniture[_furnitureIndex].AnchoredPosition += new Vector2(delta.x * scale.x, -delta.y * scale.y);
            EditorUtility.SetDirty(location);
            Repaint();
            current.Use();
        }
        else if (current.type == EventType.MouseUp)
        {
            _dragging = false;
        }
    }

    private void DrawFurnitureEditor(CosmeticLocationDefinition location)
    {
        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Мебель ({location.Furniture.Count})", EditorStyles.boldLabel);
            if (GUILayout.Button("Добавить предмет", GUILayout.Width(140)))
            {
                Undo.RecordObject(location, "Add cosmetic furniture");
                int number = location.Furniture.Count + 1;
                location.Furniture.Add(new CosmeticFurnitureDefinition
                {
                    FurnitureId = $"{location.LocationId}_furniture_{number}",
                    DisplayName = $"Предмет {number}",
                    SortingOrder = location.Furniture.Count
                });
                _furnitureIndex = location.Furniture.Count - 1;
            }
        }

        string[] names = location.Furniture.ConvertAll(item => item != null ? item.DisplayName : "<пусто>").ToArray();
        if (names.Length == 0)
            return;
        _furnitureIndex = EditorGUILayout.Popup("Выбранный предмет", Mathf.Clamp(_furnitureIndex, 0, names.Length - 1), names);
        CosmeticFurnitureDefinition furniture = location.Furniture[_furnitureIndex];
        furniture.FurnitureId = EditorGUILayout.TextField("ID", furniture.FurnitureId);
        furniture.DisplayName = EditorGUILayout.TextField("Название", furniture.DisplayName);
        furniture.CrystalPrice = Mathf.Max(0, EditorGUILayout.IntField("Цена в кристаллах", furniture.CrystalPrice));
        furniture.ShopIcon = (Sprite)EditorGUILayout.ObjectField("Картинка в магазине", furniture.ShopIcon, typeof(Sprite), false);
        furniture.LocationSprite = (Sprite)EditorGUILayout.ObjectField("Картинка в комнате", furniture.LocationSprite, typeof(Sprite), false);
        furniture.AnchoredPosition = EditorGUILayout.Vector2Field("Позиция", furniture.AnchoredPosition);
        furniture.Size = EditorGUILayout.Vector2Field("Размер", furniture.Size);
        furniture.Rotation = EditorGUILayout.FloatField("Поворот", furniture.Rotation);
        furniture.SortingOrder = EditorGUILayout.IntField("Порядок отрисовки", furniture.SortingOrder);

        if (GUILayout.Button("Удалить выбранный предмет"))
        {
            Undo.RecordObject(location, "Remove cosmetic furniture");
            location.Furniture.RemoveAt(_furnitureIndex);
            _furnitureIndex = Mathf.Min(_furnitureIndex, location.Furniture.Count - 1);
        }
    }

    private void DrawShopPreview(CosmeticLocationDefinition location)
    {
        if (_furnitureIndex < 0 || _furnitureIndex >= location.Furniture.Count)
            return;
        CosmeticFurnitureDefinition furniture = location.Furniture[_furnitureIndex];
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Предпросмотр карточки магазина", EditorStyles.boldLabel);
        Rect card = GUILayoutUtility.GetRect(310f, 155f);
        card.width = 310f;
        EditorGUI.DrawRect(card, new Color(0.2f, 0.2f, 0.24f));
        Rect icon = new Rect(card.x + 10f, card.y + 10f, 130f, 130f);
        DrawSprite(furniture.ShopIcon != null ? furniture.ShopIcon : furniture.LocationSprite, icon, Color.white);
        GUI.Label(new Rect(card.x + 150f, card.y + 15f, 150f, 28f), furniture.DisplayName, EditorStyles.boldLabel);
        GUI.Label(new Rect(card.x + 150f, card.y + 50f, 150f, 24f), $"{furniture.CrystalPrice} кристаллов");
        GUI.Box(new Rect(card.x + 150f, card.y + 92f, 140f, 42f), "Купить");
    }

    private static void DrawValidation(CosmeticLocationDefinition location)
    {
        var ids = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(location.LocationId))
            EditorGUILayout.HelpBox("У локации не указан ID.", MessageType.Error);
        if (location.Background == null)
            EditorGUILayout.HelpBox("Не назначен фон локации.", MessageType.Warning);
        foreach (CosmeticFurnitureDefinition item in location.Furniture)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.FurnitureId) || !ids.Add(item.FurnitureId))
                EditorGUILayout.HelpBox("У мебели есть пустой или повторяющийся ID.", MessageType.Error);
            else if (item.LocationSprite == null)
                EditorGUILayout.HelpBox($"У '{item.DisplayName}' нет картинки для комнаты.", MessageType.Warning);
        }
    }

    private static Rect ToPreviewRect(CosmeticFurnitureDefinition item, Vector2 referenceSize, Rect preview)
    {
        float scaleX = preview.width / Mathf.Max(1f, referenceSize.x);
        float scaleY = preview.height / Mathf.Max(1f, referenceSize.y);
        Vector2 size = new Vector2(item.Size.x * scaleX, item.Size.y * scaleY);
        Vector2 center = preview.center + new Vector2(item.AnchoredPosition.x * scaleX, -item.AnchoredPosition.y * scaleY);
        return new Rect(center - size * 0.5f, size);
    }

    private static void DrawSprite(Sprite sprite, Rect rect, Color color)
    {
        if (sprite == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 0.7f));
            return;
        }
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
    }

    private void CreateInitialCatalog()
    {
        EnsureFolder();
        _catalog = CreateInstance<CosmeticCatalog>();
        AssetDatabase.CreateAsset(_catalog, $"{DefaultFolder}/CosmeticCatalog.asset");
        CreateLocation();
        AssetDatabase.SaveAssets();
        Selection.activeObject = _catalog;
    }

    private void CreateLocation()
    {
        EnsureFolder();
        var location = CreateInstance<CosmeticLocationDefinition>();
        int number = _catalog.Locations.Count + 1;
        location.LocationId = $"location_{number}";
        location.DisplayName = $"Локация {number}";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/CosmeticLocation_{number}.asset");
        AssetDatabase.CreateAsset(location, path);
        Undo.RecordObject(_catalog, "Add cosmetic location");
        _catalog.Locations.Add(location);
        _locationIndex = _catalog.Locations.Count - 1;
        _furnitureIndex = -1;
        EditorUtility.SetDirty(_catalog);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(DefaultFolder))
            AssetDatabase.CreateFolder("Assets/Internal", "Cosmetics");
    }

    private CosmeticLocationDefinition CurrentLocation =>
        _catalog != null && _catalog.Locations != null && _catalog.Locations.Count > 0
            ? _catalog.Locations[Mathf.Clamp(_locationIndex, 0, _catalog.Locations.Count - 1)]
            : null;
}

[CustomPropertyDrawer(typeof(CosmeticBonusReward))]
public sealed class CosmeticBonusRewardDrawer : PropertyDrawer
{
    private static readonly List<ItemDefinition> AvailableBonuses = new List<ItemDefinition>();
    private static bool _cacheReady;

    static CosmeticBonusRewardDrawer()
    {
        EditorApplication.projectChanged += () => _cacheReady = false;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureBonusCache();

        SerializedProperty bonusId = property.FindPropertyRelative("BonusId");
        SerializedProperty count = property.FindPropertyRelative("Count");
        float line = EditorGUIUtility.singleLineHeight;
        Rect bonusRect = new Rect(position.x, position.y, position.width, line);
        Rect countRect = new Rect(position.x, position.y + line + EditorGUIUtility.standardVerticalSpacing,
            position.width, line);

        if (AvailableBonuses.Count == 0)
        {
            EditorGUI.PropertyField(bonusRect, bonusId, new GUIContent("Бонус ID"));
        }
        else
        {
            string[] options = new string[AvailableBonuses.Count];
            int selectedIndex = 0;
            for (int index = 0; index < AvailableBonuses.Count; index++)
            {
                ItemDefinition definition = AvailableBonuses[index];
                string displayName = string.IsNullOrWhiteSpace(definition.DisplayName)
                    ? definition.Id
                    : definition.DisplayName;
                options[index] = $"{displayName} ({definition.Id})";
                if (definition.Id == bonusId.stringValue)
                    selectedIndex = index;
            }

            int newIndex = EditorGUI.Popup(bonusRect, "Бонус", selectedIndex, options);
            bonusId.stringValue = AvailableBonuses[newIndex].Id;
        }

        count.intValue = Mathf.Max(0, EditorGUI.IntField(countRect, "Количество", count.intValue));
    }

    private static void EnsureBonusCache()
    {
        if (_cacheReady)
            return;

        AvailableBonuses.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (definition != null && definition.Category == ItemCategory.Special &&
                !string.IsNullOrWhiteSpace(definition.Id))
                AvailableBonuses.Add(definition);
        }

        AvailableBonuses.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName,
            System.StringComparison.OrdinalIgnoreCase));
        _cacheReady = true;
    }
}
