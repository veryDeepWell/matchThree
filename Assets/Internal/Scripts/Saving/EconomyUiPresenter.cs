using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public sealed class EconomyUiPresenter : MonoBehaviour
{
    private static EconomyUiPresenter _instance;
    private float _nextTimerRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        var presenterObject = new GameObject(nameof(EconomyUiPresenter));
        _instance = presenterObject.AddComponent<EconomyUiPresenter>();
        DontDestroyOnLoad(presenterObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SubscribeToSaveService();
        Refresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (SaveService.Instance != null)
            SaveService.Instance.Saved -= HandleSaved;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextTimerRefreshTime)
            return;

        _nextTimerRefreshTime = Time.unscaledTime + 1f;
        Refresh();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SubscribeToSaveService();
        Refresh();
    }

    private void SubscribeToSaveService()
    {
        if (SaveService.Instance == null)
            return;

        SaveService.Instance.Saved -= HandleSaved;
        SaveService.Instance.Saved += HandleSaved;
    }

    private void HandleSaved(SaveReason reason)
    {
        Refresh();
    }

    private void Refresh()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || saveService.Data == null || saveService.Data.Economy == null)
            return;

        bool livesChanged = saveService.RefreshLives();
        EconomySaveData economy = saveService.Data.Economy;
        SetAllTexts("GoldText (TMP)", economy.Gold.ToString());
        SetAllTexts("CristalText (TMP)", economy.Crystals.ToString());
        SetAllTexts("LifeText (TMP)", economy.Lives.ToString());
        UpdateLevelNumber(saveService.Data.LevelProgress);
        UpdateLifeTimer(economy);

        // Восстановленная жизнь должна сохраниться, иначе после перезапуска
        // игры игрок снова увидит старое количество жизней.
        if (livesChanged)
            saveService.SaveNow(SaveReason.Manual);
    }

    private static void UpdateLevelNumber(LevelProgressSaveData levelProgress)
    {
        if (levelProgress == null)
            return;

        int levelNumber = Math.Max(1, levelProgress.CurrentLevelNumber);
        SetAllTexts("LeavelNumberText (TMP)", $"Уровень {levelNumber}");
    }

    private static void UpdateLifeTimer(EconomySaveData economy)
    {
        bool timerIsRunning = economy.NextLifeRestoreUtcSeconds > 0;
        SetAllTextsActive("LifeTimerText (TMP)", timerIsRunning);
        if (!timerIsRunning)
            return;

        long currentUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long remainingSeconds = Math.Max(0, economy.NextLifeRestoreUtcSeconds - currentUtcSeconds);
        long minutes = remainingSeconds / 60;
        long seconds = remainingSeconds % 60;
        SetAllTexts("LifeTimerText (TMP)", $"{minutes:00}:{seconds:00}");
    }

    private static void SetAllTexts(string objectName, string value)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName)
                text.text = value;
        }
    }

    private static void SetAllTextsActive(string objectName, bool active)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName && text.gameObject.activeSelf != active)
                text.gameObject.SetActive(active);
        }
    }
}
