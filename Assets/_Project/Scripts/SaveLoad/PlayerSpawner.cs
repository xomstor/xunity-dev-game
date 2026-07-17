using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    public static event System.Action OnPlayerReady;
    public static bool IsPlayerReady { get; private set; }

    [Tooltip("Assign Player.prefab here")]
    public GameObject playerPrefab;

    [Tooltip("Default spawn position when no save data exists")]
    public Vector3 defaultSpawnPosition = Vector3.zero;

    private static bool _readyInvokedThisScene;
    private Coroutine _restoreRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        GetOrCreateInstance();
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
        TryLoadPrefab();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TryLoadPrefab()
    {
        if (playerPrefab != null) return;
        playerPrefab = Resources.Load<GameObject>("Player");
    }

    public static PlayerSpawner GetOrCreateInstance()
    {
        if (Instance != null) return Instance;
        PlayerSpawner existing = FindAnyObjectByType<PlayerSpawner>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("PlayerSpawner");
        DontDestroyOnLoad(go);
        PlayerSpawner spawner = go.AddComponent<PlayerSpawner>();
        spawner.TryLoadPrefab();
        return Instance = spawner;
    }

    public static void ResetReady()
    {
        IsPlayerReady = false;
        _readyInvokedThisScene = false;
    }

    public static void NotifyPlayerReady()
    {
        IsPlayerReady = true;
        if (_readyInvokedThisScene) return;
        _readyInvokedThisScene = true;
        OnPlayerReady?.Invoke();
    }

    public GameObject EnsurePlayerExists()
    {
        PlayerStats existing = FindAnyObjectByType<PlayerStats>();
        if (existing != null) return existing.gameObject;

        if (playerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawner] playerPrefab not assigned. Assign Player.prefab in inspector or put it in Resources/Player.prefab.");
            return null;
        }

        GameObject player = Instantiate(playerPrefab, defaultSpawnPosition, Quaternion.identity);
        player.name = "Player";
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) rb = player.GetComponentInChildren<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Debug.Log($"[PlayerSpawner] Spawned player at {defaultSpawnPosition}");
        return player;
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
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) rb = player.GetComponentInChildren<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Debug.Log($"[PlayerSpawner] Spawned player at {position}");
        return player;
    }

    public void SnapToSpawnPoint(GameObject player, Vector3? overridePosition = null)
    {
        Vector3 targetPos = overridePosition ?? GetHubSpawnPosition() ?? defaultSpawnPosition;

        // Drop to nearest ground if the spawn point is floating above a collider
        RaycastHit2D hit = Physics2D.Raycast(targetPos, Vector2.down, 100f, Physics2D.DefaultRaycastLayers, 0f);
        if (hit.collider != null)
        {
            // Place the player on top of the ground, not intersecting it
            float halfHeight = 0f;
            BoxCollider2D col = player.GetComponent<BoxCollider2D>();
            if (col != null)
                halfHeight = (col.size.y * 0.5f - col.offset.y) * col.transform.lossyScale.y;
            targetPos = new Vector3(hit.point.x, hit.point.y + halfHeight + 0.05f, targetPos.z);
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ResetInputAndVelocity();
            pc.LockInput(0.1f);
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) rb = player.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.position = targetPos;
            rb.transform.position = targetPos;
            _restoreRoutine = StartCoroutine(RestoreDynamicRB(rb, targetPos));
        }
        else
        {
            player.transform.position = targetPos;
        }

        Debug.Log($"[PlayerSpawner] Snapped to {targetPos}, RestoreDynamicRB will resume at {targetPos}");
    }

    Vector3? GetHubSpawnPosition()
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);
        if (spawnPoints.Length == 0) return null;

        SpawnPoint hub = System.Array.Find(spawnPoints, sp => sp != null && sp.isHub);
        SpawnPoint target = hub != null ? hub : spawnPoints[0];
        return target.transform.position;
    }

    IEnumerator RestoreDynamicRB(Rigidbody2D rb, Vector3 expectedPos)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.position = expectedPos;
            rb.transform.position = expectedPos;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            Debug.Log($"[PlayerSpawner] RestoreDynamicRB: pos={rb.position}, expected={expectedPos}");
        }
    }
}
