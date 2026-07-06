using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Tooltip("Assign Player.prefab here")]
    public GameObject playerPrefab;

    [Tooltip("Default spawn position when no save data exists")]
    public Vector3 defaultSpawnPosition = Vector3.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("PlayerSpawner");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayerSpawner>();
        Instance.TryLoadPrefab();
        Instance.EnsurePlayerExists();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryLoadPrefab();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void TryLoadPrefab()
    {
        if (playerPrefab != null) return;
        playerPrefab = Resources.Load<GameObject>("Player");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerStats existing = FindAnyObjectByType<PlayerStats>();
        if (existing != null) return;

        GameObject player = SpawnPlayer(defaultSpawnPosition);
        if (player == null) return;

        if (SaveManager.Instance != null && SaveManager.Instance.HasSave(SaveManager.Instance.lastUsedSlot))
            SaveManager.Instance.LoadGame(SaveManager.Instance.lastUsedSlot);
    }

    public GameObject EnsurePlayerExists()
    {
        PlayerStats existing = FindAnyObjectByType<PlayerStats>();
        if (existing != null) return existing.gameObject;
        return SpawnPlayer(defaultSpawnPosition);
    }

    public GameObject SpawnPlayer(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawner] playerPrefab not assigned. Assign Player.prefab in inspector or put it in Resources/Player.prefab.");
            return null;
        }

        GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
        player.name = "Player";
        Debug.Log($"[PlayerSpawner] Spawned player at {position}");
        return player;
    }
}
