using System;
using UnityEngine;
using YG;

public class AdManager : MonoBehaviour
{
    private const string AdsViewedKey = "AdsViewed";
    private const string PaintingRewardId = "painting_progress";

    [SerializeField] private GameObject[] paintings;
    [SerializeField, Min(1)] private int adsPerPainting = 5;

    // Локальная защита от двойного нажатия и повторной выдачи одной награды.
    private bool isRewardedAdRunning;
    private bool rewardReceived;

    private void Start()
    {
        UpdatePaintings();
        Debug.Log($"Сейчас просмотров рекламы: {GetAdsViewed()}");
    }

    private void OnEnable()
    {
        // PluginYG сообщает об отмене и ошибке через глобальные события.
        YG2.onCloseRewardedAdv += HandleRewardedAdClosed;
        YG2.onErrorRewardedAdv += HandleRewardedAdError;
    }

    private void OnDisable()
    {
        YG2.onCloseRewardedAdv -= HandleRewardedAdClosed;
        YG2.onErrorRewardedAdv -= HandleRewardedAdError;
    }

    // Сохраняет прежнюю привязку кнопки в MainMenu.
    public void ShowRewardedAd()
    {
        ShowRewardedAd(PaintingRewardId, GiveAdReward);
    }

    // Используется игровыми системами для любых будущих наград.
    public bool ShowRewardedAd(string rewardId, Action onReward)
    {
        if (isRewardedAdRunning || YG2.nowAdsShow)
        {
            Debug.LogWarning("Реклама уже показывается.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rewardId))
        {
            Debug.LogError("AdManager: идентификатор рекламной награды не задан.");
            return false;
        }

        if (onReward == null)
        {
            Debug.LogError($"AdManager: для награды '{rewardId}' не задан callback.");
            return false;
        }

        // Сохраняемся перед переходом во внешнее рекламное окно.
        PlayerPrefs.Save();

        isRewardedAdRunning = true;
        rewardReceived = false;

        Debug.Log($"Запрос rewarded-рекламы: {rewardId}");
        // Callback будет вызван PluginYG только после подтверждения награды платформой.
        YG2.RewardedAdvShow(rewardId, () => CompleteRewardedAd(rewardId, onReward));
        return true;
    }

    public void ShowInterstitialAd()
    {
        if (YG2.nowAdsShow)
        {
            Debug.LogWarning("Реклама уже показывается.");
            return;
        }

        // У interstitial-рекламы нет игровой награды, поэтому callback не нужен.
        PlayerPrefs.Save();
        YG2.InterstitialAdvShow();
    }

    private void CompleteRewardedAd(string rewardId, Action onReward)
    {
        if (!isRewardedAdRunning || rewardReceived)
        {
            return;
        }

        // Сначала блокируем повторный callback, затем выдаём конкретную игровую награду.
        rewardReceived = true;
        onReward.Invoke();
        PlayerPrefs.Save();

        Debug.Log($"Rewarded-реклама успешно завершена: {rewardId}");
    }

    private void HandleRewardedAdClosed()
    {
        // Закрытие само по себе не является подтверждением просмотра.
        if (isRewardedAdRunning && !rewardReceived)
        {
            Debug.Log("Rewarded-реклама закрыта без награды.");
        }

        ResetRewardedAdState();
    }

    private void HandleRewardedAdError()
    {
        if (isRewardedAdRunning)
        {
            Debug.LogWarning("Не удалось показать rewarded-рекламу. Награда не выдана.");
        }

        ResetRewardedAdState();
    }

    private void ResetRewardedAdState()
    {
        isRewardedAdRunning = false;
        rewardReceived = false;
    }

    private void GiveAdReward()
    {
        int count = GetAdsViewed() + 1;

        PlayerPrefs.SetInt(AdsViewedKey, count);
        Debug.Log($"Просмотр рекламы засчитан. Всего просмотров: {count}");

        UpdatePaintings();
    }

    private void UpdatePaintings()
    {
        int adsViewed = GetAdsViewed();

        // Целочисленное деление открывает одну картину за каждые adsPerPainting просмотров.
        int unlockedPaintings = adsViewed / adsPerPainting;

        for (int i = 0; i < paintings.Length; i++)
        {
            if (paintings[i] == null)
            {
                Debug.LogWarning($"AdManager: картина в Element {i} не назначена.");
                continue;
            }

            bool shouldShow = i < unlockedPaintings;
            paintings[i].SetActive(shouldShow);
        }

        Debug.Log($"Открыто картин: {Mathf.Min(unlockedPaintings, paintings.Length)}");
    }

    private int GetAdsViewed()
    {
        return PlayerPrefs.GetInt(AdsViewedKey, 0);
    }

    public void ResetAds()
    {
        PlayerPrefs.DeleteKey(AdsViewedKey);
        PlayerPrefs.Save();

        Debug.Log("Счётчик рекламы сброшен.");

        UpdatePaintings();
    }
}
