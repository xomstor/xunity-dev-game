using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }
    public static bool IsDialogueActive { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI npcNameText;
    public Image npcFaceImage;
    public Button continueButton;
    public Button closeButton;
    public GameObject choicePanel;
    public Transform choiceButtonContainer;
    public Button choiceButtonPrefab;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public float faceAnimationFPS = 8f;

    private Coroutine currentDialogue;
    private bool continuePressed;
    private int choiceIndex = -1;
    private System.Action<int, DialogueChoice> onChoiceMade;

    // Face animation state
    private Sprite[] activeFaceFrames;
    private int faceFrameIndex;
    private float faceTimer;

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

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDialogue);
    }

    public void ShowDialogue(string[] lines, string npcName = "", Sprite faceSprite = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceSprite);
        currentDialogue = StartCoroutine(DisplayDialogue(lines));
    }

    public void ShowDialogue(string[] lines, string npcName, Sprite[] faceFrames)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceFrames);
        currentDialogue = StartCoroutine(DisplayDialogue(lines));
    }

    public void ShowDialogueWithChoices(string[] lines, string[] choiceTexts, string[][] choiceResponses, string npcName = "", Sprite faceSprite = null, System.Action<int, DialogueChoice> callback = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceSprite);
        onChoiceMade = callback;
        currentDialogue = StartCoroutine(DisplayDialogueWithChoices(lines, choiceTexts, choiceResponses));
    }

    public void ShowDialogueWithChoices(string[] lines, string[] choiceTexts, string[][] choiceResponses, string npcName, Sprite[] faceFrames, System.Action<int, DialogueChoice> callback = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceFrames);
        onChoiceMade = callback;
        currentDialogue = StartCoroutine(DisplayDialogueWithChoices(lines, choiceTexts, choiceResponses));
    }

    public void ShowDialogueWithChoiceTree(string[] lines, DialogueChoice[] choices, string npcName = "", Sprite faceSprite = null, System.Action<int, DialogueChoice> callback = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceSprite);
        onChoiceMade = callback;
        currentDialogue = StartCoroutine(DisplayDialogueWithChoiceTree(lines, choices));
    }

    public void ShowDialogueWithChoiceTree(string[] lines, DialogueChoice[] choices, string npcName, Sprite[] faceFrames, System.Action<int, DialogueChoice> callback = null)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);

        IsDialogueActive = true;
        SetSpeakerInfo(npcName, faceFrames);
        onChoiceMade = callback;
        currentDialogue = StartCoroutine(DisplayDialogueWithChoiceTree(lines, choices));
    }

    void SetSpeakerInfo(string name, Sprite face)
    {
        if (npcNameText != null)
            npcNameText.text = name;

        activeFaceFrames = null;

        if (npcFaceImage != null)
        {
            npcFaceImage.sprite = face;
            npcFaceImage.enabled = face != null;
        }
    }

    void SetSpeakerInfo(string name, Sprite[] frames)
    {
        if (npcNameText != null)
            npcNameText.text = name;

        if (npcFaceImage != null && frames != null && frames.Length > 0)
        {
            activeFaceFrames = frames;
            faceFrameIndex = 0;
            faceTimer = 0f;
            npcFaceImage.sprite = frames[0];
            npcFaceImage.enabled = true;
        }
        else
        {
            activeFaceFrames = null;
            if (npcFaceImage != null)
                npcFaceImage.enabled = false;
        }
    }

    IEnumerator DisplayDialogue(string[] lines)
    {
        IsDialogueActive = true;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

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
        IsDialogueActive = true;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        foreach (string line in lines)
        {
            continuePressed = false;
            yield return TypeText(line);
            yield return new WaitUntil(() => continuePressed);
        }

        yield return ShowChoices(choiceTexts, choiceResponses);
    }

    IEnumerator DisplayDialogueWithChoiceTree(string[] lines, DialogueChoice[] choices)
    {
        IsDialogueActive = true;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        foreach (string line in lines)
        {
            continuePressed = false;
            yield return TypeText(line);
            yield return new WaitUntil(() => continuePressed);
        }

        yield return ShowChoiceTree(choices);
    }

    IEnumerator ShowChoiceTree(DialogueChoice[] choices)
    {
        DialogueChoice[] currentChoices = choices;

        while (currentChoices != null && currentChoices.Length > 0)
        {
            int validChoiceCount = 0;
            for (int i = 0; i < currentChoices.Length; i++)
            {
                if (currentChoices[i] != null)
                    validChoiceCount++;
            }

            if (validChoiceCount == 0)
                break;

            DialogueChoice[] validChoices = new DialogueChoice[validChoiceCount];
            string[] choiceTexts = new string[validChoiceCount];
            int validIndex = 0;
            for (int i = 0; i < currentChoices.Length; i++)
            {
                if (currentChoices[i] == null) continue;
                validChoices[validIndex] = currentChoices[i];
                choiceTexts[validIndex] = currentChoices[i].choiceText;
                validIndex++;
            }

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

            DialogueChoice selected = validChoices[choiceIndex];

            if (selected.responseLines != null)
            {
                foreach (string line in selected.responseLines)
                {
                    continuePressed = false;
                    yield return TypeText(line);
                    yield return new WaitUntil(() => continuePressed);
                }
            }

            if (selected.subChoices != null && selected.subChoices.Length > 0)
            {
                currentChoices = selected.subChoices;
            }
            else
            {
                onChoiceMade?.Invoke(choiceIndex, selected);
                currentChoices = null;
            }
        }

        HideDialogue();
    }

    void HideDialogue()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        ClearChoiceButtons();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        currentDialogue = null;
        activeFaceFrames = null;
        IsDialogueActive = false;
        SaveManager.Instance?.AutoSave();
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

        onChoiceMade?.Invoke(choiceIndex, null);
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
        if (dialogueText == null) yield break;

        if (text == null)
            text = "";

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

        // Animate NPC face frames
        if (activeFaceFrames != null && activeFaceFrames.Length > 1 && npcFaceImage != null)
        {
            faceTimer += Time.deltaTime;
            float interval = 1f / faceAnimationFPS;
            if (faceTimer >= interval)
            {
                faceTimer -= interval;
                faceFrameIndex = (faceFrameIndex + 1) % activeFaceFrames.Length;
                npcFaceImage.sprite = activeFaceFrames[faceFrameIndex];
            }
        }
    }

    public void OnContinuePressed()
    {
        continuePressed = true;
    }

    public void CloseDialogue()
    {
        if (currentDialogue != null)
        {
            StopCoroutine(currentDialogue);
            currentDialogue = null;
        }
        HideDialogue();
    }
}
