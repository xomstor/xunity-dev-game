using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public ShopManager shopManager;
    public Inventory playerInventory;
    public PlayerStats playerStats;

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
            buyButton.onClick.AddListener(BuySelectedItem);
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
                button.onClick.AddListener(() => SelectItem(index));
        }
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
            itemPriceText.text = $"Price: {shopItem.price}g";
    }

    void BuySelectedItem()
    {
        if (selectedItemIndex < 0) return;
        if (shopManager == null || playerInventory == null || playerStats == null) return;

        if (shopManager.BuyItem(selectedItemIndex, playerInventory, playerStats))
        {
            UpdateGoldText();
            RefreshShopItems();
            ClearItemDetails();
        }
        else
        {
            Debug.Log("Not enough gold or inventory full");
        }
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
