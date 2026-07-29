using System;
using UnityEngine;
using YG;

public sealed class AdvertisingService : MonoBehaviour          //универсальный рекламный сервис, добавляет постоянный объект, который сохраняется между сценами с помощью DontDestroyOnLoad 
{
    public static AdvertisingService Instance { get; private set; }

    private string pendingRewardId;
    private Action pendingReward;
    private bool rewardReceived;
    private Action<bool> pendingInterstitialClosed;
    private bool interstitialRequested;

    // Создаём сервис после инициализации PluginYG, но до первого игрового кадра.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject serviceObject = new GameObject(nameof(AdvertisingService));
        serviceObject.AddComponent<AdvertisingService>();
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
    }

    private void OnEnable()
    {
        YG2.onCloseRewardedAdv += HandleRewardedClosed;
        YG2.onErrorRewardedAdv += HandleRewardedError;
        YG2.onCloseInterAdvWasShow += HandleInterstitialClosed;
        YG2.onErrorInterAdv += HandleInterstitialError;
    }

    private void OnDisable()
    {
        YG2.onCloseRewardedAdv -= HandleRewardedClosed;
        YG2.onErrorRewardedAdv -= HandleRewardedError;
        YG2.onCloseInterAdvWasShow -= HandleInterstitialClosed;
        YG2.onErrorInterAdv -= HandleInterstitialError;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool ShowRewarded(string rewardId, Action onReward)
    {
        if (pendingReward != null || interstitialRequested || YG2.nowAdsShow)
        {
            Debug.LogWarning("AdvertisingService: реклама уже показывается.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rewardId))
        {
            Debug.LogError("AdvertisingService: идентификатор рекламной награды не задан.");
            return false;
        }

        if (onReward == null)
        {
            Debug.LogError($"AdvertisingService: для награды '{rewardId}' не задан callback.");
            return false;
        }

        // Сохраняемся перед переходом во внешнее рекламное окно.
        PlayerPrefs.Save();

        pendingRewardId = rewardId;
        pendingReward = onReward;
        rewardReceived = false;

        Debug.Log($"Запрос rewarded-рекламы: {rewardId}");
        YG2.RewardedAdvShow(rewardId, CompleteRewarded);
        return true;
    }

    public bool ShowInterstitial(Action<bool> onClosed = null)
    {
        if (interstitialRequested || pendingReward != null || YG2.nowAdsShow)
        {
            Debug.LogWarning("AdvertisingService: реклама уже показывается.");
            return false;
        }

        if (!YG2.isTimerAdvCompleted)
        {
            Debug.LogWarning($"AdvertisingService: interstitial будет доступна через {YG2.timerInterAdv:0.0} сек.");
            return false;
        }

        PlayerPrefs.Save();
        interstitialRequested = true;
        pendingInterstitialClosed = onClosed;
        YG2.InterstitialAdvShow();
        return true;
    }

    private void CompleteRewarded()
    {
        if (pendingReward == null || rewardReceived)
        {
            return;
        }

        // PluginYG вызывает этот callback только после подтверждения награды платформой.
        rewardReceived = true;
        Action reward = pendingReward;

        reward.Invoke();
        PlayerPrefs.Save();

        Debug.Log($"Rewarded-реклама успешно завершена: {pendingRewardId}");
    }

    private void HandleRewardedClosed()
    {
        if (pendingReward != null && !rewardReceived)
        {
            Debug.Log("Rewarded-реклама закрыта без награды.");
        }

        ClearRewardedState();
    }

    private void HandleRewardedError()
    {
        if (pendingReward != null)
        {
            Debug.LogWarning("Не удалось показать rewarded-рекламу. Награда не выдана.");
        }

        ClearRewardedState();
    }

    private void ClearRewardedState()
    {
        pendingRewardId = null;
        pendingReward = null;
        rewardReceived = false;
    }

    private void HandleInterstitialClosed(bool wasShown)
    {
        Action<bool> callback = pendingInterstitialClosed;
        pendingInterstitialClosed = null;
        interstitialRequested = false;

        PlayerPrefs.Save();
        callback?.Invoke(wasShown);
    }

    private void HandleInterstitialError()
    {
        Action<bool> callback = pendingInterstitialClosed;
        pendingInterstitialClosed = null;
        interstitialRequested = false;

        Debug.LogWarning("Не удалось показать interstitial-рекламу.");
        callback?.Invoke(false);
    }
}
