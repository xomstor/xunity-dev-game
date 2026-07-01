using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class QuestNPC : MonoBehaviour
{
    [Header("Quest")]
    public ItemData spiderTailItem;
    public int requiredAmount = 6;
    public int rewardGold = 50;
    public int rewardExperience = 100;

    [Header("UI")]
    public GameObject interactPrompt;
    public TextMeshProUGUI questText;

    private bool isPlayerNearby;
    private bool questCompleted;

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
            if (interactPrompt != null)
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
        var keyboard = Keyboard.current;
        if (isPlayerNearby && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            TryCompleteQuest();
        }
    }

    void TryCompleteQuest()
    {
        if (questCompleted)
        {
            ShowMessage("Quest already completed.");
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory == null || stats == null || spiderTailItem == null)
        {
            ShowMessage("Cannot complete quest now.");
            return;
        }

        int count = inventory.GetItemCount(spiderTailItem);
        if (count >= requiredAmount)
        {
            inventory.RemoveItem(spiderTailItem, requiredAmount);
            stats.gold += rewardGold;
            stats.AddReward(rewardExperience, 0);
            questCompleted = true;
            ShowMessage($"Quest completed!\n+{rewardGold} gold\n+{rewardExperience} XP");
        }
        else
        {
            ShowMessage($"Bring me {requiredAmount} spider tails.\nYou have: {count}/{requiredAmount}");
        }
    }

    void ShowMessage(string message)
    {
        if (questText != null)
            questText.text = message;
        else
            Debug.Log(message);
    }
}
