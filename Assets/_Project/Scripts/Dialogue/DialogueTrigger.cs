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
    [Tooltip("Animated frames for NPC face (overrides npcFace if assigned)")]
    public Sprite[] npcFaceFrames;

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
            ShowNPCDialogueWithChoices(dialogueLines, choices, OnChoiceMade);
        }
        else
        {
            ShowNPCDialogue(dialogueLines);
        }
    }

    void HandleQuestDialogue()
    {
        if (questCompleted)
        {
            ShowNPCDialogue(questAlreadyDoneLines);
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory == null || stats == null)
        {
            ShowNPCDialogue(dialogueLines);
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

            ShowNPCDialogue(lines);
        }
        else if (questStarted)
        {
            string[] lines = new string[questProgressLines.Length + 1];
            questProgressLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $": {count}/{requiredAmount} {requiredItem.itemName}";

            ShowNPCDialogue(lines);
        }
        else
        {
            questStarted = true;
            if (hasChoices && choices.Length > 0)
                ShowNPCDialogueWithChoices(dialogueLines, choices, OnChoiceMade);
            else
                ShowNPCDialogue(dialogueLines);
        }
    }

    void ShowNPCDialogue(string[] lines)
    {
        if (npcFaceFrames != null && npcFaceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFaceFrames);
        else
            DialogueSystem.Instance.ShowDialogue(lines, npcName, npcFace);
    }

    void ShowNPCDialogueWithChoices(string[] lines, DialogueChoice[] choices, System.Action<int> callback)
    {
        if (npcFaceFrames != null && npcFaceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogueWithChoiceTree(lines, choices, npcName, npcFaceFrames, callback);
        else
            DialogueSystem.Instance.ShowDialogueWithChoiceTree(lines, choices, npcName, npcFace, callback);
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
