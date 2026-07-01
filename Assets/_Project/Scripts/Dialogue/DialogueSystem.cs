using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI npcNameText;
    public Image npcFaceImage;
    public Button continueButton;
    public GameObject choicePanel;
    public Transform choiceButtonContainer;
    public Button choiceButtonPrefab;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private Coroutine currentDialogue;
    private bool continuePressed;
    private int choiceIndex = -1;
    private System.Action<int> onChoiceMade;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    public void ShowDialogue(string[] lines, string npcName = "", Sprite faceSprite = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);
        
        SetSpeakerInfo(npcName, faceSprite);
        currentDialogue = StartCoroutine(DisplayDialogue(lines));
    }

    public void ShowDialogueWithChoices(string[] lines, string[] choiceTexts, string[][] choiceResponses, string npcName = "", Sprite faceSprite = null, System.Action<int> callback = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);
        
        SetSpeakerInfo(npcName, faceSprite);
        onChoiceMade = callback;
        currentDialogue = StartCoroutine(DisplayDialogueWithChoices(lines, choiceTexts, choiceResponses));
    }

    void SetSpeakerInfo(string name, Sprite face)
    {
        if (npcNameText != null)
            npcNameText.text = name;
        if (npcFaceImage != null)
            npcFaceImage.sprite = face;
    }

    IEnumerator DisplayDialogue(string[] lines)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        foreach (string line in lines)
        {
            continuePressed = false;
            yield return TypeText(line);
            yield return new WaitUntil(() => continuePressed);
        }

        HideDialogue();
    }

    IEnumerator DisplayDialogueWithChoices(string[] lines, string[] choiceTexts, string[][] choiceResponses)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        foreach (string line in lines)
        {
            continuePressed = false;
            yield return TypeText(line);
            yield return new WaitUntil(() => continuePressed);
        }

        yield return ShowChoices(choiceTexts, choiceResponses);
    }

    void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        currentDialogue = null;
    }

    IEnumerator ShowChoices(string[] choiceTexts, string[][] choiceResponses)
    {
        choiceIndex = -1;
        if (choicePanel != null)
            choicePanel.SetActive(true);
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        ClearChoiceButtons();

        for (int i = 0; i < choiceTexts.Length; i++)
        {
            int index = i;
            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            button.GetComponentInChildren<TextMeshProUGUI>().text = choiceTexts[i];
            button.onClick.AddListener(() => OnChoiceSelected(index));
        }

        yield return new WaitUntil(() => choiceIndex >= 0);

        if (choicePanel != null)
            choicePanel.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        ClearChoiceButtons();

        if (choiceIndex < choiceResponses.Length)
        {
            foreach (string line in choiceResponses[choiceIndex])
            {
                continuePressed = false;
                yield return TypeText(line);
                yield return new WaitUntil(() => continuePressed);
            }
        }

        HideDialogue();

        onChoiceMade?.Invoke(choiceIndex);
    }

    void ClearChoiceButtons()
    {
        if (choiceButtonContainer == null) return;
        foreach (Transform child in choiceButtonContainer)
            Destroy(child.gameObject);
    }

    void OnChoiceSelected(int index)
    {
        choiceIndex = index;
    }

    IEnumerator TypeText(string text)
    {
        dialogueText.text = "";
        
        foreach (char letter in text.ToCharArray())
        {
            if (continuePressed)
            {
                dialogueText.text = text;
                break;
            }
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        continuePressed = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            OnContinuePressed();
    }

    public void OnContinuePressed()
    {
        continuePressed = true;
    }
}
