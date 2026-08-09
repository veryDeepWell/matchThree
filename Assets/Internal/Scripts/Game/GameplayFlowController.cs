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

    [Header("Continue settings")]
    [SerializeField, Min(1)] private int extraTimeSeconds = 120;
    [SerializeField, Min(0)] private int goldContinueCost = 1000;

    private GameObject _pausePanel;
    private GameObject _continueOfferPanel;
    private GameObject _losePanel;
    private GameObject _winPanel;
    private GameObject _userUiPanel;
    private TMP_Text _timerText;
    private Board _board;

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
        ApplySavedState();
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
    }

    private void OnDestroy()
    {
        // Не оставляем следующую сцену и Editor с остановленным временем.
        Time.timeScale = 1f;
    }

    public void OpenPause()
    {
        if (GetSavedStatus() != LevelSessionStatus.InProgress)
            return;

        SetOnlyResultPanel(null);
        SetPanelActive(_pausePanel, true);
        SetPanelActive(_userUiPanel, false);
        Time.timeScale = 0f;

        SaveCurrentBoard(SaveReason.LevelPaused);
    }

    public void ResumeFromPause()
    {
        SetPanelActive(_pausePanel, false);
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
        if (economy.Gold < goldContinueCost)
        {
            Debug.LogWarning($"[GameplayFlow] Not enough gold. Required: {goldContinueCost}.");
            return;
        }

        economy.Gold -= goldContinueCost;
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
        SetOnlyResultPanel(_winPanel);
        Time.timeScale = 0f;
        SaveStatusAndBoard(LevelSessionStatus.Victory, SaveReason.LevelVictory);
    }

    public void ShowContinueOffer()
    {
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
        runningLevel.ExtraTimeUses++;
        runningLevel.RemainingTime += extraTimeSeconds;
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
            UpdateTimerText(runningLevel.RemainingTime);

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

        Transform timerTransform = FindDescendant(transform, "TimerText (TMP)");
        if (timerTransform != null)
            _timerText = timerTransform.GetComponent<TMP_Text>();
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
