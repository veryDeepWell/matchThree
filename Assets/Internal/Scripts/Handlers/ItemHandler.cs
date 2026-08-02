using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [Header("Registry")]
    [SerializeField] private ItemRegistry _registry;
    [SerializeField] private GameObject _itemPrefab;
    
    private Dictionary<string, GameObject> _prefabCache;
    private bool _isInitialized = false;
    
    public ItemRegistry GetRegistry() => _registry;
    
    public void ForceInitialize()
    {
        if (_isInitialized)
        {
            // Если уже инициализированы — пересоздаём кеш (на случай если спрайты добавили позже)
            RebuildCache();
            return;
        }
        
        if (_registry == null)
        {
            Debug.LogError("ItemHandler: No ItemRegistry assigned!");
            return;
        }
        
        _registry.Initialize();
        BuildPrefabCache();
        _isInitialized = true;
        
        Debug.Log($"[ItemHandler] Initialized with {_prefabCache?.Count ?? 0} prefabs");
    }
    
    private void BuildPrefabCache()
    {
        _prefabCache = new Dictionary<string, GameObject>();
        
        foreach (var def in _registry.GetNormalItems())
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) continue;
            
            var go = Instantiate(_itemPrefab);
            go.name = def.Id;
            
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = def.Icon;
                sr.color = def.Color;
                sr.sortingOrder = 1;
            }
            
            var item = go.GetComponent<Item>();
            if (item != null)
            {
                item.ItemId = def.Id;
                item.SpecialItemId = "";
            }
            
            go.SetActive(false);
            _prefabCache[def.Id] = go;
        }
    }
    
    // Пересоздаём кеш без перезагрузки всего
    public void RebuildCache()
    {
        if (_registry == null)
        {
            Debug.LogError("ItemHandler: No ItemRegistry assigned!");
            return;
        }
        
        // Удаляем старые префабы
        if (_prefabCache != null)
        {
            foreach (var kvp in _prefabCache)
            {
                if (kvp.Value != null)
                    DestroyImmediate(kvp.Value);
            }
            _prefabCache.Clear();
        }
        
        _registry.Initialize();
        BuildPrefabCache();
        
        Debug.Log($"[ItemHandler] Cache rebuilt with {_prefabCache?.Count ?? 0} prefabs");
    }
    
    public List<GameObject> GetItemPrefabs()
    {
        if (_prefabCache == null)
        {
            BuildPrefabCache();
        }
        return new List<GameObject>(_prefabCache.Values);
    }
    
    public GameObject CreateItem(string id, Vector2 position, Transform parent)
    {
        // Если кеш пустой — пересоздаём
        if (_prefabCache == null || _prefabCache.Count == 0)
        {
            BuildPrefabCache();
        }
        
        if (!_prefabCache.ContainsKey(id))
        {
            Debug.LogError($"ItemHandler: Item with id '{id}' not found! Available: {string.Join(", ", _prefabCache.Keys)}");
            return null;
        }
        
        var prefab = _prefabCache[id];
        var go = Instantiate(prefab, position, Quaternion.identity, parent);
        go.SetActive(true);
        
        var item = go.GetComponent<Item>();
        if (item != null)
        {
            item.ItemId = id;
            item.SpecialItemId = "";
            
            // ОБНОВЛЯЕМ СПРАЙТ ИЗ РЕЕСТРА (на случай если префаб старый)
            var def = _registry.Get(id);
            if (def != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = def.Icon;
                    sr.color = def.Color;
                }
            }
        }
        
        return go;
    }
    
    public Sprite GetSprite(string id)
    {
        var def = _registry.Get(id);
        return def != null ? def.Icon : null;
    }
    
    public ItemDefinition GetDefinition(string id)
    {
        return _registry.Get(id);
    }
}