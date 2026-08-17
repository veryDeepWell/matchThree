using UnityEngine;

/// <summary>
/// Central catalogue of shared gameplay VFX / SFX / Music.
/// Create via Assets → Create → Game → FX Catalog.
/// Configure via Tools → FX Studio.
///
/// Priority for destroy SFX/VFX on normal items:
///   1) ItemDefinition.DestroySfx / DestroyVfx  (per-colour override)
///   2) GameFxCatalog.matchDestroySfx / matchDestroyVfx  (shared default)
///
/// Special cells use SpecialCellData.breakSound / breakEffect (fallback: cellBreak*).
/// Special items use SpecialItemEffect.ActivationSound / ActivationEffect (fallback: specialActivate*).
/// </summary>
[CreateAssetMenu(fileName = "GameFxCatalog", menuName = "Game/FX Catalog")]
public class GameFxCatalog : ScriptableObject
{
    [Header("Match / destroy (default for normal items)")]
    public GameObject matchDestroyVfx;
    public AudioClip matchDestroySfx;

    [Header("Special item created from a match")]
    public GameObject specialSpawnVfx;
    public AudioClip specialSpawnSfx;

    [Header("Special item activation (fallback)")]
    public GameObject specialActivateVfx;
    public AudioClip specialActivateSfx;

    [Header("Special cell break (fallback)")]
    public GameObject cellBreakVfx;
    public AudioClip cellBreakSfx;

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

    [Header("Magnet beam")]
    [Tooltip("Optional material for magnet pull lines. If null, a default unlit material is created at runtime.")]
    public Material magnetLineMaterial;
    public Color magnetLineColor = new Color(0.6f, 0.2f, 1f, 0.95f);
    [Min(0.01f)] public float magnetLineWidth = 0.08f;
    [Min(0.05f)] public float magnetLineDuration = 0.35f;

    [Header("Music")]
    [Tooltip("Tracks played in order (or random if shuffle). Assign menu/game themes here.")]
    public AudioClip[] musicTracks;
    public bool musicShuffle;
    [Range(0f, 5f)] public float musicCrossfadeSeconds = 1.25f;
}
