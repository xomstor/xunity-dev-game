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

    public struct EquipResult
    {
        public bool success;
        public ItemData replacedItem;
    }

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
            ItemType.Boots => boots,
            ItemType.Accessory => accessory,
            _ => null
        };
    }

    public bool CanEquip(ItemData item)
    {
        if (item == null) return false;
        return item.itemType == ItemType.Weapon ||
               item.itemType == ItemType.Armor ||
               item.itemType == ItemType.Boots ||
               item.itemType == ItemType.Accessory;
    }

    public EquipResult Equip(ItemData item, Inventory inventory)
    {
        EquipResult result = new EquipResult { success = false, replacedItem = null };

        if (item == null || inventory == null) return result;
        if (!CanEquip(item)) return result;
        if (IsEquipped(item)) return result;
        if (inventory.GetItemCount(item) <= 0) return result;

        ItemData previous = GetSlotForItem(item);
        if (previous != null)
        {
            Unequip(previous, inventory);
            result.replacedItem = previous;
        }

        SetSlotForItem(item);
        ApplyStats(item);
        result.success = true;
        return result;
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
        return item.itemType switch
        {
            ItemType.Weapon => weapon,
            ItemType.Armor => armor,
            ItemType.Boots => boots,
            ItemType.Accessory => accessory,
            _ => null
        };
    }

    void SetSlotForItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon: weapon = item; break;
            case ItemType.Armor: armor = item; break;
            case ItemType.Boots: boots = item; break;
            case ItemType.Accessory: accessory = item; break;
        }
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
