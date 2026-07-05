using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Boots,
    Accessory,
    Consumable,
    Material,
    Misc
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public ItemType itemType;
    public ItemRarity rarity = ItemRarity.Common;
    public Sprite icon;
    public int price;
    public int maxStack = 1;

    [Header("Stats Modifiers")]
    public int atkBonus;
    public int defBonus;
    public int hpBonus;
    public int spdBonus;
    public int lckBonus;
    public int atkSpdBonus;
    public int lethalityBonus;
    public int restoreHp;
    public int experienceReward;

    public Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Common    => new Color(0.7f, 0.7f, 0.7f, 1f),
            ItemRarity.Uncommon  => new Color(0.3f, 0.8f, 0.3f, 1f),
            ItemRarity.Rare      => new Color(0.3f, 0.5f, 1f, 1f),
            ItemRarity.Epic      => new Color(0.7f, 0.3f, 1f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f, 1f),
            _                    => Color.white
        };
    }
}
