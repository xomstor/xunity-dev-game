using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    public static ShopUIController Instance { get; private set; }

    [Header("References")]
    public ShopManager shopManager;
    public Inventory playerInventory;
    public PlayerStats playerStats;
    public List<ItemData> soulItems = new List<ItemData>();

    [Header("UI Text")]
    public string titleText = "Shop";
    public string goldPrefix = "Gold: ";
    public string buyButtonText = "Buy";
    public string sellButtonText = "Sell Souls";
    public string closeButtonText = "X";
    public string pricePrefix = "Price: ";
    public string priceSuffix = "g";
    public string purchasedMessage = "Purchased!";
    public string notEnoughMessage = "Not enough gold or inventory full";
    public string noItemSelectedMessage = "Select an item";

    [Header("Dynamic Pricing")]
    public float xpPotionPriceMultiplier = 1.1f;
    public string xpPotionIdSubstring = "xp";

    [Header("UI Colors")]
    public Color panelColor = new Color(0, 0.1f, 0.1f, 0.95f);
    public Color itemButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color buyButtonColor = new Color(0.2f, 0.6f, 0.2f, 1f);
    public Color sellButtonColor = new Color(0.6f, 0.4f, 0.2f, 1f);
    public Color closeButtonColor = new Color(0.6f, 0.2f, 0.2f, 1f);
    public Color tooltipColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

    [Header("UI Elements (assign manually or auto-create)")]
    public GameObject shopPanel;
    public Transform itemContainer;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIcon;
    public Button buyButton;
    public Button sellButton;
    public Button closeButton;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public GameObject itemButtonPrefab;

    private int selectedItemIndex = -1;
    private List<GameObject> itemButtons = new List<GameObject>();
    private Sprite whiteSprite;

    void Awake()
    {
        if (Instance == null || CountValidReferences(Instance) < CountValidReferences(this))
            Instance = this;

        FindExistingUI();
        CreateShopUI();
    }

    void FindExistingUI()
    {
        if (shopPanel == null)
        {
            shopPanel = FindChildByName(transform, "ShopPanel");
            if (shopPanel == null)
            {
                Transform canvas = FindChildByName(transform, "ShopCanvas")?.transform;
                if (canvas != null)
                    shopPanel = FindChildByName(canvas, "ShopPanel");
            }
        }
        if (shopPanel == null) return;

        itemContainer ??= FindChildByName(shopPanel.transform, "ItemContainer")?.transform;
        goldText ??= FindChildByName(shopPanel.transform, "GoldText")?.GetComponent<TextMeshProUGUI>();
        itemNameText ??= FindChildByName(shopPanel.transform, "ItemNameText")?.GetComponent<TextMeshProUGUI>();
        itemDescriptionText ??= FindChildByName(shopPanel.transform, "ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();
        itemPriceText ??= FindChildByName(shopPanel.transform, "ItemPriceText")?.GetComponent<TextMeshProUGUI>();
        itemIcon ??= FindChildByName(shopPanel.transform, "ItemIcon")?.GetComponent<Image>();
        buyButton ??= FindChildByName(shopPanel.transform, "BuyButton")?.GetComponent<Button>();
        sellButton ??= FindChildByName(shopPanel.transform, "SellButton")?.GetComponent<Button>();
        closeButton ??= FindChildByName(shopPanel.transform, "CloseButton")?.GetComponent<Button>();
        if (tooltipPanel == null)
        {
            tooltipPanel = FindChildByName(shopPanel.transform, "TooltipPanel");
            if (tooltipPanel == null && shopPanel.transform.parent != null)
                tooltipPanel = FindChildByName(shopPanel.transform.parent, "TooltipPanel");
        }
        tooltipText ??= tooltipPanel?.GetComponentInChildren<TextMeshProUGUI>();
    }

    GameObject FindChildByName(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform result = parent.Find(name);
        if (result != null) return result.gameObject;
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child.gameObject;
        }
        return null;
    }

    int CountValidReferences(ShopUIController c)
    {
        int count = 0;
        if (c.shopPanel != null) count++;
        if (c.itemContainer != null) count++;
        if (c.goldText != null) count++;
        if (c.itemNameText != null) count++;
        if (c.itemDescriptionText != null) count++;
        if (c.itemPriceText != null) count++;
        if (c.itemIcon != null) count++;
        if (c.buyButton != null) count++;
        if (c.sellButton != null) count++;
        if (c.closeButton != null) count++;
        if (c.tooltipPanel != null) count++;
        if (c.tooltipText != null) count++;
        return count;
    }

    void Update()
    {
        if (shopPanel != null && shopPanel.activeInHierarchy)
        {
            UpdateGoldText();
            UpdateTooltipPosition();
        }
    }

    public void OpenShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(true);
        selectedItemIndex = -1;
        ClearItemDetails();
        RefreshShopItems();
        UpdateGoldText();
        UpdateSellText();
    }

    public void CloseShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(false);
        HideTooltip();
    }

    void CreateShopUI()
    {
        if (shopPanel != null)
        {
            WireUpEvents();
            return;
        }

        Canvas mainCanvas = GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("ShopUIController: No Canvas found!");
                return;
            }
        }

        GameObject shopCanvasGO = new GameObject("ShopCanvas");
        shopCanvasGO.transform.SetParent(transform, false);
        Canvas shopCanvas = shopCanvasGO.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 100;
        shopCanvas.overrideSorting = true;
        shopCanvas.sortingLayerID = SortingLayer.NameToID("Foreground");
        shopCanvasGO.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = shopCanvasGO.AddComponent<CanvasScaler>();
        CanvasScaler mainScaler = mainCanvas.GetComponent<CanvasScaler>();
        if (mainScaler != null)
        {
            scaler.uiScaleMode = mainScaler.uiScaleMode;
            scaler.referenceResolution = mainScaler.referenceResolution;
            scaler.screenMatchMode = mainScaler.screenMatchMode;
            scaler.matchWidthOrHeight = mainScaler.matchWidthOrHeight;
            scaler.scaleFactor = mainScaler.scaleFactor;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        shopPanel = CreatePanel("ShopPanel", shopCanvasGO.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, panelColor);

        CreateText("TitleText", shopPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -30), new Vector2(300, 50), titleText, 36, TextAnchor.MiddleLeft, Color.white);

        closeButton = CreateButton("CloseButton", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -40), new Vector2(80, 40), closeButtonText, closeButtonColor, 24);
        closeButton.onClick.AddListener(CloseShop);

        goldText = CreateText("GoldText", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-280, -30), new Vector2(180, 50), goldPrefix + "0", 28, TextAnchor.MiddleRight, Color.yellow);

        GameObject leftPanel = CreatePanel("ItemListPanel", shopPanel.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(320, 480), new Color(0, 0, 0, 0.3f));

        GameObject itemList = CreatePanel("ItemContainer", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(300, 460), new Color(0, 0, 0, 0));
        VerticalLayoutGroup vlg = itemList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        itemList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemContainer = itemList.transform;

        GameObject rightPanel = CreatePanel("ItemDetailPanel", shopPanel.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-300, 20), new Vector2(360, 440), new Color(0, 0, 0, 0.3f));

        itemIcon = CreateImage("ItemIcon", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(100, 100));
        itemNameText = CreateText("ItemNameText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(340, 40), "", 30, TextAnchor.MiddleCenter, Color.white);
        itemDescriptionText = CreateText("ItemDescriptionText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(340, 120), noItemSelectedMessage, 22, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f, 1f));
        itemDescriptionText.overflowMode = TextOverflowModes.Overflow;
        itemDescriptionText.textWrappingMode = TextWrappingModes.Normal;

        itemPriceText = CreateText("ItemPriceText", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(340, 40), "", 26, TextAnchor.MiddleCenter, Color.yellow);

        buyButton = CreateButton("BuyButton", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(200, 50), buyButtonText, buyButtonColor, 28);
        buyButton.onClick.AddListener(BuySelectedItem);

        sellButton = CreateButton("SellButton", shopPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(250, 50), sellButtonText, sellButtonColor, 24);
        sellButton.onClick.AddListener(SellAllSouls);

        tooltipPanel = CreatePanel("TooltipPanel", shopCanvasGO.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(250, 80), tooltipColor);
        tooltipPanel.SetActive(false);
        tooltipText = CreateText("TooltipText", tooltipPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), "", 20, TextAnchor.MiddleCenter, Color.white);
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tooltipTextRt = tooltipText.GetComponent<RectTransform>();
        tooltipTextRt.offsetMin = new Vector2(10, 10);
        tooltipTextRt.offsetMax = new Vector2(-10, -10);
        ContentSizeFitter tooltipFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        tooltipFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        WireUpEvents();
        shopPanel.SetActive(false);
    }

    void WireUpEvents()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
        if (buyButton != null)
            buyButton.onClick.AddListener(BuySelectedItem);
        if (sellButton != null)
            sellButton.onClick.AddListener(SellAllSouls);
    }

    GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = color;
        return go;
    }

    Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
        }
        return whiteSprite;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string text, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = GetTextAlignmentOptions(alignment);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        return img;
    }

    Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string text, Color color, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return btn;
    }

    TextAlignmentOptions GetTextAlignmentOptions(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    void RefreshShopItems()
    {
        if (itemContainer == null || shopManager == null) return;

        foreach (GameObject btn in itemButtons)
            if (btn != null) Destroy(btn);
        itemButtons.Clear();

        for (int i = 0; i < shopManager.shopItems.Length; i++)
        {
            ShopItem shopItem = shopManager.shopItems[i];
            if (shopItem.itemData == null) continue;
            if (shopItem.quantity == 0) continue;

            Button button = CreateItemButton(shopItem, i);
            itemButtons.Add(button.gameObject);
        }
    }

    Button CreateItemButton(ShopItem shopItem, int index)
    {
        GameObject go = new GameObject($"Item_{shopItem.itemData.itemName}");
        go.transform.SetParent(itemContainer, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 55);
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = itemButtonColor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(32, 0);
        iconRt.sizeDelta = new Vector2(40, 40);
        iconGO.AddComponent<CanvasRenderer>();
        Image icon = iconGO.AddComponent<Image>();
        icon.sprite = shopItem.itemData.icon;
        icon.color = icon.sprite == null ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;

        GameObject textGO = new GameObject("Name");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(65, 0);
        textRt.offsetMax = new Vector2(-8, 0);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{shopItem.itemData.itemName}\n{shopItem.price}g";
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        int capturedIndex = index;
        btn.onClick.AddListener(() => SelectItem(capturedIndex));

        AddTooltip(go, shopItem.itemData.description);

        return btn;
    }

    void AddTooltip(GameObject target, string description)
    {
        EventTrigger trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((eventData) => ShowTooltip(description));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((eventData) => HideTooltip());
        trigger.triggers.Add(exitEntry);
    }

    void ShowTooltip(string text)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());
        UpdateTooltipPosition();
    }

    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void UpdateTooltipPosition()
    {
        if (tooltipPanel == null || !tooltipPanel.activeInHierarchy) return;
        Vector2 mousePos = Input.mousePosition;
        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        rt.position = new Vector3(mousePos.x + 15, mousePos.y + 15, 0);
    }

    void SelectItem(int index)
    {
        selectedItemIndex = index;
        ShopItem shopItem = shopManager.shopItems[index];
        if (itemIcon != null)
            itemIcon.sprite = shopItem.itemData.icon;
        if (itemNameText != null)
            itemNameText.text = shopItem.itemData.itemName;
        if (itemDescriptionText != null)
            itemDescriptionText.text = shopItem.itemData.description;
        if (itemPriceText != null)
            itemPriceText.text = $"{pricePrefix}{shopItem.price}{priceSuffix}";
    }

    void BuySelectedItem()
    {
        if (selectedItemIndex < 0 || selectedItemIndex >= shopManager.shopItems.Length)
        {
            if (itemDescriptionText != null)
                itemDescriptionText.text = noItemSelectedMessage;
            return;
        }

        ShopItem shopItem = shopManager.shopItems[selectedItemIndex];
        if (shopItem.itemData == null) return;

        bool success = shopManager.BuyItem(selectedItemIndex, playerInventory, playerStats);
        if (success && shopItem.itemData != null && shopItem.itemData.itemId.Contains(xpPotionIdSubstring))
        {
            shopItem.price = Mathf.RoundToInt(shopItem.price * xpPotionPriceMultiplier);
        }
        UpdateGoldText();
        RefreshShopItems();
        if (itemDescriptionText != null)
            itemDescriptionText.text = success ? purchasedMessage : notEnoughMessage;
    }

    void SellAllSouls()
    {
        if (shopManager == null || playerInventory == null || playerStats == null) return;

        int totalGold = 0;
        foreach (ItemData soul in soulItems)
        {
            if (soul == null) continue;
            totalGold += shopManager.SellItem(soul, int.MaxValue, playerInventory, playerStats);
        }

        if (totalGold > 0)
        {
            UpdateGoldText();
            UpdateSellText();
        }
    }

    void UpdateSellText()
    {
        if (sellButton == null) return;
        TextMeshProUGUI text = sellButton.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            int totalCount = 0;
            int totalValue = 0;
            foreach (ItemData soul in soulItems)
            {
                if (soul == null) continue;
                int count = playerInventory.GetItemCount(soul);
                totalCount += count;
                totalValue += count * (soul.price > 0 ? soul.price : 1);
            }
            text.text = $"{sellButtonText} ({totalCount})\n+{totalValue}g";
        }
    }

    void UpdateGoldText()
    {
        if (goldText != null && playerStats != null)
            goldText.text = goldPrefix + playerStats.gold;
    }

    void ClearItemDetails()
    {
        selectedItemIndex = -1;
        if (itemIcon != null)
            itemIcon.sprite = null;
        if (itemNameText != null)
            itemNameText.text = "";
        if (itemDescriptionText != null)
            itemDescriptionText.text = noItemSelectedMessage;
        if (itemPriceText != null)
            itemPriceText.text = "";
    }
}
