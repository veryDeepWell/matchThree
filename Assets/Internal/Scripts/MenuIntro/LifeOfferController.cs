using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LifeOfferController : MonoBehaviour
{
    private const int LifeGoldPrice = 2000;
    private const string LifeRewardId = "extra_life";
    private const string BattleSceneName = "BattleScene";

    public static LifeOfferController Instance { get; private set; }

    private GameObject _panel;
    private Action _pendingAction;
    private float _previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject controllerObject = new GameObject(nameof(LifeOfferController));
        controllerObject.AddComponent<LifeOfferController>();
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
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ConfigureScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public static bool TryShow(Action actionAfterLifeGranted)
    {
        if (Instance == null || Instance._panel == null)
            return false;

        Instance.Show(actionAfterLifeGranted);
        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _pendingAction = null;
        ConfigureScene(scene);
    }

    private void ConfigureScene(Scene scene)
    {
        _panel = FindObject(scene, "LifeOfferPanel");
        if (_panel != null)
        {
            ConfigureButton(_panel, "GoldBonusButton", BuyLifeForGold);
            ConfigureButton(_panel, "AdvertisingBonusButton", WatchAdForLife);
            ConfigureButton(_panel, "CloseThisPanelButton", Close);
            _panel.SetActive(false);
        }

        if (scene.name == "MainMenu")
        {
            Button startButton = FindComponent<Button>(scene, "StartButton");
            if (startButton != null)
            {
                // Перехватываем старый прямой переход в BattleScene, чтобы сначала
                // проверить жизни и при необходимости показать предложение.
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => SoundManager.PlayButtonClick());
                startButton.onClick.AddListener(TryStartBattle);
            }
        }
    }

    private void TryStartBattle()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || saveService.Data == null || saveService.Data.Economy == null)
        {
            SceneManager.LoadScene(BattleSceneName);
            return;
        }

        saveService.RefreshLives();
        if (saveService.Data.Economy.Lives > 0)
        {
            SceneManager.LoadScene(BattleSceneName);
            return;
        }

        Show(() => SceneManager.LoadScene(BattleSceneName));
    }

    private void Show(Action actionAfterLifeGranted)
    {
        _pendingAction = actionAfterLifeGranted;
        _previousTimeScale = Time.timeScale;
        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void BuyLifeForGold()
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || !saveService.TryBuyLife(LifeGoldPrice))
        {
            Debug.LogWarning($"[LifeOffer] Для покупки жизни нужно {LifeGoldPrice} золота.");
            return;
        }

        CompleteOffer();
    }

    private void WatchAdForLife()
    {
        if (AdvertisingService.Instance == null)
            return;

        bool started = AdvertisingService.Instance.ShowRewarded(
            LifeRewardId,
            () => SaveService.Instance?.RestoreAllLives(),
            HandleAdvertisementClosed);
        if (!started)
            Debug.LogWarning("[LifeOffer] Не удалось запустить рекламу за жизнь.");
    }

    private void HandleAdvertisementClosed(bool rewardGranted)
    {
        if (rewardGranted)
            CompleteOffer();
    }

    private void CompleteOffer()
    {
        Action pendingAction = _pendingAction;
        Close();
        pendingAction?.Invoke();
    }

    private void Close()
    {
        _pendingAction = null;
        if (_panel != null)
            _panel.SetActive(false);
        Time.timeScale = _previousTimeScale;
    }

    private static void ConfigureButton(GameObject root, string objectName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindComponent<Button>(root, objectName);
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SoundManager.PlayButtonClick());
        button.onClick.AddListener(action);
    }

    private static T FindComponent<T>(Scene scene, string objectName) where T : Component
    {
        GameObject found = FindObject(scene, objectName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static T FindComponent<T>(GameObject root, string objectName) where T : Component
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.GetComponent<T>();
        }

        return null;
    }

    private static GameObject FindObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }
}
