using UnityEngine;

/// <summary>
/// Central catalogue of shared gameplay VFX / SFX.
/// Create via Assets → Create → Game → FX Catalog.
/// Configure via Tools → FX Studio.
/// </summary>
[CreateAssetMenu(fileName = "GameFxCatalog", menuName = "Game/FX Catalog")]
public class GameFxCatalog : ScriptableObject
{
    [Header("Match / destroy")]
    public GameObject matchDestroyVfx;
    public AudioClip matchDestroySfx;

    [Header("Special item created from a match")]
    public GameObject specialSpawnVfx;
    public AudioClip specialSpawnSfx;

    [Header("Swap")]
    public AudioClip swapSfx;
    public AudioClip invalidSwapSfx;

    [Header("Gravity / land")]
    public AudioClip itemLandSfx;

    [Header("Level flow")]
    public AudioClip levelWinSfx;
    public AudioClip levelLoseSfx;
    public GameObject levelWinVfx;
    public GameObject levelLoseVfx;

    [Header("UI")]
    public AudioClip buttonClickSfx;
    public AudioClip rewardSfx;
}
