using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;

public sealed class GameplayFlowController : MonoBehaviour
{
    private const string ExtraTimeRewardId = "extra_time";

    [Header("Scene settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string battleSceneName = "BattleScene";

    private GameObject _pausePanel;
    private GameObject _continueOfferPanel;
    private GameObject _losePanel;
    private GameObject _winPanel;
    private GameObject _userUiPanel;
    private GameObject _backgroundPanel;
    private GameObject _goalBoardPanel;
    private GameObject _goalPanelTemplate;
    private TMP_Text _timerText;
    private TMP_Text _goldContinuePriceText;
    private Board _board;
    private MatchesHandler _matchesHandler;
    private float _remainingTime;
    private readonly List<GoalUiEntry> _goalUiEntries = new List<GoalUiEntry>();
    private readonly List<BonusUiEntry> _bonusUiEntries = new List<BonusUiEntry>();
    private string _selectedBonusId = string.Empty;

    private sealed class GoalUiEntry
    {
        public GoalProgressSaveData Goal;
        public TMP_Text Text;
    }

    private sealed class BonusUiEntry
    {
        public string BonusId;
        public Button Button;
        public Image Background;
        public Color NormalColor;
        public TMP_Text CountText;
    }

    private void Awake()
    {
        EnsureEventSystemExists();
        ResolveReferences();
        DisableDecorativeRaycasts();
        ConfigureButtons();
    }

    private void Start()
    {
        _board = FindFirstObjectByType<Board>();
        _matchesHandler = FindFirstObjectByType<MatchesHandler>();
        if (_board != null)
            _board.ItemsCollected += HandleItemsCollected;
        ApplySavedState();
        BuildGoalPanels();
        BuildBonusInventory();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Временные клавиши для тестирования, пока победа и поражение не связаны с целями и таймером.
        if (Input.GetKeyDown(KeyCode.W))
            DebugWin();

        if (Input.GetKeyDown(KeyCode.L))
            DebugLose();
#endif

        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel == null || runningLevel.Status != LevelSessionStatus.InProgress)
            return;

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        runningLevel.RemainingTime = _remainingTime;
        UpdateTimerText(_remainingTime);

        if (_remainingTime <= 0f)
            ShowContinueOffer();
    }

    private void OnDestroy()
    {
        if (_board != null)
            _board.ItemsCollected -= HandleItemsCollected;
        if (SaveService.Instance != null)
            SaveService.Instance.Saved -= HandleInventorySaved;
        // Не оставляем следующую сцену и Editor с остановленным временем.
        Time.timeScale = 1f;
    }

    public void OpenPause()
    {
        if (GetSavedStatus() != LevelSessionStatus.InProgress)
            return;

        SetOnlyResultPanel(null);
        SetPanelActive(_pausePanel, true);
        SetPanelActive(_backgroundPanel, true);
        SetPanelActive(_userUiPanel, false);
        Time.timeScale = 0f;

        SaveCurrentBoard(SaveReason.LevelPaused);
    }

    public void ResumeFromPause()
    {
        SetPanelActive(_pausePanel, false);
        SetPanelActive(_backgroundPanel, false);
        SetPanelActive(_userUiPanel, true);
        Time.timeScale = 1f;
    }

    public void ReturnToMainMenu()
    {
        SaveCurrentBoard(SaveReason.Manual);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void CompleteLevelAndReturnToMainMenu()
    {
        CompleteCurrentLevel();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void CompleteLevelAndRestart()
    {
        CompleteCurrentLevel();
        Time.timeScale = 1f;
        SceneManager.LoadScene(battleSceneName);
    }

    public void RestartLevel()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService != null)
            saveService.FinishRunningLevelWithDefeat();

        Time.timeScale = 1f;
        SceneManager.LoadScene(battleSceneName);
    }

    public void FinishDefeatAndReturnToMainMenu()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService != null)
            saveService.FinishRunningLevelWithDefeat();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void WatchAdForExtraTime()
    {
        AdvertisingService advertisingService = AdvertisingService.Instance;
        if (advertisingService == null)
        {
            Debug.LogError("[GameplayFlow] AdvertisingService is not available.");
            return;
        }

        bool started = advertisingService.ShowRewarded(
            ExtraTimeRewardId,
            GrantExtraTime,
            HandleExtraTimeAdvertisementClosed);
        if (!started)
            Debug.LogWarning("[GameplayFlow] Extra-time advertisement was not started.");
    }

