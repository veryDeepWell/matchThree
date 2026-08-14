using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Central editor for all VFX / SFX slots in the project.
/// Discovers GameFxCatalog, ItemDefinition, SpecialCellData, SpecialItemEffect assets.
/// </summary>
public class FxStudioWindow : EditorWindow
{
    private enum Tab
    {
        Sounds = 0,
        Visuals = 1
    }

    private Tab _tab = Tab.Sounds;
    private Vector2 _scroll;
    private string _search = "";
    private bool _onlyMissing;
    private bool _foldCatalog = true;
    private bool _foldItems = true;
    private bool _foldCells = true;
    private bool _foldSpecials = true;

    private GameFxCatalog _activeCatalog;
    private readonly List<GameFxCatalog> _catalogs = new List<GameFxCatalog>();
    private readonly List<ItemDefinition> _items = new List<ItemDefinition>();
    private readonly List<SpecialCellData> _cells = new List<SpecialCellData>();
    private readonly List<SpecialItemEffect> _specialEffects = new List<SpecialItemEffect>();

    private GUIStyle _headerStyle;
    private GUIStyle _okStyle;
    private GUIStyle _missingStyle;
    private GUIStyle _sectionStyle;

    // Cached reflection-free slot descriptors for the shared catalogue.
    private static readonly CatalogSlot[] CatalogSoundSlots =
    {
        new CatalogSlot("Match Destroy", nameof(GameFxCatalog.matchDestroySfx), isVfx: false),
        new CatalogSlot("Special Spawn", nameof(GameFxCatalog.specialSpawnSfx), isVfx: false),
        new CatalogSlot("Swap", nameof(GameFxCatalog.swapSfx), isVfx: false),
        new CatalogSlot("Invalid Swap", nameof(GameFxCatalog.invalidSwapSfx), isVfx: false),
        new CatalogSlot("Item Land", nameof(GameFxCatalog.itemLandSfx), isVfx: false),
        new CatalogSlot("Level Win", nameof(GameFxCatalog.levelWinSfx), isVfx: false),
        new CatalogSlot("Level Lose", nameof(GameFxCatalog.levelLoseSfx), isVfx: false),
        new CatalogSlot("Button Click", nameof(GameFxCatalog.buttonClickSfx), isVfx: false),
        new CatalogSlot("Reward", nameof(GameFxCatalog.rewardSfx), isVfx: false),
    };

    private static readonly CatalogSlot[] CatalogVfxSlots =
    {
        new CatalogSlot("Match Destroy", nameof(GameFxCatalog.matchDestroyVfx), isVfx: true),
        new CatalogSlot("Special Spawn", nameof(GameFxCatalog.specialSpawnVfx), isVfx: true),
        new CatalogSlot("Level Win", nameof(GameFxCatalog.levelWinVfx), isVfx: true),
        new CatalogSlot("Level Lose", nameof(GameFxCatalog.levelLoseVfx), isVfx: true),
    };

    private struct CatalogSlot
    {
        public readonly string Label;
        public readonly string FieldName;
        public readonly bool IsVfx;

        public CatalogSlot(string label, string fieldName, bool isVfx)
        {
            Label = label;
            FieldName = fieldName;
            IsVfx = isVfx;
        }
    }

    [MenuItem("Tools/FX Studio")]
    public static void Open()
    {
        var window = GetWindow<FxStudioWindow>("FX Studio");
        window.minSize = new Vector2(480, 360);
        window.RefreshAssets();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshAssets();
    }

    private void OnFocus()
    {
        // Keep list fresh when returning from Project window.
        RefreshAssets();
    }

    private void EnsureStyles()
    {
        if (_headerStyle != null)
            return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13
        };

        _okStyle = new GUIStyle(EditorStyles.miniLabel);
        _okStyle.normal.textColor = new Color(0.3f, 0.75f, 0.35f);

        _missingStyle = new GUIStyle(EditorStyles.miniLabel);
        _missingStyle.normal.textColor = new Color(0.95f, 0.55f, 0.2f);

