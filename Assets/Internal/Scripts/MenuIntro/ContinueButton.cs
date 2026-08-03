using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ContinueButton : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        RefreshState();
        _button.onClick.AddListener(ContinueGame);

        if (SaveService.Instance != null)
            SaveService.Instance.Saved += HandleSaved;
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(ContinueGame);

        if (SaveService.Instance != null)
            SaveService.Instance.Saved -= HandleSaved;
    }

    public void RefreshState()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        _button.interactable = SaveService.Instance != null && SaveService.Instance.HasRunningLevel;
    }

    private void ContinueGame()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || !saveService.HasRunningLevel)
        {
            RefreshState();
            return;
        }

        saveService.RequestContinue();
        SceneManager.LoadScene(battleSceneName);
    }

    private void HandleSaved(SaveReason reason)
    {
        RefreshState();
    }
}
