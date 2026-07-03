using UnityEngine;

public class LevelEndTeleport : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Имя объекта SpawnPoint в сцене для хаба")]
    public string hubSpawnPointName = "SpawnPoint_Hub";
    [Tooltip("Имя объекта SpawnPoint в сцене для следующего уровня")]
    public string nextLevelSpawnPointName = "SpawnPoint_Level2";
    public string targetTag = "Player";
    public float enterGracePeriod = 1f;
    public bool respawnEnemiesOnTeleport = true;

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
    [Tooltip("Animated frames for NPC face (overrides npcFace if assigned)")]
    public Sprite[] npcFaceFrames;

    private bool dialogShown;
    private float triggerTime;

    void Start()
    {
        triggerTime = 0f;
    }

    void Update()
    {
        triggerTime += Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag) && !dialogShown && triggerTime >= enterGracePeriod)
        {
            ShowChoiceDialogue();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            dialogShown = false;
        }
    }

    void ShowChoiceDialogue()
    {
        dialogShown = true;

        if (DialogueSystem.Instance == null) return;

        string continueText = string.IsNullOrEmpty(nextLevelSpawnPointName) ? choiceContinueText : choiceContinueText;

        string[] choiceTexts = new string[] { choiceReturnText, continueText };
        string[][] responses = new string[][]
        {
            new string[] { "Отправляемся в хаб..." },
            new string[] { "Продолжаем путь..." }
        };

        if (npcFaceFrames != null && npcFaceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogueWithChoices(dialogueLines, choiceTexts, responses, npcName, npcFaceFrames, OnChoiceMade);
        else
            DialogueSystem.Instance.ShowDialogueWithChoices(dialogueLines, choiceTexts, responses, npcName, npcFace, OnChoiceMade);
    }

    void OnChoiceMade(int choiceIndex)
    {
        if (choiceIndex == 0)
        {
            TeleportToSpawnPoint(hubSpawnPointName);
        }
        else if (choiceIndex == 1)
        {
            TeleportToSpawnPoint(nextLevelSpawnPointName);
        }
    }

    void TeleportToSpawnPoint(string spawnPointName)
    {
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint == null)
        {
            Debug.LogError($"LevelEndTeleport: SpawnPoint '{spawnPointName}' not found in scene!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player == null)
        {
            Debug.LogError("LevelEndTeleport: Player not found!");
            return;
        }

        player.transform.position = spawnPoint.transform.position;

        if (respawnEnemiesOnTeleport)
            EnemyRespawnManager.Instance?.RespawnAllEnemies();

        SpawnPoint spawnPointComponent = spawnPoint.GetComponent<SpawnPoint>();
        if (spawnPointComponent != null && spawnPointComponent.showLevelName)
            LevelNameDisplay.Instance?.Show(spawnPointComponent.displayName);

        Debug.Log($"Teleported player to {spawnPointName}");
    }
}
