using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ItemHandler : MonoBehaviour
{
    [SerializeField] private List<Sprite> _itemSprites;
    [SerializeField] private GameObject _itemPrefab;

    private Dictionary<ItemTypes, Sprite> _spriteDictionary;
    private List<GameObject> _itemPrefabs;

    private void Awake()
    {
        BuildDictionary();
        GeneratePrefabs();
    }

    private void BuildDictionary()
    {
        _spriteDictionary = new Dictionary<ItemTypes, Sprite>();
        int index = 0;
        foreach (ItemTypes type in System.Enum.GetValues(typeof(ItemTypes)))
        {
            if (type != ItemTypes.None && type != ItemTypes.Special)
            {
                if (index < _itemSprites.Count && _itemSprites[index] != null)
                    _spriteDictionary[type] = _itemSprites[index];
                index++;
            }
        }
    }

    private void GeneratePrefabs()
    {
        _itemPrefabs = new List<GameObject>();
        foreach (ItemTypes type in System.Enum.GetValues(typeof(ItemTypes)))
        {
            if (type == ItemTypes.None || type == ItemTypes.Special) continue;
            if (!_spriteDictionary.ContainsKey(type)) continue;

            var go = Instantiate(_itemPrefab);
            go.name = type.ToString();
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = _spriteDictionary[type];
            var item = go.GetComponent<Item>();
            if (item) item.ItemType = type;
            go.SetActive(false);
            _itemPrefabs.Add(go);
        }
    }

    public List<GameObject> GetItemPrefabs() => _itemPrefabs;

    public GameObject CreateItem(ItemTypes type, Vector2 position, Transform parent)
    {
        foreach (var prefab in _itemPrefabs)
        {
            if (prefab.GetComponent<Item>().ItemType == type)
            {
                var go = Instantiate(prefab, position, Quaternion.identity, parent);
                go.SetActive(true);
                return go;
            }
        }
        Debug.LogError($"Item type {type} not found!");
        return null;
    }
}