    public void BuyExtraTimeForGold()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || saveService.Data == null || saveService.Data.Economy == null)
            return;

        EconomySaveData economy = saveService.Data.Economy;
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel == null)
            return;

        int goldContinueCost = Mathf.Max(0, runningLevel.ExtraTimeGoldCost);
        if (economy.Gold < goldContinueCost)
        {
            Debug.LogWarning($"[GameplayFlow] Not enough gold. Required: {goldContinueCost}.");
            return;
        }

        if (!saveService.TrySpendGold(goldContinueCost))
            return;
        GrantExtraTime();
    }

    public void DeclineExtraTime()
    {
        ShowFinalDefeat();
    }

    public void DebugWin()
    {
        ShowVictory();
    }

    public void DebugLose()
    {
        ShowContinueOffer();
    }

    public void ShowVictory()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService != null)
            saveService.GrantVictoryRewards();

        SetOnlyResultPanel(_winPanel);
        Time.timeScale = 0f;
        SaveStatusAndBoard(LevelSessionStatus.Victory, SaveReason.LevelVictory);
    }

    public void ShowContinueOffer()
    {
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel != null && !runningLevel.AllowRepeatedExtraTime && runningLevel.ExtraTimeUses > 0)
        {
            ShowFinalDefeat();
            return;
        }

        SetOnlyResultPanel(_continueOfferPanel);
        Time.timeScale = 0f;
        SaveStatusAndBoard(LevelSessionStatus.ContinueOffer, SaveReason.ContinueOffer);
    }

    public void ShowFinalDefeat()
    {
        SetOnlyResultPanel(_losePanel);
        Time.timeScale = 0f;
        SaveStatusAndBoard(LevelSessionStatus.Defeat, SaveReason.LevelDefeat);
    }

    private void GrantExtraTime()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || saveService.Data == null || saveService.Data.RunningLevel == null)
            return;

        RunningLevelSaveData runningLevel = saveService.Data.RunningLevel;
        if (!runningLevel.AllowRepeatedExtraTime && runningLevel.ExtraTimeUses > 0)
        {
            ShowFinalDefeat();
            return;
        }

        runningLevel.ExtraTimeUses++;
        runningLevel.RemainingTime += Mathf.Max(1, runningLevel.ExtraTimeSeconds);
        _remainingTime = runningLevel.RemainingTime;
        runningLevel.Status = LevelSessionStatus.InProgress;

        SetOnlyResultPanel(null);
        RestoreGameplayStateAfterAdvertisement();
        UpdateTimerText(runningLevel.RemainingTime);
        SaveCurrentBoard(SaveReason.ExtraTimeGranted);
    }

    private void HandleExtraTimeAdvertisementClosed(bool rewardGranted)
    {
        if (!rewardGranted)
            return;

        // PluginYG может восстановить состояние приложения только после callback награды.
        // Поэтому окончательно возобновляем игру именно после закрытия рекламного окна.
        SetOnlyResultPanel(null);
        RestoreGameplayStateAfterAdvertisement();
    }

    private static void RestoreGameplayStateAfterAdvertisement()
    {
        // Если объект паузы PluginYG ещё существует, SetState меняет сохранённое
        // состояние, которое плагин применит после полного закрытия рекламы.
        // Если объект уже удалён, метод сразу применит значения к игре.
        PauseGameYG.SetState(
            timeScale: 1f,
            audioPause: false,
            cursorEnable: true);
    }

    private void ApplySavedState()
    {
        LevelSessionStatus status = GetSavedStatus();
        RunningLevelSaveData runningLevel = GetRunningLevel();

        if (runningLevel != null)
        {
            _remainingTime = runningLevel.RemainingTime;
            UpdateTimerText(runningLevel.RemainingTime);
            UpdateContinuePriceText(runningLevel.ExtraTimeGoldCost);
        }

        switch (status)
        {
            case LevelSessionStatus.ContinueOffer:
                SetOnlyResultPanel(_continueOfferPanel);
                Time.timeScale = 0f;
                break;

            case LevelSessionStatus.Victory:
                SetOnlyResultPanel(_winPanel);
                Time.timeScale = 0f;
                break;

            case LevelSessionStatus.Defeat:
                SetOnlyResultPanel(_losePanel);
                Time.timeScale = 0f;
                break;

            default:
                SetOnlyResultPanel(null);
                Time.timeScale = 1f;
                break;
        }
    }

    private void HandleItemsCollected(string itemId, int amount)
    {
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel == null || runningLevel.Status != LevelSessionStatus.InProgress)
            return;

        foreach (GoalProgressSaveData goal in runningLevel.Goals)
        {
            if (goal.TargetItemId == itemId)
                goal.CurrentCount = Mathf.Clamp(goal.CurrentCount + amount, 0, goal.RequiredCount);
        }

        bool hasGoals = runningLevel.Goals.Count > 0;
        bool allGoalsComplete = hasGoals;
        foreach (GoalProgressSaveData goal in runningLevel.Goals)
        {
            if (goal.CurrentCount < goal.RequiredCount)
            {
                allGoalsComplete = false;
                break;
            }
        }

        SaveCurrentBoard(SaveReason.Manual);
        RefreshGoalPanels();
        if (allGoalsComplete)
            ShowVictory();
    }

    private void BuildGoalPanels()
    {
        _goalUiEntries.Clear();
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (_goalBoardPanel == null || _goalPanelTemplate == null || runningLevel == null)
            return;

        bool hasGoals = runningLevel.Goals != null && runningLevel.Goals.Count > 0;
        _goalBoardPanel.SetActive(hasGoals);
        if (!hasGoals)
            return;

        ItemHandler itemHandler = FindFirstObjectByType<ItemHandler>();
        ItemRegistry registry = itemHandler != null ? itemHandler.GetRegistry() : null;
        RectTransform templateRect = _goalPanelTemplate.GetComponent<RectTransform>();
        Vector2 templatePosition = templateRect != null ? templateRect.anchoredPosition : Vector2.zero;
        float verticalStep = templateRect != null ? templateRect.rect.height : 100f;

        for (int i = 0; i < runningLevel.Goals.Count; i++)
        {
            GoalProgressSaveData goal = runningLevel.Goals[i];
            GameObject panel = i == 0
                ? _goalPanelTemplate
                : Instantiate(_goalPanelTemplate, _goalPanelTemplate.transform.parent);

            panel.name = $"GoalPanel_{i + 1}";
            panel.SetActive(true);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
                panelRect.anchoredPosition = templatePosition + Vector2.down * verticalStep * i;

            Transform imageTransform = FindDescendant(panel.transform, "GoalImage");
            Image goalImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            ItemDefinition definition = registry != null ? registry.Get(goal.TargetItemId) : null;
            if (goalImage != null)
            {
                goalImage.sprite = definition != null ? definition.Icon : null;
                goalImage.enabled = goalImage.sprite != null;
                goalImage.preserveAspect = true;
            }

            Transform textTransform = FindDescendant(panel.transform, "GoalText (TMP)");
            TMP_Text goalText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            _goalUiEntries.Add(new GoalUiEntry { Goal = goal, Text = goalText });
        }

        RefreshGoalPanels();
    }

    private void RefreshGoalPanels()
    {
        foreach (GoalUiEntry entry in _goalUiEntries)
        {
            if (entry.Goal == null || entry.Text == null)
                continue;

            int remaining = Mathf.Max(0, entry.Goal.RequiredCount - entry.Goal.CurrentCount);
            entry.Text.text = remaining.ToString();
        }
    }

    public bool HandleBonusCellClick(Item item)
    {
        if (string.IsNullOrEmpty(_selectedBonusId))
            return false;

        // Пока выбран бонус, клик предназначен только для его размещения:
        // неудачный клик не должен случайно превратиться в свайп фишки.
        if (item == null || item.Board == null || item.Board.IsProcessing ||
            GetSavedStatus() != LevelSessionStatus.InProgress)
            return true;

        SaveService saveService = SaveService.Instance;
        if (saveService == null || saveService.GetBonusCount(_selectedBonusId) <= 0)
        {
            CancelBonusSelection();
            return true;
        }

        ItemGenerator generator = FindFirstObjectByType<ItemGenerator>();
        if (generator == null)
            return true;

        string placedBonusId = _selectedBonusId;
        bool placed = generator.ReplaceWithSpecial(item.Board, item.Column, item.Row, placedBonusId);
        if (!placed)
            return true;

        if (saveService.TryConsumeBonus(placedBonusId))
            SaveCurrentBoard(SaveReason.RewardGranted);

        CancelBonusSelection();
        RefreshBonusInventory();
        return true;
    }

    private void BuildBonusInventory()
    {
        _bonusUiEntries.Clear();
        AddBonusSlot("BombBonusSlotPanel", "bomb");
        AddBonusSlot("LineSweeperXBonusSlotPanel", "sweeper_h");
        AddBonusSlot("LineSweeperBonusSlotPanel", "sweeper_cross");
        AddBonusSlot("MagnetBonusSlotBombPanel", "magnet");
        AddBonusSlot("LineSweeperYBonusSlotPanel", "sweeper_v");

        Transform plusTwoTransform = FindDescendant(transform, "Plus2Bonus");
        Button plusTwoButton = plusTwoTransform != null ? plusTwoTransform.GetComponent<Button>() : null;
        if (plusTwoTransform != null && plusTwoButton == null)
            plusTwoButton = plusTwoTransform.gameObject.AddComponent<Button>();
        if (plusTwoButton != null)
            plusTwoButton.onClick.AddListener(SetAllBonusesToTwo);

        SaveService saveService = SaveService.Instance;
        if (saveService != null)
            saveService.Saved += HandleInventorySaved;

        RefreshBonusInventory();
    }

    private void AddBonusSlot(string panelName, string bonusId)
    {
        Transform panelTransform = FindDescendant(transform, panelName);
        if (panelTransform == null)
        {
            Debug.LogWarning($"[GameplayFlow] Bonus panel '{panelName}' was not found.");
            return;
        }

        Image background = panelTransform.GetComponent<Image>();
        Button button = panelTransform.GetComponent<Button>();
        if (button == null)
            button = panelTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Transform countTransform = FindDescendant(panelTransform, "BonusNumberText (TMP)");
        TMP_Text countText = countTransform != null ? countTransform.GetComponent<TMP_Text>() : null;
        if (countText == null)
            countText = panelTransform.GetComponentInChildren<TMP_Text>(true);

        if (countText == null)
            Debug.LogWarning($"[GameplayFlow] Counter text inside '{panelName}' was not found.");
        var entry = new BonusUiEntry
        {
            BonusId = bonusId,
            Button = button,
            Background = background,
            NormalColor = background != null ? background.color : Color.white,
            CountText = countText
        };
        button.onClick.AddListener(() => ToggleBonusSelection(bonusId));
        _bonusUiEntries.Add(entry);
    }

    private void ToggleBonusSelection(string bonusId)
    {
        if (_selectedBonusId == bonusId)
            _selectedBonusId = string.Empty;
        else if (string.IsNullOrEmpty(_selectedBonusId) && SaveService.Instance != null &&
                 SaveService.Instance.GetBonusCount(bonusId) > 0)
            _selectedBonusId = bonusId;

        RefreshBonusInventory();
    }

    private void CancelBonusSelection()
    {
        _selectedBonusId = string.Empty;
        RefreshBonusInventory();
    }

    private void SetAllBonusesToTwo()
    {
        SaveService.Instance?.SetAllGameplayBonuses(2);
        CancelBonusSelection();
    }

    private void HandleInventorySaved(SaveReason reason)
    {
        RefreshBonusInventory();
    }

    private void RefreshBonusInventory()
    {
        SaveService saveService = SaveService.Instance;
        foreach (BonusUiEntry entry in _bonusUiEntries)
        {
            int count = saveService != null ? saveService.GetBonusCount(entry.BonusId) : 0;
            if (entry.CountText != null)
                entry.CountText.text = count.ToString();

            bool selected = entry.BonusId == _selectedBonusId;
            if (entry.Background != null)
                entry.Background.color = selected
                    ? Color.Lerp(entry.NormalColor, Color.green, 0.45f)
                    : entry.NormalColor;

            if (entry.Button != null)
            {
                bool anotherBonusSelected = !string.IsNullOrEmpty(_selectedBonusId) && !selected;
                entry.Button.interactable = !anotherBonusSelected && (count > 0 || selected);
            }
        }
    }

    private void UpdateContinuePriceText(int price)
    {
        if (_goldContinuePriceText != null)
            _goldContinuePriceText.text = Mathf.Max(0, price).ToString();
    }

    private RunningLevelSaveData GetRunningLevel()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null)
            return null;

        if (saveService.Data == null)
            return null;

        return saveService.Data.RunningLevel;
    }

    private LevelSessionStatus GetSavedStatus()
    {
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel == null)
            return LevelSessionStatus.InProgress;

        return runningLevel.Status;
    }

    private void SaveStatusAndBoard(LevelSessionStatus status, SaveReason reason)
    {
        RunningLevelSaveData runningLevel = GetRunningLevel();
        if (runningLevel == null)
            return;

        runningLevel.Status = status;
        SaveCurrentBoard(reason);
    }

    private void SaveCurrentBoard(SaveReason reason)
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null)
            return;

        if (_board == null)
            _board = FindFirstObjectByType<Board>();

        if (_board != null && _board.Data != null)
            saveService.CaptureBoard(_board, reason);
        else
            saveService.SaveNow(reason);
    }

    private static void CompleteCurrentLevel()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null)
            return;

        saveService.CompleteRunningLevel();
    }

    private void UpdateTimerText(float seconds)
    {
        if (_timerText == null)
            return;

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        _timerText.text = $"{minutes}:{remainingSeconds:00}";
    }

    private void SetOnlyResultPanel(GameObject activePanel)
    {
        SetPanelActive(_pausePanel, false);
        SetPanelActive(_continueOfferPanel, activePanel == _continueOfferPanel);
        SetPanelActive(_losePanel, activePanel == _losePanel);
        SetPanelActive(_winPanel, activePanel == _winPanel);
        SetPanelActive(_backgroundPanel, activePanel != null);
        SetPanelActive(_userUiPanel, activePanel == null);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private static void EnsureEventSystemExists()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
            return;

        // Без EventSystem Canvas отображается, но кнопки не получают клики.
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void ResolveReferences()
    {
        _pausePanel = FindGameObject(transform, "PausePanel");
        _continueOfferPanel = FindGameObject(transform, "ContinueOfferPanel");
        _losePanel = FindGameObject(transform, "LosePanel");
        _winPanel = FindGameObject(transform, "WinPanel");
        _userUiPanel = FindGameObject(transform, "UserUIPanel");
        _backgroundPanel = FindGameObject(transform, "BackgroundPanel");
        _goalBoardPanel = FindGameObject(transform, "GoalBoardPanel");
        if (_goalBoardPanel != null)
            _goalPanelTemplate = FindGameObject(_goalBoardPanel.transform, "GoalPanel");

        Transform timerTransform = FindDescendant(transform, "TimerText (TMP)");
        if (timerTransform != null)
            _timerText = timerTransform.GetComponent<TMP_Text>();

        Button goldContinueButton = FindButton(_continueOfferPanel, "GoldContinueButton");
        if (goldContinueButton != null)
        {
            Transform priceTransform = FindDescendant(goldContinueButton.transform, "PrizeNumberText (TMP)");
            if (priceTransform != null)
                _goldContinuePriceText = priceTransform.GetComponent<TMP_Text>();
        }
    }

    private void DisableDecorativeRaycasts()
    {
        DisableDecorativeRaycasts(_pausePanel);
        DisableDecorativeRaycasts(_continueOfferPanel);
        DisableDecorativeRaycasts(_losePanel);
        DisableDecorativeRaycasts(_winPanel);
    }

    private static void DisableDecorativeRaycasts(GameObject panel)
    {
        if (panel == null)
            return;

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            // Фон панели блокирует клики по игровому полю за модальным окном.
            if (graphic.gameObject == panel)
                continue;

            // Графика самой кнопки и её надписей должна продолжать принимать указатель.
            Button parentButton = graphic.GetComponentInParent<Button>(true);
            if (parentButton == null)
                graphic.raycastTarget = false;
        }
    }

    private void ConfigureButtons()
    {
        AddButtonListener(transform, "PauseButton", OpenPause);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Временные кнопки для проверки экранов до подключения целей и таймера уровня.
        AddButtonListener(transform, "WinButton", DebugWin);
        AddButtonListener(transform, "LoseButton", DebugLose);
#endif

        AddButtonListener(_pausePanel, "ContinueButton", ResumeFromPause);
        AddButtonListener(_pausePanel, "MainMenuButton", ReturnToMainMenu);

        AddButtonListener(_continueOfferPanel, "AdvertisingContinueButton", WatchAdForExtraTime);
        AddButtonListener(_continueOfferPanel, "GoldContinueButton", BuyExtraTimeForGold);
        AddButtonListener(_continueOfferPanel, "CloseThisPanelButton", DeclineExtraTime);

        AddButtonListener(_losePanel, "RestartLevelButton", RestartLevel);
        AddButtonListener(_losePanel, "MainMenuButton", FinishDefeatAndReturnToMainMenu);
        AddButtonListener(_winPanel, "MainMenuButton", CompleteLevelAndReturnToMainMenu);

        // Пока уровень один, эта кнопка завершает попытку и запускает тот же уровень заново.
        AddButtonListener(_winPanel, "NextLevelButton", CompleteLevelAndRestart);
    }

    private static void AddButtonListener(Transform root, string objectName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindButton(root, objectName);
        if (button == null)
        {
            Debug.LogWarning($"[GameplayFlow] Button '{objectName}' was not found.");
            return;
        }

        button.onClick.AddListener(action);
    }

    private static void AddButtonListener(GameObject root, string objectName, UnityEngine.Events.UnityAction action)
    {
        if (root == null)
            return;

        AddButtonListener(root.transform, objectName, action);
    }

    private static Button FindButton(Transform root, string objectName)
    {
        Transform buttonTransform = FindDescendant(root, objectName);
        if (buttonTransform == null)
            return null;

        return buttonTransform.GetComponent<Button>();
    }

    private static GameObject FindGameObject(Transform root, string objectName)
    {
        Transform foundTransform = FindDescendant(root, objectName);
        if (foundTransform == null)
            return null;

        return foundTransform.gameObject;
    }

    private static Button FindButton(GameObject root, string objectName)
    {
        if (root == null)
            return null;

        return FindButton(root.transform, objectName);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindDescendant(child, objectName);
            if (result != null)
                return result;
        }

        return null;
    }
}
