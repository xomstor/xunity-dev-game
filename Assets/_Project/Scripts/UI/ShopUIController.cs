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

    private GameObject shopPanel;
    private Transform itemContainer;
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI itemDescriptionText;
    private TextMeshProUGUI itemPriceText;
    private Image itemIcon;
    private Button buyButton;
    private Button sellButton;
    private Button closeButton;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;

    private int selectedItemIndex = -1;
    private List<GameObject> itemButtons = new List<GameObject>();
    private Sprite whiteSprite;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateShopUI();
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
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ShopUIController: No Canvas found!");
                return;
            }
        }

        shopPanel = CreatePanel("ShopPanel", canvas.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-40, -40), panelColor);

        CreateText("TitleText", shopPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(300, 40), titleText, 30, TextAnchor.MiddleLeft, Color.white);

        closeButton = CreateButton("CloseButton", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-45, -25), new Vector2(70, 40), closeButtonText, closeButtonColor, 22);
        closeButton.onClick.AddListener(CloseShop);

        goldText = CreateText("GoldText", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-210, -25), new Vector2(150, 40), goldPrefix + "0", 24, TextAnchor.MiddleRight, Color.yellow);

        GameObject leftPanel = CreatePanel("ItemListPanel", shopPanel.transform, new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(0, 0), new Vector2(-20, -20), new Color(0, 0, 0, 0.3f));

        GameObject itemList = CreatePanel("ItemContainer", leftPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-20, -20), new Color(0, 0, 0, 0));
        VerticalLayoutGroup vlg = itemList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        itemList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemContainer = itemList.transform;

        GameObject rightPanel = CreatePanel("ItemDetailPanel", shopPanel.transform, new Vector2(0.45f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-20, -20), new Color(0, 0, 0, 0.3f));

        itemIcon = CreateImage("ItemIcon", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(80, 80));
        itemNameText = CreateText("ItemNameText", rightPanel.transform, new Vector2(0, 0.85f), new Vector2(1, 0.92f), new Vector2(0, 0), new Vector2(-20, -10), "", 26, TextAnchor.MiddleCenter, Color.white);
        itemDescriptionText = CreateText("ItemDescriptionText", rightPanel.transform, new Vector2(0, 0.25f), new Vector2(1, 0.85f), new Vector2(0, 0), new Vector2(-20, -20), noItemSelectedMessage, 20, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f, 1f));
        itemDescriptionText.overflowMode = TextOverflowModes.Overflow;
        itemDescriptionText.textWrappingMode = TextWrappingModes.Normal;

        itemPriceText = CreateText("ItemPriceText", rightPanel.transform, new Vector2(0, 0.15f), new Vector2(1, 0.22f), new Vector2(0, 0), new Vector2(-20, -10), "", 24, TextAnchor.MiddleCenter, Color.yellow);

        buyButton = CreateButton("BuyButton", rightPanel.transform, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.08f), new Vector2(0, 0), new Vector2(0, 50), buyButtonText, buyButtonColor, 24);
        buyButton.onClick.AddListener(BuySelectedItem);

        sellButton = CreateButton("SellButton", shopPanel.transform, new Vector2(0.3f, 0), new Vector2(0.7f, 0), new Vector2(0, 25), new Vector2(0, 45), sellButtonText, sellButtonColor, 20);
        sellButton.onClick.AddListener(SellAllSouls);

        tooltipPanel = CreatePanel("TooltipPanel", shopPanel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(300, 100), new Color(0.05f, 0.05f, 0.05f, 0.95f));
        tooltipPanel.SetActive(false);
        tooltipText = CreateText("TooltipText", tooltipPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), "", 22, TextAnchor.MiddleCenter, Color.white);
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tooltipTextRt = tooltipText.GetComponent<RectTransform>();
        tooltipTextRt.offsetMin = new Vector2(12, 12);
        tooltipTextRt.offsetMax = new Vector2(-12, -12);
        ContentSizeFitter tooltipFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        tooltipFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        shopPanel.SetActive(false);
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
