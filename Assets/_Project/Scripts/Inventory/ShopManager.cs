using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Static Shop Items (potions, XP, materials - always available)")]
    public ShopItem[] shopItems;

    [Header("Gear Tiers (replace by player level)")]
    public ItemData[] weaponTiers;
    public ItemData[] armorTiers;
    public ItemData[] bootTiers;
    public ItemData[] accessoryTiers;

    [Header("Tier Unlock Levels")]
    public int[] tierUnlockLevels = { 1, 5, 12, 25 };

    [Header("Sell Multiplier")]
    public float sellMultiplier = 0.5f;

    private List<ShopItem> dynamicShopItems;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RebuildDynamicShop(1);
    }

    public void RebuildDynamicShop(int playerLevel)
    {
        dynamicShopItems = new List<ShopItem>();

        foreach (ShopItem si in shopItems)
            dynamicShopItems.Add(new ShopItem { itemData = si.itemData, price = si.price, quantity = si.quantity });

        int tier = GetTierForLevel(playerLevel);

        if (weaponTiers != null && tier < weaponTiers.Length && weaponTiers[tier] != null)
            dynamicShopItems.Add(new ShopItem { itemData = weaponTiers[tier], price = weaponTiers[tier].price, quantity = -1 });

        if (armorTiers != null && tier < armorTiers.Length && armorTiers[tier] != null)
            dynamicShopItems.Add(new ShopItem { itemData = armorTiers[tier], price = armorTiers[tier].price, quantity = -1 });

        if (bootTiers != null && tier < bootTiers.Length && bootTiers[tier] != null)
            dynamicShopItems.Add(new ShopItem { itemData = bootTiers[tier], price = bootTiers[tier].price, quantity = -1 });

        if (accessoryTiers != null && tier < accessoryTiers.Length && accessoryTiers[tier] != null)
            dynamicShopItems.Add(new ShopItem { itemData = accessoryTiers[tier], price = accessoryTiers[tier].price, quantity = -1 });
    }

    int GetTierForLevel(int level)
    {
        int tier = 0;
        for (int i = 0; i < tierUnlockLevels.Length; i++)
        {
            if (level >= tierUnlockLevels[i])
                tier = i;
        }
        return tier;
    }

    public ShopItem[] GetCurrentShopItems()
    {
        if (dynamicShopItems != null && dynamicShopItems.Count > 0)
            return dynamicShopItems.ToArray();
        return shopItems;
    }

    public bool BuyItem(int itemIndex, Inventory playerInventory, PlayerStats playerStats)
    {
        ShopItem[] current = GetCurrentShopItems();
        if (itemIndex < 0 || itemIndex >= current.Length) return false;

        ShopItem shopItem = current[itemIndex];
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

    public int SellItem(ItemData item, int quantity, Inventory playerInventory, PlayerStats playerStats)
    {
        if (item == null || playerInventory == null || playerStats == null) return 0;

        int available = playerInventory.GetItemCount(item);
        int sellQuantity = Mathf.Min(quantity, available);
        if (sellQuantity <= 0) return 0;

        int basePrice = item.price > 0 ? item.price : 1;
        int pricePerUnit = Mathf.Max(1, Mathf.RoundToInt(basePrice * sellMultiplier));
        int total = sellQuantity * pricePerUnit;

        playerInventory.RemoveItem(item, sellQuantity);
        playerStats.gold += total;

        return total;
    }
}
