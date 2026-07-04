using UnityEngine;
using UnityEngine.InputSystem;

public class ShopNPC : MonoBehaviour
{
    public ShopUIController shopUI;
    public GameObject interactPrompt;

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    public void OpenShop()
    {
        ShopUIController target = shopUI;
        if (target == null || target.shopPanel == null)
        {
            ShopUIController[] controllers = FindObjectsByType<ShopUIController>();
            int best = -1;
            foreach (ShopUIController c in controllers)
            {
                if (c.shopPanel == null) continue;
                int score = CountRefs(c);
                if (score > best)
                {
                    best = score;
                    target = c;
                }
            }
        }

        if (target == null)
        {
            Debug.LogError($"[{name}] ShopNPC has no usable ShopUIController. Assign one in the inspector or run Tools/Build Shop UI.");
            return;
        }
        if (target.shopPanel == null)
        {
            Debug.LogError($"[{name}] ShopUIController '{target.name}' has no shopPanel assigned.");
            return;
        }

        target.OpenShop();
    }

    int CountRefs(ShopUIController c)
    {
        int count = 0;
        if (c.shopPanel != null) count++;
        if (c.cardContainer != null) count++;
        if (c.goldText != null) count++;
        if (c.itemNameText != null) count++;
        if (c.itemDescriptionText != null) count++;
        if (c.itemPriceText != null) count++;
        if (c.itemIcon != null) count++;
        if (c.buyButton != null) count++;
        if (c.sellButton != null) count++;
        if (c.closeButton != null) count++;
        if (c.tooltipPanel != null) count++;
        if (c.tooltipText != null) count++;
        return count;
    }
}
