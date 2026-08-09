using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id;
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

    [Min(1)]
    public int SpecialCellStateCount = 1;

    [Tooltip("Sprites are ordered from the intact state to the most damaged state.")]
    public List<Sprite> SpecialCellStateSprites = new List<Sprite>();

    [Tooltip("Tint and transparency applied to special-cell overlays.")]
    public Color SpecialCellOverlayColor = new Color(1f, 1f, 1f, 0.5f);

    private void OnValidate()
    {
        SpecialCellStateCount = Mathf.Max(1, SpecialCellStateCount);

        if (SpecialCellOverlayColor.a <= 0f)
            SpecialCellOverlayColor.a = 0.5f;

        if (Color.a <= 0f)
            Color.a = 1f;
    }

    public Sprite GetSpecialCellStateSprite(int stateIndex)
    {
        if (SpecialCellStateSprites == null || SpecialCellStateSprites.Count == 0)
            return Icon;

        int clampedIndex = Mathf.Clamp(stateIndex, 0, SpecialCellStateSprites.Count - 1);
        return SpecialCellStateSprites[clampedIndex] != null
            ? SpecialCellStateSprites[clampedIndex]
            : Icon;
    }

}

public enum ItemCategory
{
    Normal,
    Special,
    SpecialCell
}
