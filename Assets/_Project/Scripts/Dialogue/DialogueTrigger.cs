using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueChoice
{
    [TextArea] public string choiceText;
    [TextArea] public string[] responseLines;
    [Header("Sub-choices (shown after responseLines, if any)")]
    [SerializeReference] public DialogueChoice[] subChoices;
    [Header("Quest")]
    [Tooltip("If true, selecting this choice starts the associated quest")]
    public bool startsQuest;
    [Header("Trash Emanator")]
    [Tooltip("If true, selecting this choice opens the Trash Emanator UI")]
    public bool opensTrashEmanator;
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

    [Header("Post-Quest Appearance")]
    [Tooltip("Face portrait shown in dialogue after quest is completed")]
    public Sprite npcFaceAfterQuest;
    [Tooltip("Animated face frames after quest (overrides npcFaceAfterQuest)")]
    public Sprite[] npcFaceFramesAfterQuest;
    [Tooltip("Animator controller to switch to after quest completion")]
    public RuntimeAnimatorController animatorAfterQuest;
    [Tooltip("Static sprite to use after quest if no animator is assigned")]
    public Sprite spriteAfterQuest;

    [Header("Settings")]
    public GameObject interactPrompt;
    public ShopNPC shopNPC;
    public bool openShopOnChoice0 = true;
    public TrashEmanatorUI trashEmanatorUI;

    [Header("Quest (optional)")]
    public ItemData requiredItem;
    public int requiredAmount = 1;
    public int rewardGold = 50;
    public int rewardExperience = 100;
    [TextArea] public string[] questCompleteLines;
    [TextArea] public string[] questProgressLines;
    [TextArea] public string[] questAlreadyDoneLines;

    private bool questCompleted;
    private bool questStarted;
    private NPCAudio npcAudio;

    void Start()
    {
        npcAudio = GetComponent<NPCAudio>();
        if (npcAudio == null)
            npcAudio = GetComponentInChildren<NPCAudio>();
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

    public void StartDialogue()
    {
        if (DialogueSystem.Instance == null) return;

        if (npcAudio != null)
            npcAudio.PlayDialogueStart();

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
            ShowNPCDialogue(CombineLines(dialogueLines, questAlreadyDoneLines));
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory != null && stats != null && requiredItem != null)
        {
            int count = inventory.GetItemCount(requiredItem);

            if (count >= requiredAmount)
            {
                inventory.RemoveItem(requiredItem, requiredAmount);
                stats.gold += rewardGold;
                stats.AddReward(rewardExperience, 0);
                questCompleted = true;
                SwapToPostQuestAppearance();

                if (npcAudio != null)
                    npcAudio.PlayQuestComplete();

                string[] rewardLines = AppendLine(questCompleteLines, $"+{rewardGold} gold\n+{rewardExperience} XP");

                ShowNPCDialogue(CombineLines(dialogueLines, rewardLines));
                return;
            }

            if (count > 0)
            {
                questStarted = true;
                string[] progressStateLines = AppendLine(questProgressLines, $": {count}/{requiredAmount} {requiredItem.itemName}");

                ShowNPCDialogue(CombineLines(dialogueLines, progressStateLines));
                return;
            }
        }

        if (questStarted)
        {
            string itemName = requiredItem != null ? requiredItem.itemName : "item";
            string[] progressStateLines = AppendLine(questProgressLines, $": 0/{requiredAmount} {itemName}");
            ShowNPCDialogue(CombineLines(dialogueLines, progressStateLines));
            return;
        }

        if (hasChoices && choices.Length > 0)
            ShowNPCDialogueWithChoices(dialogueLines, choices, OnChoiceMade);
        else
            ShowNPCDialogue(dialogueLines);
    }

    string[] AppendLine(string[] lines, string line)
    {
        int length = lines != null ? lines.Length : 0;
        string[] result = new string[length + 1];
        if (length > 0)
            lines.CopyTo(result, 0);
        result[result.Length - 1] = line;
        return result;
    }

    string[] CombineLines(string[] baseLines, string[] stateLines)
    {
        int baseLength = baseLines != null ? baseLines.Length : 0;
        int stateLength = stateLines != null ? stateLines.Length : 0;
        string[] lines = new string[baseLength + stateLength];
        if (baseLength > 0)
            baseLines.CopyTo(lines, 0);
        if (stateLength > 0)
            stateLines.CopyTo(lines, baseLength);
        return lines;
    }

    void SwapToPostQuestAppearance()
    {
        Animator animator = GetComponent<Animator>();
        if (animatorAfterQuest != null && animator != null)
        {
            animator.runtimeAnimatorController = animatorAfterQuest;
        }
        else if (spriteAfterQuest != null)
        {
            if (animator != null)
                animator.runtimeAnimatorController = null;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = spriteAfterQuest;
        }
    }

    void ShowNPCDialogue(string[] lines)
    {
        Sprite face = npcFace;
        Sprite[] faceFrames = npcFaceFrames;

        if (questCompleted)
        {
            if (npcFaceFramesAfterQuest != null && npcFaceFramesAfterQuest.Length > 0)
                faceFrames = npcFaceFramesAfterQuest;
            else if (npcFaceAfterQuest != null)
            {
                faceFrames = null;
                face = npcFaceAfterQuest;
            }
        }

        if (faceFrames != null && faceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogue(lines, npcName, faceFrames);
        else
            DialogueSystem.Instance.ShowDialogue(lines, npcName, face);
    }

    void ShowNPCDialogueWithChoices(string[] lines, DialogueChoice[] choices, System.Action<int, DialogueChoice> callback)
    {
        Sprite face = npcFace;
        Sprite[] faceFrames = npcFaceFrames;

        if (questCompleted)
        {
            if (npcFaceFramesAfterQuest != null && npcFaceFramesAfterQuest.Length > 0)
                faceFrames = npcFaceFramesAfterQuest;
            else if (npcFaceAfterQuest != null)
            {
                faceFrames = null;
                face = npcFaceAfterQuest;
            }
        }

        if (faceFrames != null && faceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogueWithChoiceTree(lines, choices, npcName, faceFrames, callback);
        else
            DialogueSystem.Instance.ShowDialogueWithChoiceTree(lines, choices, npcName, face, callback);
    }

    void OnChoiceMade(int choiceIndex, DialogueChoice selected)
    {
        Debug.Log($"Player chose option {choiceIndex}: {selected?.choiceText}");

        if (selected != null && selected.startsQuest)
        {
            questStarted = true;
            Debug.Log($"[{name}] Quest started via dialogue choice.");
        }

        if (choiceIndex == 0 && openShopOnChoice0 && shopNPC != null)
        {
            shopNPC.OpenShop();
        }

        if (selected != null && selected.opensTrashEmanator)
        {
            DialogueSystem.Instance?.CloseDialogue();
            if (trashEmanatorUI == null)
                trashEmanatorUI = FindAnyObjectByType<TrashEmanatorUI>();
            if (trashEmanatorUI != null)
                trashEmanatorUI.Open();
            else
                Debug.LogWarning($"[{name}] opensTrashEmanator selected but trashEmanatorUI is not found.");
        }
    }

    public bool GetQuestStarted() => questStarted;
    public bool GetQuestCompleted() => questCompleted;

    public void SetQuestStarted(bool value)
    {
        questStarted = value;
    }

    public void SetQuestCompleted(bool value)
    {
        if (questCompleted == value) return;
        questCompleted = value;
        if (questCompleted)
            SwapToPostQuestAppearance();
    }
}
