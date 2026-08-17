using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bind sliders/toggles in a settings panel to SoundManager volumes.
/// Assign in inspector or call Bind() from code.
/// </summary>
public sealed class AudioSettingsBinder : MonoBehaviour
{
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Toggle _muteToggle;

    private void OnEnable()
    {
        PullFromManager();
        Hook(true);
    }

    private void OnDisable()
    {
        Hook(false);
    }

    public void Bind(Slider master, Slider sfx, Slider music, Toggle mute)
    {
        Hook(false);
        _masterSlider = master;
        _sfxSlider = sfx;
        _musicSlider = music;
        _muteToggle = mute;
        PullFromManager();
        Hook(true);
    }

    private void Hook(bool on)
    {
        if (_masterSlider != null)
        {
            if (on) _masterSlider.onValueChanged.AddListener(OnMaster);
            else _masterSlider.onValueChanged.RemoveListener(OnMaster);
        }
        if (_sfxSlider != null)
        {
            if (on) _sfxSlider.onValueChanged.AddListener(OnSfx);
            else _sfxSlider.onValueChanged.RemoveListener(OnSfx);
        }
        if (_musicSlider != null)
        {
            if (on) _musicSlider.onValueChanged.AddListener(OnMusic);
            else _musicSlider.onValueChanged.RemoveListener(OnMusic);
        }
        if (_muteToggle != null)
        {
            if (on) _muteToggle.onValueChanged.AddListener(OnMute);
            else _muteToggle.onValueChanged.RemoveListener(OnMute);
        }
    }

    private void PullFromManager()
    {
        if (SoundManager.Instance == null)
            return;

        if (_masterSlider != null)
            _masterSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
        if (_sfxSlider != null)
            _sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        if (_musicSlider != null)
            _musicSlider.SetValueWithoutNotify(SoundManager.Instance.MusicVolume);
        if (_muteToggle != null)
            _muteToggle.SetIsOnWithoutNotify(SoundManager.Instance.Muted);
    }

    private void OnMaster(float v)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.MasterVolume = v;
    }

    private void OnSfx(float v)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SfxVolume = v;
    }

    private void OnMusic(float v)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.MusicVolume = v;
    }

    private void OnMute(bool muted)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.Muted = muted;
    }
}
