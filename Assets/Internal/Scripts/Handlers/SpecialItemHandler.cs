using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialItemHandler : MonoBehaviour
{
    [Header("Effect Data")]
    [SerializeField] private List<SpecialItemEffect> _allEffects;
    [SerializeField] private GameObject _specialItemPrefab;
    
    [Header("Cell Data")]
    [SerializeField] private List<SpecialCellData> _cellDataList;
    
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
        newItem.SetActive(true);
        
        var specialItem = newItem.GetComponent<SpecialItem>();
        if (specialItem != null)
        {
            specialItem.Initialize(effect, (int)position.x, (int)position.y);
        }
        else
        {
            Debug.LogError("SpecialItemHandler: specialItemPrefab must have SpecialItem component!");
            return null;
        }
        
        return newItem;
    }
}