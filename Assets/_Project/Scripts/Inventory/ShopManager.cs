using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    public ShopItem[] shopItems;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool BuyItem(int itemIndex, Inventory playerInventory, PlayerStats playerStats)
    {
        if (itemIndex < 0 || itemIndex >= shopItems.Length) return false;

        ShopItem shopItem = shopItems[itemIndex];
        if (shopItem.itemData == null) return false;
        if (shopItem.quantity == 0) return false;

        int price = shopItem.price > 0 ? shopItem.price : shopItem.itemData.price;
        if (playerStats.gold < price) return false;

        if (!playerInventory.AddItem(shopItem.itemData, 1)) return false;

        playerStats.gold -= price;
        if (shopItem.quantity > 0)
            shopItem.quantity--;

        return true;
    }
}
