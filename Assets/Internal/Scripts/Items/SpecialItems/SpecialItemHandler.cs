using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-75)]
public class SpecialItemHandler : MonoBehaviour
{
    [SerializeField] private List<Sprite> specialSprites;
    [SerializeField] private GameObject specialItemPrefab; // Базовый префаб для спец. предметов
    
    private Dictionary<SpecialItemTypes, Sprite> _spriteDictionary;
    private Dictionary<SpecialItemTypes, GameObject> _specialPrefabs;
    private Administrator _administrator;

    private void Awake()
    {
        _administrator = FindFirstObjectByType<Administrator>();
        BuildSpriteDictionary();
        GenerateSpecialPrefabs();
    }

    private void BuildSpriteDictionary()
    {
        _spriteDictionary = new Dictionary<SpecialItemTypes, Sprite>();
        
        var specialTypes = Enum.GetValues(typeof(SpecialItemTypes));
        for (int i = 0; i < specialSprites.Count && i < specialTypes.Length; i++)
        {
            if ((SpecialItemTypes)specialTypes.GetValue(i) != SpecialItemTypes.None)
            {
                _spriteDictionary[(SpecialItemTypes)specialTypes.GetValue(i)] = specialSprites[i];
            }
        }
    }

    private void GenerateSpecialPrefabs()
    {
        _specialPrefabs = new Dictionary<SpecialItemTypes, GameObject>();
        
        foreach (var kvp in _spriteDictionary)
        {
            GameObject go = Instantiate(specialItemPrefab);
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
                item.itemType = ItemTypes.Special;
                item.specialItemType = kvp.Key;
            }
            
            // Добавляем соответствующий компонент
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
                if (go.GetComponent<Bomb>() == null)
                    go.AddComponent<Bomb>();
                break;
        }
    }

    // Создает специальный предмет
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
            itemComponent.itemType = ItemTypes.Special;
            itemComponent.specialItemType = specialType;
        }
        
        // Вызываем CreateSpecialItem у компонента
        ISpecialItem specialComponent = newItem.GetComponent<ISpecialItem>();
        if (specialComponent != null)
        {
            specialComponent.CreateSpecialItem((int)position.x, (int)position.y);
        }
        
        return newItem;
    }

    // Проверка, является ли тип специальным
    public bool IsSpecialType(SpecialItemTypes type)
    {
        return type != SpecialItemTypes.None;
    }
}