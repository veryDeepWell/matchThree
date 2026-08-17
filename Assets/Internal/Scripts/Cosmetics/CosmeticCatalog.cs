using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CosmeticCatalog", menuName = "Match Three/Cosmetics/Catalog")]
public sealed class CosmeticCatalog : ScriptableObject
{
    public List<CosmeticLocationDefinition> Locations = new List<CosmeticLocationDefinition>();
}
