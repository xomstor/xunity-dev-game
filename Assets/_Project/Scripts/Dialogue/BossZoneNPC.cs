using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class BossZoneNPC : MonoBehaviour
{
    [Header("Dialogue Data")]
    public BossDialogueData data;

    [Header("Interaction")]
    public GameObject interactPrompt;

    [Header("NPC Tags")]
    public string targetTag = "Player";

    private bool dialoguePlayed;
    private BoxCollider2D zoneCollider;

    private static string pendingSpawnPointName;

    void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    public void StartDialogue()
    {
        if (data == null)
        {
            Debug.LogError($"[{name}] BossZoneNPC: BossDialogueData is not assigned!");
            return;
        }

        if (data.playOnlyOnce && dialoguePlayed)
            return;

        FreezePlayer();
        dialoguePlayed = true;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogError($"[{name}] BossZoneNPC: DialogueSystem is not found!");
            return;
        }

        if (data.npcFaceFrames != null && data.npcFaceFrames.Length > 0)
            DialogueSystem.Instance.ShowDialogue(data.dialogueLines, data.npcName, data.npcFaceFrames);
        else
            DialogueSystem.Instance.ShowDialogue(data.dialogueLines, data.npcName, data.npcFace);

        StartCoroutine(WaitForDialogueThenTeleport());
    }

    void FreezePlayer()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.ResetInputAndVelocity();
            pc.LockInput(0.5f);
        }
        VirtualJoystick joystick = FindAnyObjectByType<VirtualJoystick>();
        joystick?.ForceReset();
    }

    IEnumerator WaitForDialogueThenTeleport()
    {
        yield return new WaitUntil(() => !DialogueSystem.IsDialogueActive);
        Teleport();
    }

    void Teleport()
    {
        SaveManager.Instance?.AutoSave();

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        pc?.ResetInputAndVelocity();
        pc?.LockInput(2f);

        VirtualJoystick joystick = FindAnyObjectByType<VirtualJoystick>();
        joystick?.ForceReset();

        string capturedScene = data.bossSceneName;
        string capturedSpawnPoint = data.bossSpawnPointName;

        TeleportEffect.Play(
            () =>
            {
                pendingSpawnPointName = capturedSpawnPoint;
                SceneManager.sceneLoaded += OnBossSceneLoaded;
                SceneManager.LoadScene(capturedScene);
            },
            null);
    }

    static void OnBossSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnBossSceneLoaded;

        GameObject spawnPoint = FindSpawnPointObject(pendingSpawnPointName);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (spawnPoint != null && player != null)
            player.transform.position = spawnPoint.transform.position;
        else
            Debug.LogError($"[BossZoneNPC] Spawn failed. SpawnPoint='{pendingSpawnPointName}' found={spawnPoint != null}, player found={player != null}");

        PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null)
        {
            pc.UnlockInput();
            pc.ResetInputAndVelocity();
        }
        VirtualJoystick joystick = FindAnyObjectByType<VirtualJoystick>();
        joystick?.ForceReset();

        pendingSpawnPointName = null;
    }

    static GameObject FindSpawnPointObject(string spawnPointName)
    {
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            GameObject byName = GameObject.Find(spawnPointName);
            if (byName != null)
                return byName;
        }

        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>();
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp != null && (string.IsNullOrEmpty(spawnPointName) || sp.spawnPointName == spawnPointName))
                return sp.gameObject;
        }

        return null;
    }
}
