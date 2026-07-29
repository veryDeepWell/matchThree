using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-75)]
public class SpecialItemHandler : MonoBehaviour
{
    [SerializeField] private List<Sprite> _specialSprites;
    [SerializeField] private GameObject _specialItemPrefab;
    
    private Dictionary<SpecialItemTypes, Sprite> _spriteDictionary;
    private Dictionary<SpecialItemTypes, GameObject> _specialPrefabs;

    private void Awake()
    {
        BuildSpriteDictionary();
        GenerateSpecialPrefabs();
    }

    private void BuildSpriteDictionary()
    {
        _spriteDictionary = new Dictionary<SpecialItemTypes, Sprite>();
        
        var specialTypes = Enum.GetValues(typeof(SpecialItemTypes));
        int index = 0;
        for (int i = 0; i < specialTypes.Length; i++)
        {
            var type = (SpecialItemTypes)specialTypes.GetValue(i);
            if (type != SpecialItemTypes.None && index < _specialSprites.Count)
            {
                _spriteDictionary[type] = _specialSprites[index];
                index++;
            }
        }
    }

    private void GenerateSpecialPrefabs()
    {
        _specialPrefabs = new Dictionary<SpecialItemTypes, GameObject>();
        
        foreach (var kvp in _spriteDictionary)
        {
            GameObject go = Instantiate(_specialItemPrefab);
            go.name = kvp.Key.ToString();
            
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = kvp.Value;
                sr.sortingOrder = 2;
            }
            
            Item item = go.GetComponent<Item>();
            if (item != null)
            {
                item.ItemType = ItemTypes.Special;      // ← исправлено: ItemType
                item.SpecialType = kvp.Key;              // ← исправлено: SpecialType
            }
            
            AddSpecialComponent(go, kvp.Key);
            
            go.SetActive(false);
            _specialPrefabs[kvp.Key] = go;
        }
    }

    private void AddSpecialComponent(GameObject go, SpecialItemTypes specialType)
    {
        switch (specialType)
        {
            case SpecialItemTypes.Bomb:
                break;
            // TODO: Добавить остальные типы
        }
    }

    public GameObject CreateSpecialItem(SpecialItemTypes specialType, Vector2 position, Transform parent)
    {
        if (!_specialPrefabs.ContainsKey(specialType))
        {
            Debug.LogError($"Special item type {specialType} not found!");
            return null;
        }
        
        GameObject newItem = Instantiate(_specialPrefabs[specialType], position, Quaternion.identity, parent);
        newItem.SetActive(true);
        
        Item itemComponent = newItem.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.ItemType = ItemTypes.Special;   // ← исправлено: ItemType
            itemComponent.SpecialType = specialType;       // ← исправлено: SpecialType
        }
        
        ISpecialItem specialComponent = newItem.GetComponent<ISpecialItem>();
        if (specialComponent != null)
        {
            specialComponent.CreateSpecialItem((int)position.x, (int)position.y);
        }
        
        return newItem;
    }

    public bool IsSpecialType(SpecialItemTypes type)
    {
        return type != SpecialItemTypes.None;
    }
}