using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ItemHandler : MonoBehaviour
{
    [SerializeField] private List<Sprite> itemSprites;
    [SerializeField] private GameObject itemPrefab;
    
    private Dictionary<ItemTypes, Sprite> _spriteDictionary;
    private List<GameObject> _itemPrefabs;

    private void Awake()
    {
        BuildSpriteDictionary();
        GenerateItemPrefabs();
    }

    private void BuildSpriteDictionary()
    {
        _spriteDictionary = new Dictionary<ItemTypes, Sprite>();
        
        int index = 0;
        foreach (ItemTypes type in System.Enum.GetValues(typeof(ItemTypes)))
        {
            if (type != ItemTypes.None && type != ItemTypes.Special)
            {
                if (index < itemSprites.Count && itemSprites[index] != null)
                {
                    _spriteDictionary[type] = itemSprites[index];
                }
                index++;
            }
        }
    }

    private void GenerateItemPrefabs()
    {
        _itemPrefabs = new List<GameObject>();
        
        foreach (ItemTypes type in System.Enum.GetValues(typeof(ItemTypes)))
        {
            if (type == ItemTypes.None || type == ItemTypes.Special) continue;
            if (!_spriteDictionary.ContainsKey(type)) continue;
            
            GameObject go = Instantiate(itemPrefab);
            go.name = type.ToString();
            
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = _spriteDictionary[type];
                sr.sortingOrder = 1;
            }
            
            Item item = go.GetComponent<Item>();
            if (item != null)
            {
                item.itemType = type;
                item.specialItemType = SpecialItemTypes.None;
            }
            
            go.SetActive(false);
            _itemPrefabs.Add(go);
        }
    }

    public List<GameObject> GetItemPrefabs()
    {
        return _itemPrefabs;
    }

    public GameObject CreateItem(ItemTypes type, Vector2 position, Transform parent)
    {
        foreach (var prefab in _itemPrefabs)
        {
            Item item = prefab.GetComponent<Item>();
            if (item != null && item.itemType == type)
            {
                GameObject newItem = Instantiate(prefab, position, Quaternion.identity, parent);
                newItem.SetActive(true);
                
                Item newItemComponent = newItem.GetComponent<Item>();
                if (newItemComponent != null)
                {
                    newItemComponent.itemType = type;
                    newItemComponent.specialItemType = SpecialItemTypes.None;
                }
                
                return newItem;
            }
        }
        
        Debug.LogError($"Item type {type} not found in ItemHandler!");
        return null;
    }

    public Sprite GetSprite(ItemTypes type)
    {
        return _spriteDictionary.ContainsKey(type) ? _spriteDictionary[type] : null;
    }
}