using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int quantity;
}

public class Inventory : MonoBehaviour
{
    public List<InventoryItem> items = new List<InventoryItem>();
    public int maxSlots = 20;

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        if (item.maxStack > 1)
        {
            InventoryItem existing = items.Find(i => i.itemData == item && i.quantity < item.maxStack);
            if (existing != null)
            {
                int space = item.maxStack - existing.quantity;
                int add = Mathf.Min(quantity, space);
                existing.quantity += add;
                quantity -= add;
                if (quantity <= 0) return true;
            }
        }

        while (quantity > 0)
        {
            if (items.Count >= maxSlots) return false;

            int stack = Mathf.Min(quantity, item.maxStack);
            items.Add(new InventoryItem { itemData = item, quantity = stack });
            quantity -= stack;
        }

        return true;
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].itemData == item)
            {
                items[i].quantity -= quantity;
                if (items[i].quantity <= 0)
                {
                    quantity = -items[i].quantity;
                    items.RemoveAt(i);
                }
                if (quantity <= 0) return true;
            }
        }

        return false;
    }

    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (InventoryItem invItem in items)
        {
            if (invItem.itemData == item)
                count += invItem.quantity;
        }
        return count;
    }
}
