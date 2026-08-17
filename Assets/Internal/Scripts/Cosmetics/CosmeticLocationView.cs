using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CosmeticLocationView : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private RectTransform _furnitureContainer;

    private readonly List<GameObject> _spawnedFurniture = new List<GameObject>();

    public void Configure(Image backgroundImage, RectTransform furnitureContainer)
    {
        _backgroundImage = backgroundImage;
        _furnitureContainer = furnitureContainer;
    }

    public void Render(CosmeticLocationDefinition location, SaveService saveService)
    {
        ClearFurniture();
        if (location == null)
            return;

        if (_backgroundImage != null)
            _backgroundImage.sprite = location.Background;

        if (_furnitureContainer == null || location.Furniture == null)
            return;

        foreach (CosmeticFurnitureDefinition furniture in location.Furniture)
        {
            if (furniture == null || furniture.LocationSprite == null || saveService == null ||
                !saveService.IsFurniturePurchased(location.LocationId, furniture.FurnitureId))
                continue;

            var item = new GameObject(furniture.DisplayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(_furnitureContainer, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = furniture.AnchoredPosition;
            rect.sizeDelta = furniture.Size;
            rect.localRotation = Quaternion.Euler(0f, 0f, furniture.Rotation);
            rect.SetSiblingIndex(Mathf.Clamp(furniture.SortingOrder, 0, _furnitureContainer.childCount - 1));
            item.GetComponent<Image>().sprite = furniture.LocationSprite;
            _spawnedFurniture.Add(item);
        }
    }

    private void ClearFurniture()
    {
        foreach (GameObject item in _spawnedFurniture)
        {
            if (item != null)
                Destroy(item);
        }
        _spawnedFurniture.Clear();

        // Старые демонстрационные картинки из префаба не являются данными локации.
        if (_furnitureContainer == null)
            return;
        for (int index = 0; index < _furnitureContainer.childCount; index++)
            _furnitureContainer.GetChild(index).gameObject.SetActive(false);
    }
}
