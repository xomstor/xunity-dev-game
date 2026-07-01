using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    Misc
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public ItemType itemType;
    public Sprite icon;
    public int price;
    public int maxStack = 1;

    [Header("Stats Modifiers")]
    public int atkBonus;
    public int defBonus;
    public int hpBonus;
    public int spdBonus;
    public int lckBonus;
    public int restoreHp;
    public int experienceReward;
}
