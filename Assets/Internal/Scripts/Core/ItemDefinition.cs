using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id;           // "red", "blue", "bomb", "ice"
    public string DisplayName;
    
    [Header("Visual")]
    public Sprite Icon;
    public Color Color = Color.white;
    
    [Header("Category")]
    public ItemCategory Category;
    
    [Header("Special Item Effect (for Special category)")]
    public SpecialItemEffect SpecialEffect;
    
    [Header("Special Cell Data (for SpecialCell category)")]
    public SpecialCellData CellData;
}

public enum ItemCategory
{
    Normal,      // Обычный предмет
    Special,     // Специальный предмет (бомба, свипер...)
    SpecialCell  // Специальная ячейка (лёд, камень...)
}