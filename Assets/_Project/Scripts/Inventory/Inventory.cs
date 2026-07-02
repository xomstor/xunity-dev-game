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

        int added = 0;

        if (item.maxStack > 1)
        {
            InventoryItem existing = items.Find(i => i.itemData == item && i.quantity < item.maxStack);
            if (existing != null)
            {
                int space = item.maxStack - existing.quantity;
                int add = Mathf.Min(quantity, space);
                existing.quantity += add;
                added += add;
                quantity -= add;
                if (quantity <= 0)
                {
                    NotifyPickup(item, added);
                    return true;
                }
            }
        }

        while (quantity > 0)
        {
            if (items.Count >= maxSlots)
            {
                if (added > 0) NotifyPickup(item, added);
                return false;
            }

            int stack = Mathf.Min(quantity, item.maxStack);
            items.Add(new InventoryItem { itemData = item, quantity = stack });
            added += stack;
            quantity -= stack;
        }

        NotifyPickup(item, added);
        return true;
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        int removed = 0;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].itemData == item)
            {
                int take = Mathf.Min(quantity - removed, items[i].quantity);
                items[i].quantity -= take;
                removed += take;
                if (items[i].quantity <= 0)
                {
                    items.RemoveAt(i);
                }
                if (removed >= quantity)
                {
                    NotifySpend(item, removed);
                    return true;
                }
            }
        }

        if (removed > 0) NotifySpend(item, removed);
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

    void NotifyPickup(ItemData item, int qty)
    {
        if (ItemNotification.Instance != null)
            ItemNotification.Instance.ShowPickup(item.itemName, qty);
    }

    void NotifySpend(ItemData item, int qty)
    {
        if (ItemNotification.Instance != null)
            ItemNotification.Instance.ShowSpend(item.itemName, qty);
    }
}
