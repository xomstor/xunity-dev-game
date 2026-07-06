using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TrashedItem
{
    public string itemId;
    public string itemName;
    public int quantity;
    public string trashedTime;

    [NonSerialized]
    public ItemData itemData;

    public DateTime GetTrashedTime() => DateTime.TryParse(trashedTime, out DateTime dt) ? dt : DateTime.MinValue;
}

public enum TrashedItemState
{
    Recoverable,
    Payable,
    Destroyed
}

public class TrashEmanator : MonoBehaviour
{
    public static TrashEmanator Instance { get; private set; }

    [Header("Item Lifetimes")]
    public float freeHours = 24f;
    public float payableHours = 24f;
    public int buyBackPricePerLevel = 20;

    [Header("UI")]
    public GameObject uiPanel;

    public List<TrashedItem> trashedItems = new List<TrashedItem>();

    public event Action OnListChanged;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return;
        trashedItems.Add(new TrashedItem
        {
            itemId = item.itemId,
            itemName = item.itemName,
            quantity = quantity,
            trashedTime = DateTime.Now.ToString("O"),
            itemData = item
        });
        NotifyListChanged();
        Debug.Log($"[TrashEmanator] Получен предмет: {item.itemName} x{quantity}");
    }

    public void NotifyListChanged()
    {
        OnListChanged?.Invoke();
    }

    public TrashedItemState GetItemState(TrashedItem item)
    {
        DateTime trashed = item.GetTrashedTime();
        if (trashed == DateTime.MinValue) return TrashedItemState.Destroyed;
        TimeSpan elapsed = DateTime.Now - trashed;
        if (elapsed.TotalHours < freeHours) return TrashedItemState.Recoverable;
        if (elapsed.TotalHours < freeHours + payableHours) return TrashedItemState.Payable;
        return TrashedItemState.Destroyed;
    }

    public int GetBuyBackPrice()
    {
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        int level = stats != null ? stats.level : 1;
        return level * buyBackPricePerLevel;
    }

    public bool RecoverItem(int index, Inventory inventory, PlayerStats stats)
    {
        if (index < 0 || index >= trashedItems.Count) return false;
        TrashedItem item = trashedItems[index];
        TrashedItemState state = GetItemState(item);
        if (state == TrashedItemState.Destroyed) return false;

        if (state == TrashedItemState.Payable)
        {
            int price = GetBuyBackPrice();
            if (stats == null || stats.gold < price) return false;
            stats.gold -= price;
        }

        ItemData data = item.itemData ?? FindItemById(item.itemId);
        if (data == null) return false;
        if (inventory != null && !inventory.AddItem(data, item.quantity)) return false;

        trashedItems.RemoveAt(index);
        NotifyListChanged();
        Debug.Log($"[TrashEmanator] Возвращен предмет: {data.itemName} x{item.quantity}");
        return true;
    }

    public void CleanupDestroyedItems()
    {
        bool removed = false;
        for (int i = trashedItems.Count - 1; i >= 0; i--)
        {
            if (GetItemState(trashedItems[i]) == TrashedItemState.Destroyed)
            {
                Debug.Log($"[TrashEmanator] Уничтожен просроченный предмет: {trashedItems[i].itemName}");
                trashedItems.RemoveAt(i);
                removed = true;
            }
        }
        if (removed) NotifyListChanged();
    }

    public void OpenUI()
    {
        CleanupDestroyedItems();
        if (uiPanel != null)
            uiPanel.SetActive(true);
        else
            Debug.Log("[TrashEmanator] UI панель не назначена.");
    }

    public void CloseUI()
    {
        if (uiPanel != null)
            uiPanel.SetActive(false);
    }

    public ItemData FindItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in allItems)
            if (item.itemId == itemId)
                return item;
        return null;
    }
}
