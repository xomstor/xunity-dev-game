using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrashEmanatorSpawnManager : MonoBehaviour
{
    public static TrashEmanatorSpawnManager Instance { get; private set; }

    [Header("Spawn")]
    [Tooltip("Префаб NPC Эманатора мусорных баков")]
    public GameObject emanatorPrefab;
    [Tooltip("Точка спавна в хабе")]
    public Transform spawnPoint;
    [Tooltip("Сцена хаба (оставь пустым для автоматического поиска по имени)")]
    public string hubSceneName = "Hub";

    public bool IsSpawned { get; private set; }
    public TrashEmanator CurrentEmanator { get; private set; }

    private List<TrashedItem> pendingItems = new List<TrashedItem>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("TrashEmanatorSpawnManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TrashEmanatorSpawnManager>();
        }
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

        Inventory.OnItemTrashed += OnItemTrashed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        Inventory.OnItemTrashed -= OnItemTrashed;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSpawned)
        {
            TrySpawn();
        }
    }

    void OnItemTrashed(InventoryItem item)
    {
        pendingItems.Add(new TrashedItem
        {
            itemId = item.itemData != null ? item.itemData.itemId : null,
            itemName = item.itemData != null ? item.itemData.itemName : "Unknown",
            quantity = item.quantity,
            trashedTime = System.DateTime.Now.ToString("O")
        });

        if (IsSpawned)
        {
            FlushPendingItems();
            return;
        }
        TrySpawn();
    }

    public void TrySpawn()
    {
        if (IsSpawned) return;
        if (string.IsNullOrEmpty(hubSceneName))
        {
            hubSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != hubSceneName)
        {
            Debug.Log($"[TrashEmanatorSpawnManager] Не хаб ({hubSceneName}), откладываем спавн.");
            return;
        }
        if (emanatorPrefab == null)
        {
            Debug.LogWarning("[TrashEmanatorSpawnManager] Префаб Эманатора не назначен.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(emanatorPrefab, pos, Quaternion.identity);
        go.name = "EmanatorOfTrashcans";
        CurrentEmanator = go.GetComponent<TrashEmanator>();
        IsSpawned = true;
        FlushPendingItems();
        Debug.Log("[TrashEmanatorSpawnManager] Эманатор мусорных баков заспавнен.");
    }

    void FlushPendingItems()
    {
        if (CurrentEmanator == null) return;
        foreach (var item in pendingItems)
        {
            ItemData data = CurrentEmanator.FindItemById(item.itemId);
            if (data != null)
                CurrentEmanator.AddItem(data, item.quantity);
            else
                CurrentEmanator.trashedItems.Add(item);
        }
        pendingItems.Clear();
    }

    public List<TrashedItem> GetPendingItems() => pendingItems;
}
