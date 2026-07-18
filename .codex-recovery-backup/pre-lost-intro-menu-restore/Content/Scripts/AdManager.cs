using System.Collections;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    private const string AdsViewedKey = "AdsViewed";

    [SerializeField] private GameObject[] paintings;
    [SerializeField] private int adsPerPainting = 5;

    private void Start()
    {
        UpdatePaintings();
        Debug.Log("Сейчас просмотров рекламы: " + GetAdsViewed());
    }

    public void ShowRewardedAd()
    {
        StartCoroutine(FakeAd());
    }

    private IEnumerator FakeAd()
    {
        Debug.Log("▶ Типа показываем рекламу...");
        Debug.Log("👀 Игрок типа смотрит рекламу...");

        yield return new WaitForSeconds(1.5f);

        GiveAdReward();
    }

    private void GiveAdReward()
    {
        int count = GetAdsViewed() + 1;

        PlayerPrefs.SetInt(AdsViewedKey, count);
        PlayerPrefs.Save();

        Debug.Log("✅ Реклама досмотрена. Всего просмотров: " + count);

        UpdatePaintings();
    }

    private void UpdatePaintings()
    {
        int adsViewed = GetAdsViewed();

        int unlockedPaintings = adsViewed / adsPerPainting;

        for (int i = 0; i < paintings.Length; i++)
        {
            bool shouldShow = i < unlockedPaintings;
            paintings[i].SetActive(shouldShow);
        }

        Debug.Log("Открыто картин: " + unlockedPaintings);
    }

    private int GetAdsViewed()
    {
        return PlayerPrefs.GetInt(AdsViewedKey, 0);
    }

    public void ResetAds()
    {
        PlayerPrefs.DeleteKey(AdsViewedKey);
        PlayerPrefs.Save();

        Debug.Log("🔄 Счетчик рекламы сброшен.");

        UpdatePaintings();
    }
}