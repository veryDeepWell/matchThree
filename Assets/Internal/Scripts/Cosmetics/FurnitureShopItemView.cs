using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FurnitureShopItemView : MonoBehaviour
{
    [SerializeField] private Image _furnitureImage;
    [SerializeField] private TMP_Text _furnitureNameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _purchasedText;
    [SerializeField] private Button _buyButton;

    public void Configure(Image furnitureImage, TMP_Text furnitureNameText, TMP_Text priceText,
        TMP_Text purchasedText, Button buyButton)
    {
        _furnitureImage = furnitureImage;
        _furnitureNameText = furnitureNameText;
        _priceText = priceText;
        _purchasedText = purchasedText;
        _buyButton = buyButton;
    }

    public void Initialize(CosmeticFurnitureDefinition furniture, bool purchased, Action buyAction)
    {
        if (_furnitureImage != null)
            _furnitureImage.sprite = furniture.ShopIcon != null ? furniture.ShopIcon : furniture.LocationSprite;
        if (_furnitureNameText != null)
            _furnitureNameText.text = furniture.DisplayName;
        if (_priceText != null)
            _priceText.text = furniture.CrystalPrice.ToString();
        if (_purchasedText != null)
        {
            _purchasedText.text = "Куплено";
            _purchasedText.gameObject.SetActive(purchased);
        }

        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.interactable = !purchased;
            if (!purchased && buyAction != null)
                _buyButton.onClick.AddListener(() => buyAction());
        }
    }
}
