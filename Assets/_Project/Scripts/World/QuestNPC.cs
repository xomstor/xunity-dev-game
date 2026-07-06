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

    [Header("Post-Quest Appearance")]
    [Tooltip("Face portrait shown in dialogue after quest is completed")]
    public Sprite npcFaceAfterQuest;
    [Tooltip("Animated face frames after quest (overrides npcFaceAfterQuest)")]
    public Sprite[] npcFaceFramesAfterQuest;
    [Tooltip("Animator controller to switch to after quest completion")]
    public RuntimeAnimatorController animatorAfterQuest;
    [Tooltip("Static sprite to use after quest if no animator is assigned")]
    public Sprite spriteAfterQuest;

    [Header("Dialogue Lines")]
    [TextArea] public string[] introLines;
    [TextArea] public string[] progressLines;
    [TextArea] public string[] completeLines;
    [TextArea] public string[] alreadyDoneLines;

    [Header("UI")]
    public GameObject interactPrompt;

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
            ShowNPCDialogue(CombineLines(introLines, alreadyDoneLines));
            return;
        }

        Inventory inventory = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        if (inventory != null && stats != null && requiredItem != null)
        {
            int count = inventory.GetItemCount(requiredItem);
            string progressStatus = $"Принесено: {count}/{requiredAmount} {requiredItem.itemName}";

            if (count >= requiredAmount)
            {
                string[] rewardLines = AppendLine(completeLines, $"+{rewardGold} золота, +{rewardExperience} опыта");

                inventory.RemoveItem(requiredItem, requiredAmount);
                stats.gold += rewardGold;
                stats.AddReward(rewardExperience, 0);
                questCompleted = true;
                SwapToPostQuestAppearance();

                ShowNPCDialogue(CombineLines(introLines, rewardLines));
                return;
            }

            if (count > 0)
            {
                string[] progressStateLines = AppendLine(progressLines, progressStatus);
                ShowNPCDialogue(CombineLines(introLines, progressStateLines));
                return;
            }

            ShowNPCDialogue(AppendLine(introLines, progressStatus));
            return;
        }

        ShowNPCDialogue(introLines);
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
