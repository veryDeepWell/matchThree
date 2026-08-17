using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class CosmeticsSceneController : MonoBehaviour
{
    private const string CosmeticsSceneName = "CosmeticsScene";

    [Header("Данные")]
    [SerializeField] private CosmeticCatalog _catalog;
    [SerializeField] private CosmeticLocationView _locationView;

    [Header("Магазин")]
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private RectTransform _shopContent;
    [SerializeField] private FurnitureShopItemView _shopItemTemplate;
    [SerializeField] private Button _closeShopButton;

    [Header("Навигация")]
    [SerializeField] private Button _previousLocationButton;
    [SerializeField] private Button _nextLocationButton;
    [SerializeField] private TMP_Text _locationNameText;
    [SerializeField] private TMP_Text _crystalsText;

    private int _locationIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != CosmeticsSceneName)
            return;

        Canvas canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CosmeticsSceneController existingController =
                root.GetComponentInChildren<CosmeticsSceneController>(true);
            if (existingController != null)
                return;

            Canvas sceneCanvas = root.GetComponentInChildren<Canvas>(true);
            if (sceneCanvas != null)
            {
                canvas = sceneCanvas;
                break;
            }
        }

        if (canvas == null)
        {
            Debug.LogError("[Cosmetics] На CosmeticsScene не найден Canvas.");
            return;
        }

        canvas.gameObject.AddComponent<CosmeticsSceneController>();
    }

    private void Start()
    {
        AutoWireOnce();
        if (_closeShopButton != null)
            _closeShopButton.onClick.AddListener(CloseShop);
        if (_previousLocationButton != null)
            _previousLocationButton.onClick.AddListener(ShowPreviousLocation);
        if (_nextLocationButton != null)
            _nextLocationButton.onClick.AddListener(ShowNextLocation);

        if (_shopItemTemplate != null)
            _shopItemTemplate.gameObject.SetActive(false);
        if (_shopPanel != null)
            _shopPanel.SetActive(false);

        SelectSavedLocation();
        Refresh();
    }

    // Эта привязка выполняется один раз при входе на сцену. Здесь нет поиска в Update,
    // поэтому она не создаёт микрофризов, о которых обычно говорят применительно к GetComponent.
    private void AutoWireOnce()
    {
        if (_catalog == null)
            _catalog = Resources.Load<CosmeticCatalog>("CosmeticCatalog");

        Transform viewRoot = FindDescendant(transform, "CosmeticDefoltView");
        if (_locationView == null && viewRoot != null)
        {
            _locationView = viewRoot.GetComponent<CosmeticLocationView>();
            if (_locationView == null)
                _locationView = viewRoot.gameObject.AddComponent<CosmeticLocationView>();
            Transform background = FindDescendant(viewRoot, "BackgroundImage");
            Transform items = FindDescendant(viewRoot, "CosmeticItemsPanel");
            _locationView.Configure(background?.GetComponent<Image>(), items as RectTransform);
        }

        Transform shopPanel = FindDescendant(transform, "FurnitureShopPanel");
        _shopPanel ??= shopPanel != null ? shopPanel.gameObject : null;
        _shopContent ??= FindDescendant(transform, "Content") as RectTransform;
        _closeShopButton ??= FindDescendant(transform, "CloseShopButton")?.GetComponent<Button>();
        _previousLocationButton ??= FindDescendant(transform, "PreviousLocationButton")?.GetComponent<Button>();
        _nextLocationButton ??= FindDescendant(transform, "NextLocationButton")?.GetComponent<Button>();
        _locationNameText ??= FindDescendant(transform, "LocationNameText (TMP)")?.GetComponent<TMP_Text>();
        _crystalsText ??= FindDescendant(transform, "MoneyNumberText (TMP)")?.GetComponent<TMP_Text>();

        // NavigationPanel растянута на весь Canvas. Прозрачная Image с включённым
        // Raycast Target иначе перекрывает магазин и все его кнопки.
        Transform navigationPanel = FindDescendant(transform, "NavigationPanel");
        Image navigationBackground = navigationPanel?.GetComponent<Image>();
        if (navigationBackground != null)
            navigationBackground.raycastTarget = false;

        if (_shopItemTemplate == null && _shopContent != null && _shopContent.childCount > 0)
        {
            Transform template = _shopContent.GetChild(0);
            _shopItemTemplate = template.GetComponent<FurnitureShopItemView>();
            if (_shopItemTemplate == null)
                _shopItemTemplate = template.gameObject.AddComponent<FurnitureShopItemView>();
            _shopItemTemplate.Configure(
                FindDescendant(template, "FurnitureImage")?.GetComponent<Image>(),
                FindDescendant(template, "FurnitureNameText (TMP)")?.GetComponent<TMP_Text>(),
                FindDescendant(template, "PriceText (TMP)")?.GetComponent<TMP_Text>(),
                FindDescendant(template, "PurchasedText (TMP)")?.GetComponent<TMP_Text>(),
                FindDescendant(template, "BuyButton")?.GetComponent<Button>());
        }

        if (_shopContent != null)
        {
            Vector2 contentPosition = _shopContent.anchoredPosition;
            contentPosition.x = 0f;
            _shopContent.anchoredPosition = contentPosition;

            VerticalLayoutGroup layout = _shopContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                layout.reverseArrangement = false;

            ScrollRect scrollRect = _shopContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
        }

        BindPanelButton("ShopPanel", OpenShop);
        BindPanelButton("BackToMainMenuPanel", () => SceneManager.LoadScene("MainMenu"));
    }

    private void BindPanelButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        Transform target = FindDescendant(transform, objectName);
        if (target == null)
            return;
        Button button = target.GetComponent<Button>();
        if (button == null)
            button = target.gameObject.AddComponent<Button>();
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindDescendant(root.GetChild(index), objectName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (_closeShopButton != null)
            _closeShopButton.onClick.RemoveListener(CloseShop);
        if (_previousLocationButton != null)
            _previousLocationButton.onClick.RemoveListener(ShowPreviousLocation);
        if (_nextLocationButton != null)
            _nextLocationButton.onClick.RemoveListener(ShowNextLocation);
    }

    public void OpenShop()
    {
        if (_shopPanel == null)
            return;
        _shopPanel.SetActive(true);
        RebuildShop();
    }

    public void CloseShop()
    {
        SoundManager.PlayButtonClick();
        if (_shopPanel != null)
            _shopPanel.SetActive(false);
    }

    private void ShowPreviousLocation()
    {
        if (_locationIndex <= 0)
            return;
        _locationIndex--;
        ChangeLocation();
    }

    private void ShowNextLocation()
    {
        if (_catalog == null || _locationIndex >= _catalog.Locations.Count - 1 || !IsCurrentLocationComplete())
            return;
        _locationIndex++;
        ChangeLocation();
    }

    private void ChangeLocation()
    {
        SoundManager.PlayButtonClick();
        CosmeticLocationDefinition location = CurrentLocation;
        SaveService.Instance?.SetCurrentCosmeticLocation(location.LocationId);
        Refresh();
        if (_shopPanel != null && _shopPanel.activeSelf)
            RebuildShop();
    }

    private void SelectSavedLocation()
    {
        string savedId = string.Empty;
        SaveService saveService = SaveService.Instance;
        if (saveService != null && saveService.Data != null && saveService.Data.MacroProgress != null)
            savedId = saveService.Data.MacroProgress.CurrentCosmeticLocationId;
        if (string.IsNullOrWhiteSpace(savedId) || _catalog == null || _catalog.Locations == null)
            return;

        int found = _catalog.Locations.FindIndex(location => location != null && location.LocationId == savedId);
        if (found >= 0 && IsLocationUnlocked(found))
            _locationIndex = found;
    }

    private void Refresh()
    {
        CosmeticLocationDefinition location = CurrentLocation;
        if (location == null)
        {
            Debug.LogWarning("[Cosmetics] В каталоге нет локаций.", this);
            return;
        }

        SaveService saveService = SaveService.Instance;
        _locationView?.Render(location, saveService);
        if (_locationNameText != null)
            _locationNameText.text = location.DisplayName;
        if (_crystalsText != null)
        {
            int crystals = 0;
            if (saveService != null && saveService.Data != null && saveService.Data.Economy != null)
                crystals = saveService.Data.Economy.Crystals;
            _crystalsText.text = crystals.ToString();
        }

        bool currentComplete = IsCurrentLocationComplete();
        if (_previousLocationButton != null)
            _previousLocationButton.gameObject.SetActive(_locationIndex > 0);
        if (_nextLocationButton != null)
            _nextLocationButton.gameObject.SetActive(
                _catalog != null && _locationIndex < _catalog.Locations.Count - 1 && currentComplete);
    }

    private void RebuildShop()
    {
        if (_shopContent == null || _shopItemTemplate == null)
            return;

        for (int index = _shopContent.childCount - 1; index >= 0; index--)
        {
            Transform child = _shopContent.GetChild(index);
            if (child != _shopItemTemplate.transform)
                Destroy(child.gameObject);
        }

        CosmeticLocationDefinition location = CurrentLocation;
        if (location?.Furniture == null)
            return;

        SaveService saveService = SaveService.Instance;
        foreach (CosmeticFurnitureDefinition furniture in location.Furniture)
        {
            if (furniture == null)
                continue;
            FurnitureShopItemView item = Instantiate(_shopItemTemplate, _shopContent);
            item.gameObject.SetActive(true);
            bool purchased = saveService != null && saveService.IsFurniturePurchased(location.LocationId, furniture.FurnitureId);
            item.Initialize(furniture, purchased, () => BuyFurniture(location, furniture));
        }
    }

    private void BuyFurniture(CosmeticLocationDefinition location, CosmeticFurnitureDefinition furniture)
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || !saveService.TryPurchaseFurniture(
                location.LocationId, furniture.FurnitureId, furniture.CrystalPrice))
        {
            Debug.LogWarning($"[Cosmetics] Для покупки '{furniture.DisplayName}' не хватает кристаллов.", this);
            return;
        }

        TryCompleteLocation(location);
        Refresh();
        RebuildShop();
    }

    private void TryCompleteLocation(CosmeticLocationDefinition location)
    {
        if (!AreAllFurnitureItemsPurchased(location))
            return;

        SaveService.Instance?.TryCompleteCosmeticLocation(location.LocationId, location.CompletionReward);
    }

    private bool IsCurrentLocationComplete()
    {
        CosmeticLocationDefinition location = CurrentLocation;
        return location != null && AreAllFurnitureItemsPurchased(location);
    }

    private static bool AreAllFurnitureItemsPurchased(CosmeticLocationDefinition location)
    {
        SaveService saveService = SaveService.Instance;
        if (saveService == null || location?.Furniture == null || location.Furniture.Count == 0)
            return false;

        foreach (CosmeticFurnitureDefinition furniture in location.Furniture)
        {
            if (furniture != null && !saveService.IsFurniturePurchased(location.LocationId, furniture.FurnitureId))
                return false;
        }
        return true;
    }

    private bool IsLocationUnlocked(int index)
    {
        if (index <= 0)
            return true;
        for (int previous = 0; previous < index; previous++)
        {
            CosmeticLocationDefinition location = _catalog.Locations[previous];
            if (location == null || !AreAllFurnitureItemsPurchased(location))
                return false;
        }
        return true;
    }

    private CosmeticLocationDefinition CurrentLocation
    {
        get
        {
            if (_catalog == null || _catalog.Locations == null || _catalog.Locations.Count == 0)
                return null;
            _locationIndex = Mathf.Clamp(_locationIndex, 0, _catalog.Locations.Count - 1);
            return _catalog.Locations[_locationIndex];
        }
    }
}
