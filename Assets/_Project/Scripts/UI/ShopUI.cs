using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public ShopManager shopManager;
    public Inventory playerInventory;
    public PlayerStats playerStats;

    [Header("Sell")]
    public ItemData spiderTailItem;
    public Button sellButton;
    public TextMeshProUGUI sellText;

    [Header("UI")]
    public GameObject shopPanel;
    public Transform itemContainer;
    public GameObject shopItemPrefab;
    public TextMeshProUGUI goldText;
    public Button closeButton;

    [Header("Item UI")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;
    public Button buyButton;

    private int selectedItemIndex = -1;

    void Awake()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        if (buyButton != null)
            buyButton.gameObject.SetActive(false);

        if (sellButton != null)
            sellButton.onClick.AddListener(SellSpiderTails);
    }

    void Update()
    {
        if (shopPanel != null && shopPanel.activeInHierarchy)
            UpdateGoldText();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);

        RefreshShopItems();
        UpdateGoldText();
        UpdateSellText();
        ClearItemDetails();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    void RefreshShopItems()
    {
        if (itemContainer == null || shopManager == null) return;

        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < shopManager.shopItems.Length; i++)
        {
            ShopItem shopItem = shopManager.shopItems[i];
            if (shopItem.itemData == null) continue;

            GameObject itemObject = Instantiate(shopItemPrefab, itemContainer);
            Button button = itemObject.GetComponent<Button>();
            Image icon = itemObject.GetComponentInChildren<Image>();
            TextMeshProUGUI nameText = itemObject.GetComponentInChildren<TextMeshProUGUI>();

            if (icon != null)
                icon.sprite = shopItem.itemData.icon;
            if (nameText != null)
                nameText.text = shopItem.itemData.itemName;

            int index = i;
            if (button != null)
                button.onClick.AddListener(() => BuyItem(index));
        }
    }

    void BuyItem(int index)
    {
        if (shopManager == null || playerInventory == null || playerStats == null) return;

        ShopItem shopItem = shopManager.shopItems[index];
        if (shopItem == null || shopItem.itemData == null) return;

        bool success = shopManager.BuyItem(index, playerInventory, playerStats);
        if (success)
        {
            UpdateGoldText();
            RefreshShopItems();
        }

        SelectItem(index);

        if (itemDescriptionText != null)
            itemDescriptionText.text = success ? "Purchased!" : "Not enough gold or inventory full";
    }

    void SelectItem(int index)
    {
        selectedItemIndex = index;
        ShopItem shopItem = shopManager.shopItems[index];

        if (itemIcon != null)
            itemIcon.sprite = shopItem.itemData.icon;
        if (itemNameText != null)
            itemNameText.text = shopItem.itemData.itemName;
        if (itemDescriptionText != null && itemDescriptionText.text != "Purchased!" && itemDescriptionText.text != "Not enough gold or inventory full")
            itemDescriptionText.text = shopItem.itemData.description;
        if (itemPriceText != null)
            itemPriceText.text = $"Price: {shopItem.price}g";
    }

    void SellSpiderTails()
    {
        if (shopManager == null || playerInventory == null || playerStats == null) return;
        if (spiderTailItem == null) return;

        int sold = shopManager.SellItem(spiderTailItem, int.MaxValue, playerInventory, playerStats);
        if (sold > 0)
        {
            UpdateGoldText();
            UpdateSellText();
        }
    }

    void UpdateSellText()
    {
        if (sellText == null || spiderTailItem == null || playerInventory == null) return;
        int count = playerInventory.GetItemCount(spiderTailItem);
        sellText.text = $"Sell Spider Tail ({count})\n+{count * spiderTailItem.price}g";
    }

    void UpdateGoldText()
    {
        if (goldText != null && playerStats != null)
            goldText.text = $"Gold: {playerStats.gold}";
    }

    void ClearItemDetails()
    {
        selectedItemIndex = -1;
        if (itemIcon != null)
            itemIcon.sprite = null;
        if (itemNameText != null)
            itemNameText.text = "";
        if (itemDescriptionText != null)
            itemDescriptionText.text = "";
        if (itemPriceText != null)
            itemPriceText.text = "";
    }
}
