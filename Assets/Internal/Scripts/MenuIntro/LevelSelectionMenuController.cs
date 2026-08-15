using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelSelectionMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private LevelCatalog _catalog;
    private GameObject _panel;
    private Button _showButton;
    private Button _closeButton;
    private Button _applyButton;
    private TMP_InputField _inputField;
    private TMP_Text _actualLevelText;
    private TMP_Text _limitsText;
    private TMP_Text _errorText;
    private bool _usesLimitsTextForErrors;
    private Color _errorTextNormalColor;
    private RectTransform _content;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject controllerObject = new GameObject(nameof(LevelSelectionMenuController));
        DontDestroyOnLoad(controllerObject);
        controllerObject.AddComponent<LevelSelectionMenuController>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Configure(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Configure(scene);
    }

    private void Configure(Scene scene)
    {
        ClearReferences();
        if (scene.name != MainMenuSceneName)
            return;

        _catalog = Resources.Load<LevelCatalog>(nameof(LevelCatalog));
        _panel = FindObject(scene, "ChangeLeaveMenuPanel");
        _showButton = FindComponent<Button>(scene, "ShowChangeLeaveMenuButton");
        _closeButton = FindComponent<Button>(scene, "CloseChangeLeaveMenuButton");
        _applyButton = FindComponent<Button>(scene, "ApplyChangeLeaveMenuButton");
        _inputField = FindComponent<TMP_InputField>(scene, "LevelNumbeInputField (TMP)", "LeavelNumbeInputField (TMP)");
        _actualLevelText = FindComponent<TMP_Text>(scene, "ActualLeavelText (TMP)", "LeavelText (TMP)");
        _limitsText = FindComponent<TMP_Text>(scene, "LimitsText (TMP)");
        _errorText = FindComponent<TMP_Text>(scene, "ErrorText (TMP)");
        if (_errorText == null)
        {
            _errorText = _limitsText;
            _usesLimitsTextForErrors = _errorText != null;
        }
        if (_errorText != null)
            _errorTextNormalColor = _errorText.color;

        ScrollRect scrollRect = FindComponent<ScrollRect>(scene, "LevelScrollView");
        _content = scrollRect != null ? scrollRect.content : null;
        if (scrollRect != null)
            scrollRect.horizontal = false;

        if (_panel == null || _catalog == null)
        {
            Debug.LogError("[LevelSelection] Panel or LevelCatalog was not found.");
            return;
        }

        AddListener(_showButton, ShowPanel);
        AddListener(_closeButton, ClosePanel);
        AddListener(_applyButton, ApplySelectedLevel);

        BuildLevelList();
        RefreshTexts();
        SetError(string.Empty);
        _panel.SetActive(false);
        RefreshAvailability();
    }

    private void BuildLevelList()
    {
        if (_content == null || _applyButton == null)
            return;

        for (int childIndex = _content.childCount - 1; childIndex >= 0; childIndex--)
            Destroy(_content.GetChild(childIndex).gameObject);

        ContentSizeFitter fitter = _content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int index = 0; index < _catalog.Count; index++)
        {
            LevelData level = _catalog.GetLevel(index);
            if (level == null)
                continue;

            int displayedNumber = index + 1;
            GameObject itemObject = Instantiate(_applyButton.gameObject, _content);
            itemObject.name = $"LevelButton_{displayedNumber}";
            itemObject.SetActive(true);

            Button itemButton = itemObject.GetComponent<Button>();
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => SelectLevelNumber(displayedNumber));

            TMP_Text itemText = itemObject.GetComponentInChildren<TMP_Text>(true);
            if (itemText != null)
                itemText.text = $"Уровень {displayedNumber}";

            LayoutElement layout = itemObject.GetComponent<LayoutElement>();
            if (layout == null)
                layout = itemObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 70f;
        }
    }

    private void ShowPanel()
    {
        RefreshTexts();
        SetError(string.Empty);
        _panel.SetActive(true);
    }

    private void ClosePanel()
    {
        _panel.SetActive(false);
    }

    private void SelectLevelNumber(int levelNumber)
    {
        if (_inputField != null)
            _inputField.text = levelNumber.ToString();
        SetError(string.Empty);
    }

    private void ApplySelectedLevel()
    {
        if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text))
        {
            SetError("Введите номер уровня.");
            return;
        }

        if (!int.TryParse(_inputField.text.Trim(), out int levelNumber))
        {
            SetError("Номер уровня должен быть целым числом.");
            return;
        }

        if (levelNumber < 1 || levelNumber > _catalog.Count)
        {
            SetError($"Введите номер от 1 до {_catalog.Count}.");
            return;
        }

        LevelData selectedLevel = _catalog.GetLevel(levelNumber - 1);
        if (selectedLevel == null)
        {
            SetError("Данные выбранного уровня отсутствуют.");
            return;
        }

        SaveService saveService = SaveService.Instance;
        if (saveService == null)
        {
            SetError("Система сохранений ещё не готова.");
            return;
        }

        saveService.SelectLevelForReplay(selectedLevel, levelNumber);
        RefreshTexts();
        SetError(string.Empty);
    }

    private void RefreshTexts()
    {
        int currentLevelNumber = 1;
        SaveService saveService = SaveService.Instance;
        if (saveService != null && saveService.Data != null && saveService.Data.LevelProgress != null)
            currentLevelNumber = Math.Max(1, saveService.Data.LevelProgress.CurrentLevelNumber);

        if (_actualLevelText != null)
            _actualLevelText.text = $"Текущий уровень: {currentLevelNumber}";
        if (_limitsText != null)
            _limitsText.text = $"Допустимые значения: от 1 до {_catalog.Count}";
    }

    private void RefreshAvailability()
    {
        if (_showButton == null)
            return;

        SaveService saveService = SaveService.Instance;
        _showButton.gameObject.SetActive(
            saveService != null &&
            saveService.Data != null &&
            saveService.Data.LevelProgress != null &&
            saveService.Data.LevelProgress.AllAvailableLevelsCompleted);
    }

    private void SetError(string message)
    {
        if (_errorText == null)
        {
            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning($"[LevelSelection] {message} ErrorText (TMP) was not found.");
            return;
        }

        if (_usesLimitsTextForErrors)
        {
            if (string.IsNullOrEmpty(message))
            {
                _errorText.color = _errorTextNormalColor;
                _errorText.text = $"Допустимые значения: от 1 до {_catalog.Count}";
            }
            else
            {
                _errorText.color = Color.red;
                _errorText.text = message;
            }
            return;
        }

        _errorText.text = message;
        _errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private void ClearReferences()
    {
        _panel = null;
        _showButton = null;
        _closeButton = null;
        _applyButton = null;
        _inputField = null;
        _actualLevelText = null;
        _limitsText = null;
        _errorText = null;
        _usesLimitsTextForErrors = false;
        _content = null;
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static T FindComponent<T>(Scene scene, params string[] names) where T : Component
    {
        foreach (string objectName in names)
        {
            GameObject found = FindObject(scene, objectName);
            if (found != null && found.TryGetComponent(out T component))
                return component;
        }

        return null;
    }

    private static GameObject FindObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }
}
