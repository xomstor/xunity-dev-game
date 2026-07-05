using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public string buyButtonText = "BUY";
    public string sellButtonText = "Продать Души";
    public string closeButtonText = "X";
    public string pricePrefix = "";
    public string priceSuffix = "";
    public string purchasedMessage = "Purchased!";
    public string notEnoughMessage = "Not enough gold or inventory full";
    public string noItemSelectedMessage = "Select an item";

    [Header("Dynamic Pricing")]
    public float xpPotionPriceMultiplier = 1.1f;
    public string xpPotionIdSubstring = "xp";

    [Header("UI Colors")]
    public Color panelColor = new Color(0.08f, 0.04f, 0.02f, 0.95f);
    public Color headerColor = new Color(0.35f, 0.18f, 0.08f, 1f);
    public Color cardColor = new Color(0.45f, 0.25f, 0.12f, 1f);
    public Color cardHighlightColor = new Color(0.65f, 0.4f, 0.2f, 1f);
    public Color buyButtonColor = new Color(0.2f, 0.7f, 0.25f, 1f);
    public Color sellButtonColor = new Color(0.75f, 0.55f, 0.15f, 1f);
    public Color closeButtonColor = new Color(0.7f, 0.2f, 0.15f, 1f);
    public Color tabColor = new Color(0.4f, 0.22f, 0.1f, 1f);
    public Color tabActiveColor = new Color(0.7f, 0.4f, 0.15f, 1f);
    public Color tooltipColor = new Color(0.1f, 0.08f, 0.04f, 0.95f);

    [Header("UI Settings")]
    public int cardsPerPage = 4;
    public Vector2 screenInset = new Vector2(60, 60);

    [Header("UI Elements (assign manually or auto-create)")]
    public GameObject shopPanel;
    public Transform categoryContainer;
    public Transform cardContainer;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIcon;
    public Button buyButton;
    public Button sellButton;
    public Button closeButton;
    public Button prevButton;
    public Button nextButton;
    public TextMeshProUGUI pageText;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public GameObject cardButtonPrefab;

    private int selectedItemIndex = -1;
    private int currentPage = 0;
    private int currentCategory = 0;
    private List<GameObject> cardButtons = new List<GameObject>();
    private List<Button> tabButtons = new List<Button>();
    private List<int> filteredIndices = new List<int>();
    private Sprite whiteSprite;

    private readonly string[] categoryNames = { "All", "Weapons", "Armor", "Potions", "Misc" };

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

        categoryContainer ??= FindChildByName(shopPanel.transform, "CategoryPanel")?.transform;
        cardContainer ??= FindChildByName(shopPanel.transform, "CardContainer")?.transform;
        goldText ??= FindChildByName(shopPanel.transform, "GoldText")?.GetComponent<TextMeshProUGUI>();
        itemNameText ??= FindChildByName(shopPanel.transform, "ItemNameText")?.GetComponent<TextMeshProUGUI>();
        itemDescriptionText ??= FindChildByName(shopPanel.transform, "ItemDescriptionText")?.GetComponent<TextMeshProUGUI>();
        itemPriceText ??= FindChildByName(shopPanel.transform, "ItemPriceText")?.GetComponent<TextMeshProUGUI>();
        itemIcon ??= FindChildByName(shopPanel.transform, "ItemIcon")?.GetComponent<Image>();
        buyButton ??= FindChildByName(shopPanel.transform, "BuyButton")?.GetComponent<Button>();
        sellButton ??= FindChildByName(shopPanel.transform, "SellButton")?.GetComponent<Button>();
        closeButton ??= FindChildByName(shopPanel.transform, "CloseButton")?.GetComponent<Button>();
        prevButton ??= FindChildByName(shopPanel.transform, "PrevButton")?.GetComponent<Button>();
        nextButton ??= FindChildByName(shopPanel.transform, "NextButton")?.GetComponent<Button>();
        pageText ??= FindChildByName(shopPanel.transform, "PageText")?.GetComponent<TextMeshProUGUI>();

        if (tabButtons.Count == 0 && categoryContainer != null)
        {
            foreach (Transform child in categoryContainer)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) tabButtons.Add(btn);
            }
        }

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
        if (c.categoryContainer != null) count++;
        if (c.cardContainer != null) count++;
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
        if (shopPanel == null)
        {
            Debug.LogError($"[{name}] ShopUIController.shopPanel is null.");
            return;
        }

        Transform current = shopPanel.transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            if (current == transform)
                break;
            current = current.parent;
        }

        Canvas canvas = shopPanel.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvas.sortingLayerID = SortingLayer.NameToID("Foreground");
        }

        shopPanel.SetActive(true);
        FitPanelToScreen();

        selectedItemIndex = -1;
        currentPage = 0;
        currentCategory = 0;
        ClearItemDetails();
        if (categoryContainer != null)
        {
            int tabCount = 0;
            foreach (Transform child in categoryContainer)
                if (child != null && child.name.StartsWith("Tab_")) tabCount++;
            if (tabCount == 0)
                CreateCategoryTabs(categoryContainer);
            else
            {
                tabButtons.Clear();
                foreach (Transform child in categoryContainer)
                {
                    Button btn = child.GetComponent<Button>();
                    if (btn != null) tabButtons.Add(btn);
                }
            }
        }
        RefreshCategoryTabs();
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

    void FitPanelToScreen()
    {
        RectTransform rt = shopPanel.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = screenInset;
        rt.offsetMax = -screenInset;
    }

    public void CreateShopUI()
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

        shopPanel = CreatePanel("ShopPanel", shopCanvasGO.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, panelColor);

        // Header
        GameObject header = CreatePanel("Header", shopPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(0, 180), headerColor);
        CreateText("TitleText", header.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 25), new Vector2(500, 80), titleText, 48, TextAnchor.MiddleLeft, Color.white);
        closeButton = CreatePillButton("CloseButton", header.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-70, -65), new Vector2(90, 90), closeButtonText, closeButtonColor, new Color(0.5f, 0.15f, 0.1f, 1f), new Color(0.2f, 0.05f, 0.02f, 0.8f), 36, Color.white);
        closeButton.onClick.AddListener(CloseShop);
        goldText = CreateText("GoldText", header.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-450, 25), new Vector2(360, 80), goldPrefix + "0", 40, TextAnchor.MiddleRight, Color.yellow);

        // Category tabs inside header
        GameObject categoryPanel = CreatePanel("CategoryPanel", header.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 40), new Vector2(0, 70), new Color(0, 0, 0, 0));
        categoryContainer = categoryPanel.transform;
        CreateCategoryTabs(categoryPanel.transform);

        // Card grid
        GameObject cardPanel = CreatePanel("CardPanel", shopPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 50), new Vector2(0, -660), new Color(0, 0, 0, 0.2f));
        GameObject cardGrid = CreatePanel("CardContainer", cardPanel.transform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        GridLayoutGroup grid = cardGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(240, 380);
        grid.spacing = new Vector2(24, 24);
        grid.padding = new RectOffset(30, 30, 20, 20);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 1;
        cardContainer = cardGrid.transform;

        // Details panel (bottom)
        GameObject detailsPanel = CreatePanel("DetailsPanel", shopPanel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 220), new Vector2(0, 320), new Color(0, 0, 0, 0.3f));
        itemIcon = CreateImage("ItemIcon", detailsPanel.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(150, 150));
        itemNameText = CreateText("ItemNameText", detailsPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 60), "", 36, TextAnchor.MiddleCenter, Color.white);
        itemDescriptionText = CreateText("ItemDescriptionText", detailsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300, 0), new Vector2(500, 110), noItemSelectedMessage, 28, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f, 1f));
        itemPriceText = CreateText("ItemPriceText", detailsPanel.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 100), new Vector2(220, 60), "", 32, TextAnchor.MiddleLeft, Color.yellow);
        buyButton = CreatePillButton("BuyButton", detailsPanel.transform, new Vector2(90, -90), new Vector2(300, 90), buyButtonText, buyButtonColor, new Color(0.15f, 0.5f, 0.2f, 1f), new Color(0.05f, 0.2f, 0.08f, 0.8f), 34, Color.white);
        buyButton.onClick.AddListener(BuySelectedItem);

        sellButton = CreatePillButton("SellButton", detailsPanel.transform, new Vector2(-240, -90), new Vector2(360, 90), sellButtonText, sellButtonColor, new Color(0.55f, 0.4f, 0.12f, 1f), new Color(0.2f, 0.12f, 0.03f, 0.8f), 28, Color.white);
        sellButton.onClick.AddListener(SellAllSouls);

        // Pagination
        GameObject pagination = CreatePanel("Pagination", shopPanel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 30), new Vector2(0, 60), new Color(0, 0, 0, 0));
        prevButton = CreatePillButton("PrevButton", pagination.transform, new Vector2(-80, 0), new Vector2(100, 60), "<", tabColor, new Color(0.3f, 0.15f, 0.07f, 1f), new Color(0.15f, 0.07f, 0.03f, 0.8f), 36, Color.white);
        prevButton.onClick.AddListener(PrevPage);
        pageText = CreateText("PageText", pagination.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 60), "1 / 1", 32, TextAnchor.MiddleCenter, Color.white);
        nextButton = CreatePillButton("NextButton", pagination.transform, new Vector2(80, 0), new Vector2(100, 60), ">", tabColor, new Color(0.3f, 0.15f, 0.07f, 1f), new Color(0.15f, 0.07f, 0.03f, 0.8f), 36, Color.white);
        nextButton.onClick.AddListener(NextPage);

        // Tooltip
        tooltipPanel = CreatePanel("TooltipPanel", shopCanvasGO.transform, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(260, 0), tooltipColor);
        tooltipPanel.SetActive(false);
        tooltipText = CreateText("TooltipText", tooltipPanel.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 0), "", 18, TextAnchor.MiddleCenter, Color.white);
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tooltipTextRt = tooltipText.GetComponent<RectTransform>();
        tooltipTextRt.offsetMin = new Vector2(12, 12);
        tooltipTextRt.offsetMax = new Vector2(-12, -12);
        ContentSizeFitter tooltipFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        tooltipFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        WireUpEvents();
        shopPanel.SetActive(false);
    }

    void CreateCategoryTabs(Transform parent)
    {
        tabButtons.Clear();
        HorizontalLayoutGroup oldHlg = parent.GetComponent<HorizontalLayoutGroup>();
        if (oldHlg != null) Destroy(oldHlg);
        foreach (Transform child in parent)
            if (child != null && child.name.StartsWith("Tab_"))
                Destroy(child.gameObject);

        float totalWidth = categoryNames.Length * 150f + (categoryNames.Length - 1) * 15f;
        float startX = -totalWidth / 2f + 75f;
        for (int i = 0; i < categoryNames.Length; i++)
        {
            int idx = i;
            Color left = i == currentCategory ? tabActiveColor : tabColor;
            Color right = new Color(left.r * 0.7f, left.g * 0.7f, left.b * 0.7f, 1f);
            Button btn = CreatePillButton($"Tab_{categoryNames[i]}", parent, new Vector2(startX + i * 165f, 0), new Vector2(150, 70), categoryNames[i], left, right, new Color(0.2f, 0.1f, 0.05f, 0.8f), 32, Color.white);
            btn.onClick.AddListener(() => SelectCategory(idx));
            tabButtons.Add(btn);
        }
    }

    void RefreshCategoryTabs()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            Image img = tabButtons[i].GetComponent<Image>();
            if (img != null)
            {
                Color target = i == currentCategory ? tabActiveColor : tabColor;
                img.color = new Color(target.r, target.g, target.b, 1f);
            }
        }
    }

    void SelectCategory(int index)
    {
        Debug.Log($"[ShopUIController] Selected category {index}: {categoryNames[index]}");
        currentCategory = index;
        currentPage = 0;
        selectedItemIndex = -1;
        ClearItemDetails();
        RefreshCategoryTabs();
        RefreshShopItems();
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshShopItems();
        }
    }

    void NextPage()
    {
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(filteredIndices.Count / (float)cardsPerPage) - 1);
        if (currentPage < maxPage)
        {
            currentPage++;
            RefreshShopItems();
        }
    }

    void WireUpEvents()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        if (buyButton != null) buyButton.onClick.AddListener(BuySelectedItem);
        if (sellButton != null) sellButton.onClick.AddListener(SellAllSouls);
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
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
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
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

    Button CreatePillButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, string text, Color leftColor, Color rightColor, Color shadowColor, int fontSize, Color textColor)
    {
        return CreatePillButton(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta, text, leftColor, rightColor, shadowColor, fontSize, textColor);
    }

    Button CreatePillButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string text, Color leftColor, Color rightColor, Color shadowColor, int fontSize, Color textColor)
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
        int texW = Mathf.Max(32, (int)sizeDelta.x + 20);
        int texH = Mathf.Max(32, (int)sizeDelta.y + 20);
        img.sprite = CreateGradientPillSprite(texW, texH, (int)sizeDelta.x, (int)sizeDelta.y, leftColor, rightColor, shadowColor, new Vector2(4, -4), Color.black, 3);
        img.type = Image.Type.Simple;
        img.color = Color.white;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.05f, 0.02f, 0.9f);
        outline.effectDistance = new Vector2(3, -3);

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
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return btn;
    }

    Sprite CreateGradientPillSprite(int texWidth, int texHeight, int pillWidth, int pillHeight, Color leftColor, Color rightColor, Color shadowColor, Vector2 shadowOffset, Color outlineColor, int outlineThickness)
    {
        Texture2D tex = new Texture2D(texWidth, texHeight);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[texWidth * texHeight];
        Vector2 center = new Vector2(texWidth / 2f, texHeight / 2f);
        Vector2 shadowCenter = center + shadowOffset;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int i = y * texWidth + x;
                pixels[i] = Color.clear;

                if (IsInsidePill(x, y, shadowCenter.x, shadowCenter.y, pillWidth, pillHeight))
                    pixels[i] = shadowColor;

                bool insidePill = IsInsidePill(x, y, center.x, center.y, pillWidth, pillHeight);
                bool insideOutline = IsInsidePill(x, y, center.x, center.y, pillWidth - outlineThickness * 2, pillHeight - outlineThickness * 2);

                if (insidePill)
                {
                    if (insideOutline)
                    {
                        float t = Mathf.InverseLerp(center.x - pillWidth / 2f, center.x + pillWidth / 2f, x);
                        pixels[i] = Color.Lerp(leftColor, rightColor, t);
                    }
                    else
                    {
                        pixels[i] = outlineColor;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 100);
    }

    bool IsInsidePill(int x, int y, float cx, float cy, int width, int height)
    {
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float radius = halfHeight;
        float dx = Mathf.Abs(x - cx);
        float dy = Mathf.Abs(y - cy);
        if (dx <= halfWidth - radius) return dy <= halfHeight;
        dx -= halfWidth - radius;
        return dx * dx + dy * dy <= radius * radius;
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

    int GetItemCategory(ShopItem shopItem)
    {
        if (shopItem.itemData == null) return 4;
        string lowerName = shopItem.itemData.itemName.ToLowerInvariant();
        switch (shopItem.itemData.itemType)
        {
            case ItemType.Weapon: return 1;
            case ItemType.Armor: return 2;
            case ItemType.Consumable: return 3;
            default:
                if (lowerName.Contains("sword") || lowerName.Contains("axe") || lowerName.Contains("bow") || lowerName.Contains("dagger") || lowerName.Contains("mace") || lowerName.Contains("staff") || lowerName.Contains("blade"))
                    return 1;
                if (lowerName.Contains("armor") || lowerName.Contains("shield") || lowerName.Contains("helm") || lowerName.Contains("boot") || lowerName.Contains("chest") || lowerName.Contains("gauntlet"))
                    return 2;
                if (lowerName.Contains("potion") || lowerName.Contains("heal") || lowerName.Contains("elixir") || lowerName.Contains("scroll") || lowerName.Contains("spell"))
                    return 3;
                return 4;
        }
    }

    void RefreshShopItems()
    {
        if (cardContainer == null || shopManager == null) return;

        shopManager.RebuildDynamicShop();

        ShopItem[] currentItems = shopManager.GetCurrentShopItems();
        filteredIndices.Clear();
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (currentItems[i].itemData == null) continue;
            if (currentItems[i].quantity == 0) continue;
            if (currentCategory != 0 && GetItemCategory(currentItems[i]) != currentCategory) continue;
            filteredIndices.Add(i);
        }

        foreach (GameObject btn in cardButtons)
            if (btn != null) Destroy(btn);
        cardButtons.Clear();

        int maxPage = Mathf.Max(0, Mathf.CeilToInt(filteredIndices.Count / (float)cardsPerPage) - 1);
        currentPage = Mathf.Clamp(currentPage, 0, maxPage);

        int startIndex = currentPage * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, filteredIndices.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int realIndex = filteredIndices[i];
            Button card = CreateItemCard(currentItems[realIndex], realIndex);
            cardButtons.Add(card.gameObject);
        }

        if (pageText != null)
            pageText.text = $"{currentPage + 1} / {maxPage + 1}";
        if (prevButton != null)
            prevButton.interactable = currentPage > 0;
        if (nextButton != null)
            nextButton.interactable = currentPage < maxPage;
    }

    Button CreateItemCard(ShopItem shopItem, int index)
    {
        GameObject go = new GameObject($"Card_{shopItem.itemData.itemName}");
        go.transform.SetParent(cardContainer, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240, 380);
        go.AddComponent<CanvasRenderer>();
        Image bg = go.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.type = Image.Type.Simple;
        bg.color = cardColor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        // Icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 1);
        iconRt.anchorMax = new Vector2(0.5f, 1);
        iconRt.pivot = new Vector2(0.5f, 1);
        iconRt.anchoredPosition = new Vector2(0, -15);
        iconRt.sizeDelta = new Vector2(120, 120);
        iconGO.AddComponent<CanvasRenderer>();
        Image icon = iconGO.AddComponent<Image>();
        icon.sprite = shopItem.itemData.icon;
        icon.color = icon.sprite == null ? shopItem.itemData.GetRarityColor() : Color.white;

        // Name
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(go.transform, false);
        RectTransform nameRt = nameGO.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.62f);
        nameRt.anchorMax = new Vector2(1, 0.62f);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, 0);
        nameRt.sizeDelta = new Vector2(220, 50);
        nameGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = shopItem.itemData.itemName;
        nameText.fontSize = 24;
        nameText.color = shopItem.itemData.GetRarityColor();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        // Price
        GameObject priceGO = new GameObject("Price");
        priceGO.transform.SetParent(go.transform, false);
        RectTransform priceRt = priceGO.AddComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0.5f, 0);
        priceRt.anchorMax = new Vector2(0.5f, 0);
        priceRt.pivot = new Vector2(0.5f, 0);
        priceRt.anchoredPosition = new Vector2(0, 115);
        priceRt.sizeDelta = new Vector2(200, 40);
        priceGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI priceText = priceGO.AddComponent<TextMeshProUGUI>();
        priceText.text = $"{shopItem.price}g";
        priceText.fontSize = 28;
        priceText.color = Color.yellow;
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.raycastTarget = false;

        // Buy mini button
        Button buyMini = CreatePillButton("BuyMini", go.transform, new Vector2(0, 25), new Vector2(200, 55), buyButtonText, buyButtonColor, new Color(0.15f, 0.5f, 0.2f, 1f), new Color(0.05f, 0.2f, 0.08f, 0.8f), 26, Color.white);
        buyMini.onClick.AddListener(() => BuyItem(index));

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
        Vector2 pointerPos = Vector2.zero;
        if (Mouse.current != null)
            pointerPos = Mouse.current.position.ReadValue();
        else if (Pointer.current != null)
            pointerPos = Pointer.current.position.ReadValue();
        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        rt.position = new Vector3(pointerPos.x + 15, pointerPos.y - 15, 0);
    }

    void SelectItem(int index)
    {
        selectedItemIndex = index;
        ShopItem[] currentItems = shopManager.GetCurrentShopItems();
        if (index < 0 || index >= currentItems.Length) return;
        ShopItem shopItem = currentItems[index];
        if (itemIcon != null) itemIcon.sprite = shopItem.itemData.icon;
        if (itemNameText != null) itemNameText.text = shopItem.itemData.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = shopItem.itemData.description;
        if (itemPriceText != null) itemPriceText.text = $"{pricePrefix}{shopItem.price}{priceSuffix}";
    }

    void BuyItem(int index)
    {
        selectedItemIndex = index;
        ShopItem[] currentItems = shopManager.GetCurrentShopItems();
        if (index < 0 || index >= currentItems.Length) return;
        ShopItem shopItem = currentItems[index];
        if (shopItem.itemData == null) return;

        bool success = shopManager.BuyItem(index, playerInventory, playerStats);
        if (success && shopItem.itemData.itemId.Contains(xpPotionIdSubstring))
        {
            shopItem.price = Mathf.RoundToInt(shopItem.price * xpPotionPriceMultiplier);
        }
        UpdateGoldText();
        RefreshShopItems();
        if (itemDescriptionText != null)
            itemDescriptionText.text = success ? purchasedMessage : notEnoughMessage;
        if (success)
            SelectItem(index);
    }

    void BuySelectedItem()
    {
        if (selectedItemIndex < 0)
        {
            if (itemDescriptionText != null)
                itemDescriptionText.text = noItemSelectedMessage;
            return;
        }
        BuyItem(selectedItemIndex);
    }

    void SellAllSouls()
    {
        if (shopManager == null || playerInventory == null || playerStats == null) return;
        int totalGold = 0;

        if (soulItems != null && soulItems.Count > 0)
        {
            foreach (ItemData soul in soulItems)
            {
                if (soul == null) continue;
                totalGold += shopManager.SellItem(soul, int.MaxValue, playerInventory, playerStats);
            }
        }
        else
        {
            // Sell all materials if no explicit soul items configured
            List<ItemData> materialsToSell = new List<ItemData>();
            foreach (InventoryItem invItem in playerInventory.items)
            {
                if (invItem.itemData != null && invItem.itemData.itemType == ItemType.Material && !materialsToSell.Contains(invItem.itemData))
                    materialsToSell.Add(invItem.itemData);
            }
            foreach (ItemData material in materialsToSell)
                totalGold += shopManager.SellItem(material, int.MaxValue, playerInventory, playerStats);
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
            if (soulItems != null && soulItems.Count > 0)
            {
                foreach (ItemData soul in soulItems)
                {
                    if (soul == null) continue;
                    int count = playerInventory.GetItemCount(soul);
                    totalCount += count;
                    totalValue += count * (soul.price > 0 ? soul.price : 1);
                }
            }
            else
            {
                foreach (InventoryItem invItem in playerInventory.items)
                {
                    if (invItem.itemData != null && invItem.itemData.itemType == ItemType.Material)
                    {
                        totalCount += invItem.quantity;
                        totalValue += invItem.quantity * (invItem.itemData.price > 0 ? invItem.itemData.price : 1);
                    }
                }
            }
            text.text = $"{sellButtonText}\n({totalCount}) +{totalValue}g";
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
        if (itemIcon != null) itemIcon.sprite = null;
        if (itemNameText != null) itemNameText.text = "";
        if (itemDescriptionText != null) itemDescriptionText.text = noItemSelectedMessage;
        if (itemPriceText != null) itemPriceText.text = "";
    }
}
