using System.Collections.Generic;
using UnityEngine;

public class SpecialItemHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemRegistry _registry;
    [SerializeField] private GameObject _specialItemPrefab;

    private Dictionary<string, SpecialItemEffect> _effectsById;

    private void Awake()
    {
        BuildEffectDictionary();
    }

    public SpecialItemEffect GetEffect(string id)
    {
        EnsureEffectDictionary();
        if (string.IsNullOrEmpty(id)) return null;

        string key = id.ToLowerInvariant();
        if (_effectsById.TryGetValue(key, out var effect) && effect != null)
            return effect;

        // Runtime fallback so the game keeps working even if ScriptableObject
        // assets lost their script references after a reimport.
        effect = CreateRuntimeEffect(key);
        if (effect != null)
            _effectsById[key] = effect;

        return effect;
    }

    private static SpecialItemEffect CreateRuntimeEffect(string id)
    {
        switch (id)
        {
            case "bomb":
                return ScriptableObject.CreateInstance<BombEffect>();
            case "sweeper_h":
                return LineSweeperEffect.Create(SweeperMode.Horizontal);
            case "sweeper_v":
                return LineSweeperEffect.Create(SweeperMode.Vertical);
            case "sweeper_cross":
                return LineSweeperEffect.Create(SweeperMode.Cross);
            case "magnet":
                return ScriptableObject.CreateInstance<MagnetEffect>();
            default:
                return null;
        }
    }

    public GameObject CreateSpecialItem(string id, Vector2 position, Transform parent)
    {
        var effect = GetEffect(id);
        var definition = GetDefinition(id);

        if (effect == null)
        {
            Debug.LogError($"[SpecialItemHandler] No effect found for id '{id}'.");
            return null;
        }

        if (_specialItemPrefab == null)
        {
            Debug.LogError("[SpecialItemHandler] Special item prefab is not assigned.");
            return null;
        }

        // Definition is preferred for visuals, but we can still spawn without it.
        if (definition == null)
            Debug.LogWarning($"[SpecialItemHandler] No ItemDefinition for '{id}', spawning with effect only.");

        var itemObject = Instantiate(_specialItemPrefab, position, Quaternion.identity, parent);
        itemObject.name = $"Special_{id}";
        itemObject.SetActive(true);

        if (itemObject.GetComponent<Item>() == null)
            itemObject.AddComponent<Item>();

        var specialItem = itemObject.GetComponent<SpecialItem>() ?? itemObject.AddComponent<SpecialItem>();
        specialItem.Initialize(effect, definition, -1, -1);

        return itemObject;
    }

    private ItemDefinition GetDefinition(string id)
    {
        if (_registry == null)
            _registry = Resources.Load<ItemRegistry>("ItemRegistry");

        if (_registry == null) return null;
        _registry.Initialize();
        return _registry.Get(id);
    }

    private void EnsureEffectDictionary()
    {
        if (_effectsById == null)
            BuildEffectDictionary();
    }

    private void BuildEffectDictionary()
    {
        _effectsById = new Dictionary<string, SpecialItemEffect>();

        if (_registry == null)
        {
            var itemHandler = FindObjectOfType<ItemHandler>();
            _registry = itemHandler != null ? itemHandler.GetRegistry() : Resources.Load<ItemRegistry>("ItemRegistry");
        }

        if (_registry != null)
            _registry.Initialize();

        var definitions = _registry != null ? _registry.GetSpecialItems() : null;
        if (definitions == null)
            return;

        foreach (var definition in definitions)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id) || definition.SpecialEffect == null)
                continue;

            _effectsById[definition.Id.ToLowerInvariant()] = definition.SpecialEffect;
        }
    }
}
