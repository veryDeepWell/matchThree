using UnityEngine;

public sealed class PaintingAdProgress : MonoBehaviour
{
    private const string AdsViewedKey = "AdsViewed";
    private const string PaintingRewardId = "painting_progress";

    [SerializeField] private GameObject[] paintings;
    [SerializeField, Min(1)] private int adsPerPainting = 5;

    private void Start()
    {
        UpdatePaintings();
        Debug.Log($"Сейчас просмотров рекламы: {GetAdsViewed()}");
    }

    // Вызывается кнопкой в MainMenu.
    public void WatchAdForPainting()
    {
        AdvertisingService advertising = AdvertisingService.Instance;

        if (advertising == null)
        {
            Debug.LogError("PaintingAdProgress: AdvertisingService не создан.");
            return;
        }

        advertising.ShowRewarded(PaintingRewardId, RegisterAdView);
    }

    private void RegisterAdView()
    {
        int count = GetAdsViewed() + 1;
        PlayerPrefs.SetInt(AdsViewedKey, count);
        PlayerPrefs.Save();

        Debug.Log($"Просмотр рекламы засчитан. Всего просмотров: {count}");
        UpdatePaintings();
    }

    private void UpdatePaintings()
    {
        int unlockedPaintings = GetAdsViewed() / adsPerPainting;

        // Целочисленное деление открывает одну картину за каждые adsPerPainting просмотров.
        for (int i = 0; i < paintings.Length; i++)
        {
            if (paintings[i] == null)
            {
                Debug.LogWarning($"PaintingAdProgress: картина в Element {i} не назначена.");
                continue;
            }

            paintings[i].SetActive(i < unlockedPaintings);
        }

        Debug.Log($"Открыто картин: {Mathf.Min(unlockedPaintings, paintings.Length)}");
    }

    private int GetAdsViewed()
    {
        return PlayerPrefs.GetInt(AdsViewedKey, 0);
    }

    // Временный метод для тестирования. Не следует выводить эту кнопку в релизный интерфейс.
    public void ResetAds()
    {
        PlayerPrefs.DeleteKey(AdsViewedKey);
        PlayerPrefs.Save();

        Debug.Log("Счётчик рекламы сброшен.");
        UpdatePaintings();
    }
}
