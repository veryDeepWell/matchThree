using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop on any UI Button (or parent) to play catalog buttonClickSfx on click.
/// Also provides a static helper to wire all buttons under a root at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiClickSound : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private AudioClip _overrideClick;
    [SerializeField] private bool _playOnEnable;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlayClick);
    }

    private void OnEnable()
    {
        if (_playOnEnable)
            PlayClick();
    }

    public void PlayClick()
    {
        if (_overrideClick != null)
        {
            SoundManager.PlayUi(_overrideClick);
            return;
        }

        SoundManager.PlayButtonClick();
    }

    /// <summary>
    /// Attach UiClickSound to every Button under root that does not already have one.
    /// Call once from menu bootstrap / GameplayFlowController.Start.
    /// </summary>
    public static void WireAllButtons(Transform root)
    {
        if (root == null)
            return;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null)
                continue;
            if (btn.GetComponent<UiClickSound>() != null)
                continue;
            btn.gameObject.AddComponent<UiClickSound>();
        }
    }
}
