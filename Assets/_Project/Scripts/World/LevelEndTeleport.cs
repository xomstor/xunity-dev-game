using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTeleport : MonoBehaviour
{
    [Header("Teleport")]
    public string hubSceneName = "Hub";
    public string nextLevelSceneName = "";
    public string targetTag = "Player";

    [Header("Dialogue")]
    public string[] dialogueLines = new string[]
    {
        "Вы достигли конца уровня.",
        "Куда вы хотите отправиться?"
    };
    public string choiceReturnText = "Вернуться в хаб";
    public string choiceContinueText = "Продолжить";
    public string npcName = "Портал";
    public Sprite npcFace;

    private bool isPlayerInTrigger;
    private bool dialogShown;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag) && !dialogShown)
        {
            isPlayerInTrigger = true;
            ShowChoiceDialogue();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            isPlayerInTrigger = false;
            dialogShown = false;
        }
    }

    void ShowChoiceDialogue()
    {
        dialogShown = true;

        if (DialogueSystem.Instance == null) return;

        string continueText = string.IsNullOrEmpty(nextLevelSceneName) ? choiceContinueText : $"{choiceContinueText} ({nextLevelSceneName})";

        string[] choiceTexts = new string[] { choiceReturnText, continueText };
        string[][] responses = new string[][]
        {
            new string[] { "Отправляемся в хаб..." },
            new string[] { "Продолжаем путь..." }
        };

        DialogueSystem.Instance.ShowDialogueWithChoices(dialogueLines, choiceTexts, responses, npcName, npcFace, OnChoiceMade);
    }

    void OnChoiceMade(int choiceIndex)
    {
        if (choiceIndex == 0)
        {
            LoadHub();
        }
        else if (choiceIndex == 1)
        {
            ContinueLevel();
        }
    }

    void LoadHub()
    {
        if (!string.IsNullOrEmpty(hubSceneName))
            SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
    }

    void ContinueLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelSceneName))
            SceneManager.LoadScene(nextLevelSceneName, LoadSceneMode.Single);
        else
            Debug.LogWarning("LevelEndTeleport: nextLevelSceneName is not set");
    }
}
