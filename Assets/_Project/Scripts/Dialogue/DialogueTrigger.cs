using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueChoice
{
    [TextArea] public string choiceText;
    [TextArea] public string[] responseLines;
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

    private bool isPlayerNearby;

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

        if (hasChoices && choices.Length > 0)
        {
            string[] choiceTexts = new string[choices.Length];
            string[][] responses = new string[choices.Length][];
            for (int i = 0; i < choices.Length; i++)
            {
                choiceTexts[i] = choices[i].choiceText;
                responses[i] = choices[i].responseLines;
            }
            DialogueSystem.Instance.ShowDialogueWithChoices(dialogueLines, choiceTexts, responses, npcName, npcFace, OnChoiceMade);
        }
        else
        {
            DialogueSystem.Instance.ShowDialogue(dialogueLines, npcName, npcFace);
        }
    }

    void OnChoiceMade(int choiceIndex)
    {
        Debug.Log($"Player chose option {choiceIndex}");
        if (choiceIndex == 0)
        {
            if (shopNPC != null)
                shopNPC.OpenShop();
            else
                Debug.LogError($"[{name}] DialogueTrigger.shopNPC is not assigned. Cannot open shop.");
        }
    }
}
