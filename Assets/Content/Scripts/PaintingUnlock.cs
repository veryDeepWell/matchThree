//using UnityEngine;

//public class PaintingUnlock : MonoBehaviour
//{
//    [SerializeField] private GameObject paintingAfter5Add;
//    [SerializeField] private int requiredAds = 5;

//    private void Start()
//    {
//        UpdatePaintingState();
//    }

//    private void OnEnable()
//    {
//        AdManager.OnAdViewed += HandleAdViewed;
//    }

//    private void OnDisable()
//    {
//        AdManager.OnAdViewed -= HandleAdViewed;
//    }

//    private void HandleAdViewed(int adsViewed)
//    {
//        if (adsViewed >= requiredAds)
//        {
//            paintingAfter5Add.SetActive(true);
//        }
//    }

//    private void UpdatePaintingState()
//    {
//        bool shouldShow = AdManager.Instance.AdsViewed >= requiredAds;
//        paintingAfter5Add.SetActive(shouldShow);
//    }
//}