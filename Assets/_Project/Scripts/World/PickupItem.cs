using UnityEngine;
using UnityEngine.InputSystem;

public class PickupItem : MonoBehaviour
{
    [Header("Item")]
    public ItemData itemData;
    public int quantity = 1;

    [Header("Settings")]
    public GameObject interactPrompt;
    public bool pickUpOnce = true;

    private bool isPlayerNearby;
    private bool alreadyPickedUp;

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactPrompt != null && !alreadyPickedUp)
                interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (alreadyPickedUp || !isPlayerNearby) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            TryPickUp();
        }
    }

    void TryPickUp()
    {
        if (itemData == null)
        {
            Debug.LogError($"[{name}] PickupItem.itemData is not assigned!");
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"[{name}] No Inventory found in scene!");
            return;
        }

        bool added = inventory.AddItem(itemData, quantity);
        if (added)
        {
            Debug.Log($"Picked up: {itemData.itemName} x{quantity}");
            alreadyPickedUp = true;

            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[{name}] Inventory full! Cannot pick up {itemData.itemName}.");
        }
    }
}
