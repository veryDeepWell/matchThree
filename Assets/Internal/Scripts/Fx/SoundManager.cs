using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio: SFX pool + music channel, volumes, PlayerPrefs persistence.
/// Works with Time.timeScale == 0 (unscaled time).
/// </summary>
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string PrefMaster = "audio.master";
    private const string PrefSfx = "audio.sfx";
    private const string PrefMusic = "audio.music";
    private const string PrefMuted = "audio.muted";

    [Header("Catalog")]
    [SerializeField] private GameFxCatalog _catalog;

    [Header("Pool")]
    [SerializeField] private int _poolSize = 12;
    [SerializeField] [Range(1, 8)] private int _maxSameClipVoices = 3;
    [SerializeField] private float _defaultCooldown = 0.05f;

    [Header("Volumes (runtime; also saved)")]
    [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.7f;
    [SerializeField] private bool _muted;

    private readonly List<AudioSource> _pool = new List<AudioSource>();
    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>();
    private readonly Dictionary<int, int> _activeSameClip = new Dictionary<int, int>();
    private Transform _poolRoot;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private bool _musicUsingA = true;
    private int _musicIndex = -1;
    private Coroutine _musicFadeRoutine;
    private bool _musicStarted;

    public GameFxCatalog Catalog
    {
        get
        {
            if (_catalog == null)
                TryResolveCatalog();
            return _catalog;
        }
        set => _catalog = value;
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyMusicVolume();
            SaveSettings();
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            SaveSettings();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            ApplyMusicVolume();
            SaveSettings();
        }
    }

    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            ApplyMusicVolume();
            SaveSettings();
        }
    }

    public float EffectiveSfxVolume => _muted ? 0f : _masterVolume * _sfxVolume;
    public float EffectiveMusicVolume => _muted ? 0f : _masterVolume * _musicVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject("[SoundManager]");
        go.AddComponent<SoundManager>();
        DontDestroyOnLoad(go);
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
        LoadSettings();
        BuildPool();
        BuildMusicSources();
        TryResolveCatalog();
    }

    private void Start()
    {
        // Auto-start music once catalog is known (menu or first scene).
        if (!_musicStarted && Catalog != null && Catalog.musicTracks != null && Catalog.musicTracks.Length > 0)
            PlayMusicPlaylist();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void TryResolveCatalog()
    {
        if (_catalog != null)
            return;

        // 1) Resources
        _catalog = Resources.Load<GameFxCatalog>("FxCatalog");
        if (_catalog != null)
            return;
        _catalog = Resources.Load<GameFxCatalog>("GameFxCatalog");
        if (_catalog != null)
            return;

        // 2) Any catalog in loaded assets (editor / addressables-less projects)
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:GameFxCatalog");
        if (guids != null && guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFxCatalog>(path);
            if (_catalog != null)
                return;
        }
#endif

        // 3) MatchesHandler on scene (runtime fallback)
        var handler = FindObjectOfType<MatchesHandler>();
        if (handler != null && handler.FxCatalog != null)
            _catalog = handler.FxCatalog;
    }

    private void BuildPool()
    {
        if (_poolRoot != null)
            return;

        _poolRoot = new GameObject("SfxPool").transform;
        _poolRoot.SetParent(transform, false);

        int size = Mathf.Max(4, _poolSize);
        for (int i = 0; i < size; i++)
            _pool.Add(CreateSource($"Sfx_{i}", music: false));
    }

    private void BuildMusicSources()
    {
        if (_musicA != null)
            return;

        _musicA = CreateSource("Music_A", music: true);
        _musicB = CreateSource("Music_B", music: true);
        _musicA.loop = true;
        _musicB.loop = true;
    }

    private AudioSource CreateSource(string name, bool music)
    {
        var go = new GameObject(name);
        go.transform.SetParent(music ? transform : _poolRoot, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.rolloffMode = AudioRolloffMode.Linear;
        if (music)
            source.priority = 0;
        return source;
    }

    // ---------- Settings persistence ----------

    private void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(PrefSfx, 1f);
        _musicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.7f);
        _muted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(PrefMaster, _masterVolume);
        PlayerPrefs.SetFloat(PrefSfx, _sfxVolume);
        PlayerPrefs.SetFloat(PrefMusic, _musicVolume);
        PlayerPrefs.SetInt(PrefMuted, _muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume()
    {
        float v = EffectiveMusicVolume;
        if (_musicA != null) _musicA.volume = _musicA.isPlaying ? v : 0f;
        if (_musicB != null) _musicB.volume = _musicB.isPlaying ? v : 0f;
        // Keep current playing source at effective volume
        var active = _musicUsingA ? _musicA : _musicB;
        if (active != null && active.isPlaying)
            active.volume = v;
    }

    // ---------- SFX API ----------

    public static GameFxCatalog GetCatalog()
    {
        if (Instance == null)
            return null;
        return Instance.Catalog;
    }

    public static void Play(AudioClip clip, float volumeScale = 1f, float cooldown = -1f)
    {
        if (Instance == null || clip == null)
            return;
        Instance.PlayInternal(clip, volumeScale, cooldown);
    }

    public static void PlayUi(AudioClip clip)
    {
        if (Instance == null || clip == null)
            return;
        Instance.PlayInternal(clip, 1f, 0.02f);
    }

    public static void PlayButtonClick()
    {
        var catalog = GetCatalog();
        if (catalog != null && catalog.buttonClickSfx != null)
            PlayUi(catalog.buttonClickSfx);
    }

    private void PlayInternal(AudioClip clip, float volumeScale, float cooldown)
    {
        if (clip == null || EffectiveSfxVolume <= 0f)
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
        source.volume = Mathf.Clamp01(EffectiveSfxVolume * volumeScale);
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.Play();

        StartCoroutine(ReleaseWhenDone(source, clip, id));
    }

    private IEnumerator ReleaseWhenDone(AudioSource source, AudioClip clip, int clipId)
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

        if (_pool.Count > 0)
        {
            var stolen = _pool[0];
            stolen.Stop();
            return stolen;
        }

        return null;
    }

    // ---------- Music API ----------

    public void PlayMusicPlaylist(int startIndex = 0)
    {
        var cat = Catalog;
        if (cat == null || cat.musicTracks == null || cat.musicTracks.Length == 0)
            return;

        _musicStarted = true;
        _musicIndex = Mathf.Clamp(startIndex, 0, cat.musicTracks.Length - 1);
        if (cat.musicShuffle)
            _musicIndex = Random.Range(0, cat.musicTracks.Length);

        CrossfadeTo(cat.musicTracks[_musicIndex], cat.musicCrossfadeSeconds);
    }

    public void PlayMusic(AudioClip clip, float fadeSeconds = -1f)
    {
        if (clip == null)
            return;
        _musicStarted = true;
        var cat = Catalog;
        float fade = fadeSeconds >= 0f ? fadeSeconds : (cat != null ? cat.musicCrossfadeSeconds : 1f);
        CrossfadeTo(clip, fade);
    }

    public void StopMusic(float fadeSeconds = 0.5f)
    {
        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(FadeOutAllMusic(fadeSeconds));
    }

    public void PlayNextMusicTrack()
    {
        var cat = Catalog;
        if (cat == null || cat.musicTracks == null || cat.musicTracks.Length == 0)
            return;

        if (cat.musicShuffle)
            _musicIndex = Random.Range(0, cat.musicTracks.Length);
        else
            _musicIndex = (_musicIndex + 1) % cat.musicTracks.Length;

        CrossfadeTo(cat.musicTracks[_musicIndex], cat.musicCrossfadeSeconds);
    }

    private void CrossfadeTo(AudioClip clip, float fadeSeconds)
    {
        if (clip == null)
            return;

        BuildMusicSources();

        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        _musicFadeRoutine = StartCoroutine(CrossfadeRoutine(clip, Mathf.Max(0.01f, fadeSeconds)));
    }

    private IEnumerator CrossfadeRoutine(AudioClip nextClip, float fadeSeconds)
    {
        AudioSource from = _musicUsingA ? _musicA : _musicB;
        AudioSource to = _musicUsingA ? _musicB : _musicA;

        to.clip = nextClip;
        to.volume = 0f;
        to.loop = true;
        to.Play();

        float startFrom = from != null && from.isPlaying ? from.volume : 0f;
        float target = EffectiveMusicVolume;
        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeSeconds);
            if (from != null)
                from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = Mathf.Lerp(0f, target, k);
            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.clip = null;
            from.volume = 0f;
        }

        to.volume = EffectiveMusicVolume;
        _musicUsingA = !_musicUsingA;
        _musicFadeRoutine = null;
    }

    private IEnumerator FadeOutAllMusic(float fadeSeconds)
    {
        float startA = _musicA != null ? _musicA.volume : 0f;
        float startB = _musicB != null ? _musicB.volume : 0f;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / fadeSeconds);
            if (_musicA != null) _musicA.volume = startA * k;
            if (_musicB != null) _musicB.volume = startB * k;
            yield return null;
        }

        if (_musicA != null) { _musicA.Stop(); _musicA.clip = null; }
        if (_musicB != null) { _musicB.Stop(); _musicB.clip = null; }
        _musicFadeRoutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _poolSize = Mathf.Max(4, _poolSize);
        _maxSameClipVoices = Mathf.Clamp(_maxSameClipVoices, 1, 8);
    }
#endif
}
