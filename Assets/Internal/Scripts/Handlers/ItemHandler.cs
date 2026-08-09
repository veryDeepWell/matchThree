using System.Collections.Generic;
using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemRegistry _registry;
    [SerializeField] private GameObject _itemPrefab;

    private Dictionary<string, GameObject> _prefabById;
    private bool _isInitialized;

    public ItemRegistry GetRegistry() => _registry;

    public void ForceInitialize()
    {
        if (_registry == null)
        {
            Debug.LogError("[ItemHandler] ItemRegistry is not assigned.");
            return;
        }

        _registry.Initialize();
        RebuildCache();
        _isInitialized = true;
    }

    public void RebuildCache()
    {
        if (_registry == null || _itemPrefab == null)
        {
            Debug.LogError("[ItemHandler] Registry or item prefab is missing.");
            return;
        }

        if (_prefabById != null)
        {
            foreach (var prefab in _prefabById.Values)
            {
                if (prefab != null)
                {
                    if (Application.isPlaying)
                        Destroy(prefab);
                    else
                        DestroyImmediate(prefab);
                }
            }
        }

        _prefabById = new Dictionary<string, GameObject>();
        foreach (var definition in _registry.GetNormalItems())
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id)) continue;

            var prefab = Instantiate(_itemPrefab);
            prefab.name = definition.Id;
            prefab.SetActive(false);

            var item = prefab.GetComponent<Item>();
            if (item != null)
            {
                item.ItemId = definition.Id;
                item.SpecialItemId = "";
            }

            ApplyVisual(prefab, definition);
            _prefabById[definition.Id] = prefab;
        }
    }

    public GameObject CreateItem(string id, Vector2 position, Transform parent)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(id) || !_prefabById.TryGetValue(id, out var prefab))
        {
            Debug.LogError($"[ItemHandler] Item with id '{id}' was not found.");
            return null;
        }

        var itemObject = Instantiate(prefab, position, Quaternion.identity, parent);
        itemObject.SetActive(true);
        ApplyVisual(itemObject, _registry.Get(id));

        var item = itemObject.GetComponent<Item>();
        if (item != null)
        {
            item.ItemId = id;
            item.SpecialItemId = "";
        }

        return itemObject;
    }

    public Sprite GetSprite(string id)
    {
        var definition = _registry != null ? _registry.Get(id) : null;
        return definition != null ? definition.Icon : null;
    }

    public ItemDefinition GetDefinition(string id)
    {
        return _registry != null ? _registry.Get(id) : null;
    }

    public List<GameObject> GetItemPrefabs()
    {
        EnsureInitialized();
        return _prefabById != null ? new List<GameObject>(_prefabById.Values) : new List<GameObject>();
    }

    public void ApplyVisual(Item item, string id)
    {
        if (item == null) return;
        ApplyVisual(item.gameObject, _registry != null ? _registry.Get(id) : null);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _prefabById == null || _prefabById.Count == 0)
            ForceInitialize();
    }

    private static void ApplyVisual(GameObject itemObject, ItemDefinition definition)
    {
        if (itemObject == null || definition == null) return;

        var renderer = itemObject.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        renderer.sprite = definition.Icon;
        renderer.color = definition.Color;
        renderer.sortingOrder = 1;
    }
}
