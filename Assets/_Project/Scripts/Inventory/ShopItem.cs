using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public ItemData itemData;
    public int price;
    public int quantity = -1; // -1 = unlimited
}
