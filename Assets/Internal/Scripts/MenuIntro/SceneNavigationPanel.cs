using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class SceneNavigationPanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string _sceneName;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(_sceneName))
        {
            Debug.LogError($"[{nameof(SceneNavigationPanel)}] Сцена для перехода не указана.", this);
            return;
        }

        SoundManager.PlayButtonClick();
        SceneManager.LoadScene(_sceneName);
    }
}
