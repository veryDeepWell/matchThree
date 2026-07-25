using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ItemHandler : MonoBehaviour
{
    [SerializeField] private List<Sprite> itemSprites;
    [SerializeField] private GameObject itemPrefab; // Базовый префаб с компонентом Item
    
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
        
        for (int i = 0; i < itemSprites.Count && i < Enum.GetValues(typeof(ItemTypes)).Length; i++)
        {
            if ((ItemTypes)i != ItemTypes.Special)
            {
                _spriteDictionary[(ItemTypes)i] = itemSprites[i];
            }
        }
    }

    private void GenerateItemPrefabs()
    {
        _itemPrefabs = new List<GameObject>();
        
        foreach (var kvp in _spriteDictionary)
        {
            GameObject go = Instantiate(itemPrefab);
            go.name = kvp.Key.ToString();
            
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = kvp.Value;
                sr.sortingOrder = 1;
            }
            
            Item item = go.GetComponent<Item>();
            if (item != null)
            {
                item._itemType = kvp.Key;
                item._specialType = SpecialItemTypes.None;
            }
            
            go.SetActive(false); // Отключаем, чтобы не отображался в сцене как префаб
            _itemPrefabs.Add(go);
        }
    }

    // Возвращает список префабов для генерации
    public List<GameObject> GetItemPrefabs()
    {
        return _itemPrefabs;
    }

    // Создает копию предмета по типу
    public GameObject CreateItem(ItemTypes type, Vector2 position, Transform parent)
    {
        foreach (var prefab in _itemPrefabs)
        {
            Item item = prefab.GetComponent<Item>();
            if (item != null && item._itemType == type)
            {
                GameObject newItem = Instantiate(prefab, position, Quaternion.identity, parent);
                newItem.SetActive(true);
                
                Item newItemComponent = newItem.GetComponent<Item>();
                if (newItemComponent != null)
                {
                    newItemComponent._itemType = type;
                    newItemComponent._specialType = SpecialItemTypes.None;
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