using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CosmeticLocation", menuName = "Match Three/Cosmetics/Location")]
public sealed class CosmeticLocationDefinition : ScriptableObject
{
    public string LocationId = "location_1";
    public string DisplayName = "Локация 1";
    public Sprite Background;
    public Vector2 ReferenceSize = new Vector2(1920f, 1080f);
    public List<CosmeticFurnitureDefinition> Furniture = new List<CosmeticFurnitureDefinition>();
    public CosmeticLocationReward CompletionReward = new CosmeticLocationReward();
}

[Serializable]
public sealed class CosmeticFurnitureDefinition
{
    public string FurnitureId = "furniture_1";
    public string DisplayName = "Предмет";
    [Min(0)] public int CrystalPrice = 100;
    public Sprite ShopIcon;
    public Sprite LocationSprite;
    public Vector2 AnchoredPosition;
    public Vector2 Size = new Vector2(200f, 200f);
    public float Rotation;
    public int SortingOrder;
}

[Serializable]
public sealed class CosmeticLocationReward
{
    [Min(0)] public int Gold;
    public List<CosmeticBonusReward> Bonuses = new List<CosmeticBonusReward>();
}

[Serializable]
public sealed class CosmeticBonusReward
{
    public string BonusId = "bomb";
    [Min(0)] public int Count = 1;
}
