using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("Equipped Items")]
    public ItemData weapon;
    public ItemData armor;
    public ItemData boots;
    public ItemData accessory;

    private PlayerStats playerStats;
    private AutoCombat playerCombat;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
        playerCombat = FindAnyObjectByType<AutoCombat>();
    }

    public bool IsEquipped(ItemData item)
    {
        if (item == null) return false;
        return weapon == item || armor == item || boots == item || accessory == item;
    }

    public ItemData GetEquipped(ItemType type)
    {
        return type switch
        {
            ItemType.Weapon => weapon,
            ItemType.Armor => armor,
            _ => null
        };
    }

    public bool CanEquip(ItemData item)
    {
        if (item == null) return false;
        return item.itemType == ItemType.Weapon ||
               item.itemType == ItemType.Armor ||
               item.itemType == ItemType.Misc;
    }

    public bool Equip(ItemData item, Inventory inventory)
    {
        if (item == null || inventory == null) return false;
        if (!CanEquip(item)) return false;
        if (IsEquipped(item)) return false;
        if (inventory.GetItemCount(item) <= 0) return false;

        ItemData slot = GetSlotForItem(item);
        if (slot != null)
            Unequip(slot, inventory);

        SetSlotForItem(item);
        ApplyStats(item);
        return true;
    }

    public bool Unequip(ItemData item, Inventory inventory)
    {
        if (item == null) return false;
        if (!IsEquipped(item)) return false;

        ClearSlotForItem(item);
        RemoveStats(item);
        return true;
    }

    ItemData GetSlotForItem(ItemData item)
    {
        if (item.itemType == ItemType.Weapon) return weapon;
        if (IsBoots(item)) return boots;
        if (item.itemType == ItemType.Armor) return armor;
        if (item.itemType == ItemType.Misc) return accessory;
        return null;
    }

    void SetSlotForItem(ItemData item)
    {
        if (item.itemType == ItemType.Weapon) weapon = item;
        else if (IsBoots(item)) boots = item;
        else if (item.itemType == ItemType.Armor) armor = item;
        else if (item.itemType == ItemType.Misc) accessory = item;
    }

    bool IsBoots(ItemData item)
    {
        return item != null && item.spdBonus > 0 && item.atkBonus == 0 && item.defBonus == 0;
    }

    void ClearSlotForItem(ItemData item)
    {
        if (weapon == item) weapon = null;
        if (armor == item) armor = null;
        if (boots == item) boots = null;
        if (accessory == item) accessory = null;
    }

    void ApplyStats(ItemData item)
    {
        if (playerStats == null) return;
        playerStats.atk += item.atkBonus;
        playerStats.def += item.defBonus;
        playerStats.maxHp += item.hpBonus;
        playerStats.spd += item.spdBonus;
        playerStats.lck += item.lckBonus;

        if (playerCombat != null)
        {
            playerCombat.maxHealth = playerStats.maxHp;
            playerCombat.damage = playerStats.atk;
        }
    }

    void RemoveStats(ItemData item)
    {
        if (playerStats == null) return;
        playerStats.atk -= item.atkBonus;
        playerStats.def -= item.defBonus;
        playerStats.maxHp -= item.hpBonus;
        playerStats.spd -= item.spdBonus;
        playerStats.lck -= item.lckBonus;
        playerStats.hp = Mathf.Min(playerStats.hp, playerStats.maxHp);

        if (playerCombat != null)
        {
            playerCombat.maxHealth = playerStats.maxHp;
            playerCombat.damage = playerStats.atk;
        }
    }
}
