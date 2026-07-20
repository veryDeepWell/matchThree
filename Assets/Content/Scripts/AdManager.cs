using System.Collections;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    private const string AdsViewedKey = "AdsViewed";

    [SerializeField] private GameObject[] paintings;        //массив объектов с картинкой 
    [SerializeField] private int adsPerPainting = 5;        //скок надо реклам для картинки 

    private void Start()
    {
        UpdatePaintings();
        Debug.Log("Сейчас просмотров рекламы: " + GetAdsViewed());
    }

    public void ShowRewardedAd()        //метод для onClick
    {
        StartCoroutine(FakeAd());//вместо FakeAd вставить настоящую рекламу 
    }

    private IEnumerator FakeAd()        //заглушка, отображаем рекламу
    {
        Debug.Log("▶ Типа показываем рекламу...");
        Debug.Log("👀 Игрок типа смотрит рекламу...");

        yield return new WaitForSeconds(1.5f);

        GiveAdReward();
    }

    private void GiveAdReward()         //считаем количество просмотров
    {
        int count = GetAdsViewed() + 1;

        PlayerPrefs.SetInt(AdsViewedKey, count);
        PlayerPrefs.Save();

        Debug.Log("✅ Реклама досмотрена. Всего просмотров: " + count);

        UpdatePaintings();
    }

    private void UpdatePaintings()      //сверяем количество просмотров с требованием к разблокировки картины
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

    private int GetAdsViewed()          //читаем количество просмотров
    {
        return PlayerPrefs.GetInt(AdsViewedKey, 0);
    }

    public void ResetAds()              //сброс количества просмотров
    {
        PlayerPrefs.DeleteKey(AdsViewedKey);
        PlayerPrefs.Save();

        Debug.Log("🔄 Счетчик рекламы сброшен.");

        UpdatePaintings();
    }
}