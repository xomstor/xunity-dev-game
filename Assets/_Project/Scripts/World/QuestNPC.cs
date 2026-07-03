using UnityEngine;
using UnityEngine.InputSystem;

public class QuestNPC : MonoBehaviour
{
    [Header("Quest")]
    public ItemData requiredItem;
    public int requiredAmount = 6;
    public int rewardGold = 50;
    public int rewardExperience = 100;

    [Header("NPC Info")]
    public string npcName = "NPC";
    public Sprite npcFace;
    [Tooltip("Animated frames for NPC face (overrides npcFace if assigned)")]
    public Sprite[] npcFaceFrames;

    [Header("Dialogue Lines")]
    [TextArea] public string[] introLines;
    [TextArea] public string[] progressLines;
    [TextArea] public string[] completeLines;
    [TextArea] public string[] alreadyDoneLines;

    [Header("UI")]
    public GameObject interactPrompt;

    private bool isPlayerNearby;
    private bool questCompleted;
    private bool questStarted;

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
            TryStartQuestDialogue();
        }
    }

    void TryStartQuestDialogue()
    {
        if (DialogueSystem.Instance == null)
        {
            Debug.LogWarning($"[{name}] DialogueSystem.Instance is null — using fallback.");
            TryCompleteQuestFallback();
            return;
        }

        if (questCompleted)
        {
            ShowNPCDialogue(alreadyDoneLines);
            return;
        }

        if (!questStarted)
        {
            questStarted = true;
            ShowNPCDialogue(introLines);
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory == null || stats == null || requiredItem == null)
        {
            ShowNPCDialogue(
                new string[] { "Something is wrong... come back later." });
            return;
        }

        int count = inventory.GetItemCount(requiredItem);

        if (count >= requiredAmount)
        {
            string[] lines = new string[completeLines.Length + 1];
            completeLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $"+{rewardGold} gold\n+{rewardExperience} XP";

            inventory.RemoveItem(requiredItem, requiredAmount);
            stats.gold += rewardGold;
            stats.AddReward(rewardExperience, 0);
            questCompleted = true;

            ShowNPCDialogue(lines);
        }
        else
        {
            string[] lines = new string[progressLines.Length + 1];
            progressLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $"You have: {count}/{requiredAmount} {requiredItem.itemName}";

            ShowNPCDialogue(lines);
        }
    }

    void ShowNPCDialogue(string[] lines)
    {
        if (npcFaceFrames != null && npcFaceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFaceFrames);
        else
            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFace);
    }

    void TryCompleteQuestFallback()
    {
        if (questCompleted)
        {
            Debug.Log("Quest already completed.");
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory == null || stats == null || requiredItem == null)
        {
            Debug.Log("Cannot complete quest now.");
            return;
        }

        int count = inventory.GetItemCount(requiredItem);
        if (count >= requiredAmount)
        {
            inventory.RemoveItem(requiredItem, requiredAmount);
            stats.gold += rewardGold;
            stats.AddReward(rewardExperience, 0);
            questCompleted = true;
            Debug.Log($"Quest completed! +{rewardGold} gold, +{rewardExperience} XP");
        }
        else
        {
            Debug.Log($"Bring me {requiredAmount} {requiredItem.itemName}. You have: {count}/{requiredAmount}");
        }
    }
}