        _sectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };
    }

    private void RefreshAssets()
    {
        _catalogs.Clear();
        _items.Clear();
        _cells.Clear();
        _specialEffects.Clear();

        foreach (var guid in AssetDatabase.FindAssets("t:GameFxCatalog"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameFxCatalog>(path);
            if (asset != null)
                _catalogs.Add(asset);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (asset != null)
                _items.Add(asset);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:SpecialCellData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SpecialCellData>(path);
            if (asset != null)
                _cells.Add(asset);
        }

        // SpecialItemEffect is abstract — FindAssets still returns concrete subclasses.
        foreach (var guid in AssetDatabase.FindAssets("t:SpecialItemEffect"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SpecialItemEffect>(path);
            if (asset != null)
                _specialEffects.Add(asset);
        }

        if (_activeCatalog == null && _catalogs.Count > 0)
            _activeCatalog = _catalogs[0];

        // Prefer catalog referenced by a MatchesHandler in open scenes / prefabs is hard;
        // user picks one in the toolbar.
        Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();

        DrawToolbar();
        EditorGUILayout.Space(4);

        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "🔊  Sounds", "✨  Visuals" }, GUILayout.Height(28));
        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (_tab == Tab.Sounds)
            DrawSoundsTab();
        else
            DrawVisualsTab();

        EditorGUILayout.EndScrollView();

        DrawFooterStats();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            RefreshAssets();

        GUILayout.Space(8);
        GUILayout.Label("Catalog", EditorStyles.miniLabel, GUILayout.Width(48));

        var catalogNames = _catalogs.Select(c => c != null ? c.name : "(null)").ToArray();
        int catalogIndex = Mathf.Max(0, _catalogs.IndexOf(_activeCatalog));
        EditorGUI.BeginChangeCheck();
        int newIndex = catalogIndex;
        if (catalogNames.Length > 0)
            newIndex = EditorGUILayout.Popup(catalogIndex, catalogNames, EditorStyles.toolbarPopup, GUILayout.MinWidth(140));
        else
            GUILayout.Label("— create Game → FX Catalog —", EditorStyles.miniLabel);

        if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < _catalogs.Count)
            _activeCatalog = _catalogs[newIndex];

        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
            CreateNewCatalog();

        GUILayout.FlexibleSpace();

        _onlyMissing = GUILayout.Toggle(_onlyMissing, "Only missing", EditorStyles.toolbarButton, GUILayout.Width(90));
        _search = GUILayout.TextField(_search ?? "", EditorStyles.toolbarSearchField, GUILayout.MinWidth(120), GUILayout.MaxWidth(200));

        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewCatalog()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create FX Catalog",
            "GameFxCatalog",
            "asset",
            "Choose a location for the FX catalogue");

        if (string.IsNullOrEmpty(path))
            return;

        var catalog = CreateInstance<GameFxCatalog>();
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        RefreshAssets();
        _activeCatalog = catalog;
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
    }

    // ───────────────────────── Sounds tab ─────────────────────────

    private void DrawSoundsTab()
    {
        DrawCatalogSection(sounds: true);
        DrawItemDefinitionsSection(sounds: true);
        DrawSpecialCellSection(sounds: true);
        DrawSpecialEffectSection(sounds: true);
    }

    // ───────────────────────── Visuals tab ─────────────────────────

    private void DrawVisualsTab()
    {
        DrawCatalogSection(sounds: false);
        DrawItemDefinitionsSection(sounds: false);
        DrawSpecialCellSection(sounds: false);
        DrawSpecialEffectSection(sounds: false);
    }

    // ───────────────────────── Sections ─────────────────────────

    private void DrawCatalogSection(bool sounds)
    {
        int missing = CountCatalogMissing(sounds);
        string title = sounds
            ? $"Shared catalogue (SFX)  —  {missing} missing"
            : $"Shared catalogue (VFX)  —  {missing} missing";

        _foldCatalog = DrawFoldoutSection(_foldCatalog, title, () =>
        {
            if (_activeCatalog == null)
            {
                EditorGUILayout.HelpBox(
                    "Нет GameFxCatalog. Нажми «+» в тулбаре или Create → Game → FX Catalog.",
                    MessageType.Warning);
                return;
            }

            var so = new SerializedObject(_activeCatalog);
            so.Update();

            var slots = sounds ? CatalogSoundSlots : CatalogVfxSlots;
            foreach (var slot in slots)
            {
                if (!PassesSearch(slot.Label) && !PassesSearch(_activeCatalog.name))
                    continue;

                var prop = so.FindProperty(slot.FieldName);
                if (prop == null)
                    continue;

                bool isMissing = prop.objectReferenceValue == null;
                if (_onlyMissing && !isMissing)
                    continue;

                DrawSlotRow(slot.Label, prop, isMissing, _activeCatalog);
            }

            so.ApplyModifiedProperties();
        });
    }

    private void DrawItemDefinitionsSection(bool sounds)
    {
        // Only normal crystals usually need destroy FX; still list all with the fields.
        var relevant = _items
            .Where(i => i != null)
            .Where(i => PassesSearch(i.name) || PassesSearch(i.Id) || PassesSearch(i.DisplayName))
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Id)
            .ToList();

        int missing = relevant.Count(i =>
        {
            if (sounds) return i.DestroySfx == null;
            return i.DestroyVfx == null;
        });

        string title = sounds
            ? $"Item definitions (Destroy SFX)  —  {missing}/{relevant.Count} missing"
            : $"Item definitions (Destroy VFX)  —  {missing}/{relevant.Count} missing";

        _foldItems = DrawFoldoutSection(_foldItems, title, () =>
        {
            if (relevant.Count == 0)
            {
                EditorGUILayout.LabelField("ItemDefinition assets not found.", EditorStyles.miniLabel);
                return;
            }

            foreach (var item in relevant)
            {
                bool isMissing = sounds ? item.DestroySfx == null : item.DestroyVfx == null;
                if (_onlyMissing && !isMissing)
                    continue;

                var so = new SerializedObject(item);
                so.Update();
                string field = sounds ? "DestroySfx" : "DestroyVfx";
                var prop = so.FindProperty(field);

                // Field may be missing if ItemDefinition was not updated yet.
                if (prop == null)
                {
                    EditorGUILayout.HelpBox(
                        $"{item.name}: no '{field}' on ItemDefinition — update the script.",
                        MessageType.None);
                    continue;
                }

                string label = string.IsNullOrEmpty(item.DisplayName) ? item.name : $"{item.DisplayName}  ({item.Id})";
                DrawSlotRow(label, prop, isMissing, item);
                so.ApplyModifiedProperties();
            }
        });
    }

    private void DrawSpecialCellSection(bool sounds)
    {
        var relevant = _cells
            .Where(c => c != null)
            .Where(c => PassesSearch(c.name))
            .OrderBy(c => c.name)
            .ToList();

        int missing = relevant.Count(c => sounds ? c.breakSound == null : c.breakEffect == null);
        string title = sounds
            ? $"Special cells (Break SFX)  —  {missing}/{relevant.Count} missing"
            : $"Special cells (Break VFX)  —  {missing}/{relevant.Count} missing";

        _foldCells = DrawFoldoutSection(_foldCells, title, () =>
        {
            if (relevant.Count == 0)
            {
                EditorGUILayout.LabelField("SpecialCellData assets not found.", EditorStyles.miniLabel);
                return;
            }

            foreach (var cell in relevant)
            {
                bool isMissing = sounds ? cell.breakSound == null : cell.breakEffect == null;
                if (_onlyMissing && !isMissing)
                    continue;

                var so = new SerializedObject(cell);
                so.Update();
                var prop = so.FindProperty(sounds ? "breakSound" : "breakEffect");
                if (prop == null)
                    continue;

                DrawSlotRow(cell.name, prop, isMissing, cell);
                so.ApplyModifiedProperties();
            }
        });
    }

    private void DrawSpecialEffectSection(bool sounds)
    {
        var relevant = _specialEffects
            .Where(e => e != null)
            .Where(e => PassesSearch(e.name) || PassesSearch(e.GetType().Name))
            .OrderBy(e => e.name)
            .ToList();

        int missing = relevant.Count(e =>
            sounds ? e.ActivationSound == null : e.ActivationEffect == null);

        string title = sounds
            ? $"Special item effects (Activation SFX)  —  {missing}/{relevant.Count} missing"
            : $"Special item effects (Activation VFX)  —  {missing}/{relevant.Count} missing";

        _foldSpecials = DrawFoldoutSection(_foldSpecials, title, () =>
        {
            if (relevant.Count == 0)
            {
                EditorGUILayout.LabelField("SpecialItemEffect assets not found.", EditorStyles.miniLabel);
                return;
            }

            foreach (var effect in relevant)
            {
                bool isMissing = sounds ? effect.ActivationSound == null : effect.ActivationEffect == null;
                if (_onlyMissing && !isMissing)
                    continue;

                var so = new SerializedObject(effect);
                so.Update();
                var prop = so.FindProperty(sounds ? "ActivationSound" : "ActivationEffect");
                if (prop == null)
                    continue;

                string label = $"{effect.name}  [{effect.GetType().Name}]";
                DrawSlotRow(label, prop, isMissing, effect);
                so.ApplyModifiedProperties();
            }
        });
    }

    // ───────────────────────── Row / helpers ─────────────────────────

    private void DrawSlotRow(string label, SerializedProperty prop, bool isMissing, UnityEngine.Object pingTarget)
    {
        EditorGUILayout.BeginHorizontal();

        // Status: filled / empty
        GUILayout.Label(isMissing ? "○" : "●", isMissing ? _missingStyle : _okStyle, GUILayout.Width(14));

        // Fixed-width name so columns align; ObjectField has no extra label
        // (PropertyField pulls [Header] decorators from the SO and breaks layout).
        GUILayout.Label(label, EditorStyles.label, GUILayout.Width(168));

        System.Type fieldType = typeof(UnityEngine.Object);
        if (prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            // Prefer concrete type so the object picker filters correctly.
            if (prop.type.Contains("AudioClip"))
                fieldType = typeof(AudioClip);
            else if (prop.type.Contains("GameObject"))
                fieldType = typeof(GameObject);
            else if (prop.objectReferenceValue != null)
                fieldType = prop.objectReferenceValue.GetType();
        }

        EditorGUI.BeginChangeCheck();
        UnityEngine.Object newValue = EditorGUILayout.ObjectField(
            prop.objectReferenceValue,
            fieldType,
            false,
            GUILayout.MinWidth(120));
        if (EditorGUI.EndChangeCheck())
        {
            prop.objectReferenceValue = newValue;
            prop.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prop.serializedObject.targetObject);
        }

        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
        {
            Selection.activeObject = pingTarget;
            EditorGUIUtility.PingObject(pingTarget);
        }

        if (!isMissing && prop.objectReferenceValue != null &&
            GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(48)))
        {
            Selection.activeObject = prop.objectReferenceValue;
            EditorGUIUtility.PingObject(prop.objectReferenceValue);
        }

        EditorGUILayout.EndHorizontal();
    }

    private bool DrawFoldoutSection(bool fold, string title, Action body)
    {
        EditorGUILayout.BeginVertical(_sectionStyle);
        fold = EditorGUILayout.Foldout(fold, title, true, _headerStyle);
        if (fold)
        {
            EditorGUI.indentLevel++;
            body?.Invoke();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
        return fold;
    }

    private bool PassesSearch(string text)
    {
        if (string.IsNullOrEmpty(_search))
            return true;
        if (string.IsNullOrEmpty(text))
            return false;
        return text.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private int CountCatalogMissing(bool sounds)
    {
        if (_activeCatalog == null)
            return 0;

        int n = 0;
        var slots = sounds ? CatalogSoundSlots : CatalogVfxSlots;
        var so = new SerializedObject(_activeCatalog);
        foreach (var slot in slots)
        {
            var prop = so.FindProperty(slot.FieldName);
            if (prop != null && prop.objectReferenceValue == null)
                n++;
        }

        return n;
    }

    private void DrawFooterStats()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        int soundMissing = CountAllMissing(sounds: true);
        int vfxMissing = CountAllMissing(sounds: false);
        GUILayout.Label($"Assets:  catalog {_catalogs.Count}  ·  items {_items.Count}  ·  cells {_cells.Count}  ·  specials {_specialEffects.Count}",
            EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Missing SFX: {soundMissing}   Missing VFX: {vfxMissing}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private int CountAllMissing(bool sounds)
    {
        int n = CountCatalogMissing(sounds);
        if (sounds)
        {
            n += _items.Count(i => i != null && i.DestroySfx == null);
            n += _cells.Count(c => c != null && c.breakSound == null);
            n += _specialEffects.Count(e => e != null && e.ActivationSound == null);
        }
        else
        {
            n += _items.Count(i => i != null && i.DestroyVfx == null);
            n += _cells.Count(c => c != null && c.breakEffect == null);
            n += _specialEffects.Count(e => e != null && e.ActivationEffect == null);
        }

        return n;
    }
}
