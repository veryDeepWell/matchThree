using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Game/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> _allItems = new List<ItemDefinition>();
    
    private Dictionary<string, ItemDefinition> _idMap;
    private Dictionary<ItemCategory, List<ItemDefinition>> _categoryMap;
    
    public IReadOnlyList<ItemDefinition> AllItems => _allItems;
    
    public void Initialize()
    {
        _idMap = new Dictionary<string, ItemDefinition>();
        _categoryMap = new Dictionary<ItemCategory, List<ItemDefinition>>();
        
        foreach (var category in System.Enum.GetValues(typeof(ItemCategory)))
        {
            _categoryMap[(ItemCategory)category] = new List<ItemDefinition>();
        }
        
        foreach (var item in _allItems)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) continue;
            _idMap[item.Id] = item;
            _categoryMap[item.Category].Add(item);
        }
    }
    
    public ItemDefinition Get(string id)
    {
        if (_idMap == null) Initialize();
        return _idMap.ContainsKey(id) ? _idMap[id] : null;
    }
    
    public List<ItemDefinition> GetByCategory(ItemCategory category)
    {
        if (_idMap == null) Initialize();
        return _categoryMap.ContainsKey(category) ? _categoryMap[category] : new List<ItemDefinition>();
    }
    
    public List<ItemDefinition> GetNormalItems() => GetByCategory(ItemCategory.Normal);
    public List<ItemDefinition> GetSpecialItems() => GetByCategory(ItemCategory.Special);
    public List<ItemDefinition> GetSpecialCells() => GetByCategory(ItemCategory.SpecialCell);
    
    public string GetRandomNormalId()
    {
        var normals = GetNormalItems();
        if (normals == null || normals.Count == 0) return "";
        return normals[Random.Range(0, normals.Count)].Id;
    }
    
    public List<SpecialCellData> GetAllSpecialCellData()
    {
        var result = new List<SpecialCellData>();
        foreach (var item in _allItems)
        {
            if (item != null && item.Category == ItemCategory.SpecialCell && item.CellData != null)
            {
                result.Add(item.CellData);
            }
        }
        return result;
    }

    public SpecialCellData GetSpecialCellDataById(string id)
    {
        var def = Get(id);
        if (def != null && def.Category == ItemCategory.SpecialCell)
            return def.CellData;
        return null;
    }
}