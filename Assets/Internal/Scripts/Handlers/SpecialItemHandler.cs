using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialItemHandler : MonoBehaviour
{
    [Header("Effect Data")] [SerializeField]
    private List<SpecialItemEffect> _allEffects;

    [SerializeField] private GameObject _specialItemPrefab;

    [Header("Cell Data")] [SerializeField] private List<SpecialCellData> _cellDataList;

    private Dictionary<string, SpecialItemEffect> _effectDictionary;

    public List<SpecialCellData> GetAllCellData() => _cellDataList;

    private void Awake()
    {
        BuildEffectDictionary();
    }

    private void BuildEffectDictionary()
    {
        _effectDictionary = new Dictionary<string, SpecialItemEffect>();

        foreach (var effect in _allEffects)
        {
            if (effect == null) continue;
            string id = effect.name.ToLower();
            _effectDictionary[id] = effect;
            Debug.Log($"[SpecialItemHandler] Registered effect: {id} -> {effect.name}");
        }
    }

    public SpecialItemEffect GetEffect(string id)
    {
        string key = id.ToLower();
        return _effectDictionary.ContainsKey(key) ? _effectDictionary[key] : null;
    }

    public GameObject CreateSpecialItem(string id, Vector2 position, Transform parent)
    {
        var effect = GetEffect(id);
        if (effect == null)
        {
            Debug.LogError($"SpecialItemHandler: No effect found for id '{id}'!");
            return null;
        }
    
        GameObject newItem = Instantiate(_specialItemPrefab, position, Quaternion.identity, parent);
        newItem.name = $"Special_{id}";
        newItem.SetActive(true);
    
        // Убеждаемся что есть Item компонент
        var item = newItem.GetComponent<Item>();
        if (item == null)
        {
            item = newItem.AddComponent<Item>();
            Debug.Log("[SpecialItemHandler] Added Item component");
        }
    
        // Убеждаемся что есть SpecialItem компонент
        var specialItem = newItem.GetComponent<SpecialItem>();
        if (specialItem == null)
        {
            specialItem = newItem.AddComponent<SpecialItem>();
            Debug.Log("[SpecialItemHandler] Added SpecialItem component");
        }
    
        // Инициализируем SpecialItem
        specialItem.Initialize(effect, -1, -1);
    
        // Добавляем коллайдер если нет
        if (newItem.GetComponent<Collider2D>() == null)
        {
            newItem.AddComponent<BoxCollider2D>();
        }
    
        return newItem;
    }
}