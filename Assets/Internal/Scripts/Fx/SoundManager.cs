using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio playback with voice pooling, per-clip cooldowns and unscaled time
/// (works while Time.timeScale == 0 on win/lose screens).
/// </summary>
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private GameFxCatalog _catalog;

    [Header("Pool")]
    [SerializeField] private int _poolSize = 12;
    [SerializeField] [Range(1, 8)] private int _maxSameClipVoices = 2;
    [SerializeField] private float _defaultCooldown = 0.06f;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;

    private readonly List<AudioSource> _pool = new List<AudioSource>();
    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>();
    private readonly Dictionary<int, int> _activeSameClip = new Dictionary<int, int>();
    private Transform _poolRoot;

    public GameFxCatalog Catalog
    {
        get => _catalog;
        set => _catalog = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject("[SoundManager]");
        var manager = go.AddComponent<SoundManager>();
        DontDestroyOnLoad(go);
        manager.EnsureCatalog();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
        EnsureCatalog();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildPool()
    {
        if (_poolRoot != null)
            return;

        _poolRoot = new GameObject("SfxPool").transform;
        _poolRoot.SetParent(transform, false);

        int size = Mathf.Max(4, _poolSize);
        for (int i = 0; i < size; i++)
            _pool.Add(CreateSource($"Sfx_{i}"));
    }

    private AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_poolRoot, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;          // 2D — слышно всегда
        source.ignoreListenerPause = true;
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    /// <summary>
    /// Catalog comes only from the serialized field on this component.
    /// Assign FX/FxCatalog in the inspector on MatchesHandler; gameplay code reads
    /// MatchesHandler.FxCatalog directly. SoundManager.GetCatalog() is optional.
    /// Never queries MatchesHandler here — avoids any risk of recursive lookups.
    /// </summary>
    public void EnsureCatalog()
    {
        // Intentionally empty beyond the field: do not FindObjectOfType(MatchesHandler).
    }

    public static GameFxCatalog GetCatalog()
    {
        return Instance != null ? Instance._catalog : null;
    }

    public static void Play(AudioClip clip, float volumeScale = 1f, float cooldown = -1f)
    {
        if (Instance == null || clip == null)
            return;

        Instance.PlayInternal(clip, volumeScale, cooldown);
    }

    public static void PlayUi(AudioClip clip)
    {
        // UI always bypasses aggressive cooldown so clicks feel responsive,
        // but still goes through the pool (no stacked PlayClipAtPoint objects).
        if (Instance == null || clip == null)
            return;

        Instance.PlayInternal(clip, 1f, 0.02f);
    }

    public static void PlayButtonClick()
    {
        var catalog = GetCatalog();
        if (catalog != null)
            PlayUi(catalog.buttonClickSfx);
    }

    private void PlayInternal(AudioClip clip, float volumeScale, float cooldown)
    {
        if (clip == null || _sfxVolume <= 0f || _masterVolume <= 0f)
            return;

        int id = clip.GetInstanceID();
        float now = Time.unscaledTime;
        float cd = cooldown >= 0f ? cooldown : _defaultCooldown;

        if (_nextAllowedTime.TryGetValue(id, out float next) && now < next)
            return;

        if (!_activeSameClip.TryGetValue(id, out int activeCount))
            activeCount = 0;

        if (activeCount >= _maxSameClipVoices)
            return;

        AudioSource source = RentSource();
        if (source == null)
            return;

        _nextAllowedTime[id] = now + cd;
        _activeSameClip[id] = activeCount + 1;

        source.clip = clip;
        source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.Play();

        // Release voice after clip ends (unscaled).
        StartCoroutine(ReleaseWhenDone(source, clip, id));
    }

    private System.Collections.IEnumerator ReleaseWhenDone(AudioSource source, AudioClip clip, int clipId)
    {
        float end = Time.unscaledTime + clip.length + 0.05f;
        while (Time.unscaledTime < end && source != null && source.isPlaying)
            yield return null;

        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }

        if (_activeSameClip.TryGetValue(clipId, out int count))
        {
            count = Mathf.Max(0, count - 1);
            if (count == 0)
                _activeSameClip.Remove(clipId);
            else
                _activeSameClip[clipId] = count;
        }
    }

    private AudioSource RentSource()
    {
        BuildPool();

        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
                return _pool[i];
        }

        // Steal the oldest-finished-ish: just take index 0 and restart.
        if (_pool.Count > 0)
        {
            var stolen = _pool[0];
            stolen.Stop();
            return stolen;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _poolSize = Mathf.Max(4, _poolSize);
        _maxSameClipVoices = Mathf.Clamp(_maxSameClipVoices, 1, 8);
    }
#endif
}
