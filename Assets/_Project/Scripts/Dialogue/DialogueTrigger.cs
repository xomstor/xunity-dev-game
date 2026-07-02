using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueChoice
{
    [TextArea] public string choiceText;
    [TextArea] public string[] responseLines;
    [Header("Sub-choices (shown after responseLines, if any)")]
    [SerializeReference] public DialogueChoice[] subChoices;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [TextArea] public string[] dialogueLines;
    public bool hasChoices;
    public DialogueChoice[] choices;

    [Header("NPC Info")]
    public string npcName = "NPC";
    public Sprite npcFace;

    [Header("Settings")]
    public GameObject interactPrompt;
    public ShopNPC shopNPC;
    public bool openShopOnChoice0 = true;

    [Header("Quest (optional)")]
    public ItemData requiredItem;
    public int requiredAmount = 1;
    public int rewardGold = 50;
    public int rewardExperience = 100;
    [TextArea] public string[] questCompleteLines;
    [TextArea] public string[] questProgressLines;
    [TextArea] public string[] questAlreadyDoneLines;

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
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        if (DialogueSystem.Instance == null) return;

        if (requiredItem != null)
        {
            HandleQuestDialogue();
            return;
        }

        if (hasChoices && choices.Length > 0)
        {
            DialogueSystem.Instance.ShowDialogueWithChoiceTree(dialogueLines, choices, npcName, npcFace, OnChoiceMade);
        }
        else
        {
            DialogueSystem.Instance.ShowDialogue(dialogueLines, npcName, npcFace);
        }
    }

    void HandleQuestDialogue()
    {
        if (questCompleted)
        {
            DialogueSystem.Instance.ShowDialogue(questAlreadyDoneLines, npcName, npcFace);
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory == null || stats == null)
        {
            DialogueSystem.Instance.ShowDialogue(dialogueLines, npcName, npcFace);
            return;
        }

        int count = inventory.GetItemCount(requiredItem);

        if (count >= requiredAmount)
        {
            inventory.RemoveItem(requiredItem, requiredAmount);
            stats.gold += rewardGold;
            stats.AddReward(rewardExperience, 0);
            questCompleted = true;

            string[] lines = new string[questCompleteLines.Length + 1];
            questCompleteLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $"+{rewardGold} gold\n+{rewardExperience} XP";

            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFace);
        }
        else if (questStarted)
        {
            string[] lines = new string[questProgressLines.Length + 1];
            questProgressLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $": {count}/{requiredAmount} {requiredItem.itemName}";

            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFace);
        }
        else
        {
            questStarted = true;
            if (hasChoices && choices.Length > 0)
                DialogueSystem.Instance.ShowDialogueWithChoiceTree(dialogueLines, choices, npcName, npcFace, OnChoiceMade);
            else
                DialogueSystem.Instance.ShowDialogue(dialogueLines, npcName, npcFace);
        }
    }

    void OnChoiceMade(int choiceIndex)
    {
        Debug.Log($"Player chose option {choiceIndex}");
        if (choiceIndex == 0 && openShopOnChoice0)
        {
            if (shopNPC != null)
                shopNPC.OpenShop();
            else
                Debug.LogError($"[{name}] DialogueTrigger.shopNPC is not assigned. Cannot open shop.");
        }
    }
}
