using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Static Shop Items (potions, XP, materials - always available)")]
    public ShopItem[] shopItems;

    [Header("Gear Tiers (each purchase unlocks the next tier)")]
    public ItemData[] weaponTiers;
    public ItemData[] armorTiers;
    public ItemData[] bootTiers;
    public ItemData[] accessoryTiers;

    [Header("Purchased Gear Tier Index")]
    public int weaponTierIndex = 0;
    public int armorTierIndex = 0;
    public int bootTierIndex = 0;
    public int accessoryTierIndex = 0;

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
        LoadTierIndices();
        RebuildDynamicShop();
    }

    public void RebuildDynamicShop()
    {
        dynamicShopItems = new List<ShopItem>();

        foreach (ShopItem si in shopItems)
            dynamicShopItems.Add(new ShopItem { itemData = si.itemData, price = si.price, quantity = si.quantity });

        AddGearItem(weaponTiers, weaponTierIndex, "weapon");
        AddGearItem(armorTiers, armorTierIndex, "armor");
        AddGearItem(bootTiers, bootTierIndex, "boot");
        AddGearItem(accessoryTiers, accessoryTierIndex, "accessory");
    }

    void AddGearItem(ItemData[] tiers, int tierIndex, string category)
    {
        if (tiers == null || tiers.Length == 0) return;
        if (tierIndex < 0 || tierIndex >= tiers.Length) return;
        if (tiers[tierIndex] == null) return;

        ItemData item = tiers[tierIndex];
        int price = item.price > 0 ? item.price : Mathf.RoundToInt(50 * Mathf.Pow(1.5f, tierIndex));
        dynamicShopItems.Add(new ShopItem { itemData = item, price = price, quantity = -1 });
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

        AdvanceGearTier(shopItem.itemData);
        SaveTierIndices();
        RebuildDynamicShop();

        return true;
    }

    void AdvanceGearTier(ItemData item)
    {
        if (item == null) return;

        if (weaponTiers != null && System.Array.IndexOf(weaponTiers, item) >= 0)
            weaponTierIndex = Mathf.Min(weaponTierIndex + 1, weaponTiers.Length - 1);
        else if (armorTiers != null && System.Array.IndexOf(armorTiers, item) >= 0)
            armorTierIndex = Mathf.Min(armorTierIndex + 1, armorTiers.Length - 1);
        else if (bootTiers != null && System.Array.IndexOf(bootTiers, item) >= 0)
            bootTierIndex = Mathf.Min(bootTierIndex + 1, bootTiers.Length - 1);
        else if (accessoryTiers != null && System.Array.IndexOf(accessoryTiers, item) >= 0)
            accessoryTierIndex = Mathf.Min(accessoryTierIndex + 1, accessoryTiers.Length - 1);
    }

    void SaveTierIndices()
    {
        PlayerPrefs.SetInt("ShopWeaponTier", weaponTierIndex);
        PlayerPrefs.SetInt("ShopArmorTier", armorTierIndex);
        PlayerPrefs.SetInt("ShopBootTier", bootTierIndex);
        PlayerPrefs.SetInt("ShopAccessoryTier", accessoryTierIndex);
        PlayerPrefs.Save();
    }

    void LoadTierIndices()
    {
        weaponTierIndex = PlayerPrefs.GetInt("ShopWeaponTier", 0);
        armorTierIndex = PlayerPrefs.GetInt("ShopArmorTier", 0);
        bootTierIndex = PlayerPrefs.GetInt("ShopBootTier", 0);
        accessoryTierIndex = PlayerPrefs.GetInt("ShopAccessoryTier", 0);
    }

    public void ResetShopProgression()
    {
        weaponTierIndex = 0;
        armorTierIndex = 0;
        bootTierIndex = 0;
        accessoryTierIndex = 0;
        SaveTierIndices();
        RebuildDynamicShop();
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
