using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public sealed class CheatMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainMenuSceneName)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform cheatPanel = FindDescendant(root.transform, "CheatPanel");
            if (cheatPanel == null)
                continue;
            if (cheatPanel.GetComponent<CheatMenuController>() == null)
                cheatPanel.gameObject.AddComponent<CheatMenuController>();
            return;
        }
    }

    private void Awake()
    {
        ConfigureScrollView();
        RefreshLevelSelectionLabel();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            Transform itemPanel = FindParentItemPanel(button.transform);
            if (itemPanel != null)
            {
                string description = ReadPanelText(itemPanel);
                button.onClick.AddListener(() => Execute(description, itemPanel));
                continue;
            }

            if (HasText(button.transform, "X"))
                button.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    private void Execute(string description, Transform itemPanel)
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null)
            return;

        string normalized = description.ToLowerInvariant();
        int value = ReadNumber(itemPanel, description, 0);

        // У полного сброса длинное описание содержит слова «валюты» и «бонусы»,
        // поэтому составную команду необходимо проверять раньше отдельных.
        if (normalized.Contains("полное обнуление"))
            saveService.ResetTestingProgress();
        else if (normalized.Contains("кристал"))
            saveService.AddCrystalsForTesting(value);
        else if (normalized.Contains("золота"))
            saveService.AddGoldForTesting(value);
        else if (normalized.Contains("бонут") || normalized.Contains("бонус"))
            saveService.SetAllGameplayBonuses(Mathf.Max(0, value));
        else if (normalized.Contains("разблокировать") || normalized.Contains("заблокировать"))
        {
            saveService.ToggleLevelSelectionForTesting();
            UpdateLevelSelectionLabel(itemPanel);
        }
        else if (normalized.Contains("обнулить жизни"))
            saveService.RestoreAllLives();
        else if (normalized.Contains("включить читы"))
            saveService.SetLevelCheatsEnabled(true);
        else if (normalized.Contains("отключить читы"))
            saveService.SetLevelCheatsEnabled(false);
        else
            Debug.LogWarning($"[Cheats] Неизвестная команда: '{description}'.");

        RefreshLevelSelectionLabel();
        SoundManager.PlayButtonClick();
    }

    private void RefreshLevelSelectionLabel()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform item in transforms)
        {
            if (item.name.StartsWith("CheatItemPanel", StringComparison.Ordinal))
            {
                string description = ReadPanelText(item).ToLowerInvariant();
                if (description.Contains("разблокировать") || description.Contains("заблокировать"))
                    UpdateLevelSelectionLabel(item);
            }
        }

        Transform showLevelSelectionButton = FindDescendant(transform.root, "ShowChangeLeaveMenuButton");
        if (showLevelSelectionButton != null)
        {
            bool selectionEnabled = SaveService.Instance != null && SaveService.Instance.Data != null &&
                                    SaveService.Instance.Data.LevelProgress != null &&
                                    SaveService.Instance.Data.LevelProgress.AllAvailableLevelsCompleted;
            showLevelSelectionButton.gameObject.SetActive(selectionEnabled);
        }
    }

    private static void UpdateLevelSelectionLabel(Transform itemPanel)
    {
        bool selectionEnabled = false;
        SaveService saveService = SaveService.Instance;
        if (saveService != null && saveService.Data != null && saveService.Data.LevelProgress != null)
            selectionEnabled = saveService.Data.LevelProgress.AllAvailableLevelsCompleted;

        TMP_Text[] texts = itemPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            string normalized = text.text.ToLowerInvariant();
            if (!normalized.Contains("разблокировать") && !normalized.Contains("заблокировать"))
                continue;

            text.text = selectionEnabled
                ? "заблокировать выбор уровней"
                : "разблокировать выбор уровней";
            return;
        }
    }

    private void ConfigureScrollView()
    {
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null || scrollRect.content == null)
            return;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        Vector2 position = scrollRect.content.anchoredPosition;
        position.x = 0f;
        scrollRect.content.anchoredPosition = position;

        VerticalLayoutGroup layout = scrollRect.content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = scrollRect.content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static int ReadNumber(Transform panel, string description, int fallback)
    {
        TMP_InputField input = panel.GetComponentInChildren<TMP_InputField>(true);
        if (input != null && int.TryParse(input.text, out int value))
            return value;

        Match number = Regex.Match(description.ToLowerInvariant(), @"(\d+)\s*([кk]?)");
        if (number.Success && int.TryParse(number.Groups[1].Value, out value))
        {
            bool thousands = !string.IsNullOrEmpty(number.Groups[2].Value);
            return thousands ? value * 1000 : value;
        }
        return fallback;
    }

    private static string ReadPanelText(Transform panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        string description = string.Empty;
        foreach (TMP_Text text in texts)
        {
            string value = text.text.Trim();
            if (!string.IsNullOrWhiteSpace(value) && value != "выбрать")
                description += " " + value;
        }
        return description.Trim();
    }

    private static bool HasText(Transform root, string expected)
    {
        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        return text != null && string.Equals(text.text.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static Transform FindParentItemPanel(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name.StartsWith("CheatItemPanel", StringComparison.Ordinal))
                return current;
            current = current.parent;
        }
        return null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindDescendant(root.GetChild(index), objectName);
            if (found != null)
                return found;
        }
        return null;
    }
}
