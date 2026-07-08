using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSingleton : MonoBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
        SpawnPoint target = null;
        float nearestDist = float.MaxValue;

        foreach (SpawnPoint sp in spawnPoints)
        {
            float dist = Vector3.Distance(transform.position, sp.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                target = sp;
            }
        }

        if (target != null)
            transform.position = target.transform.position;
    }
}
