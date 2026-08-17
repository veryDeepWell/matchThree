using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CosmeticsShopOpenPanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CosmeticsSceneController _controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.PlayButtonClick();
        _controller?.OpenShop();
    }
}
