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
    public Button continueButton;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private Coroutine currentDialogue;
    private bool isTyping;
    private bool continuePressed;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    public void ShowDialogue(string[] lines)
    {
        if (currentDialogue != null)
            StopCoroutine(currentDialogue);
        
        currentDialogue = StartCoroutine(DisplayDialogue(lines));
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

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        currentDialogue = null;
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
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
        
        isTyping = false;
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
