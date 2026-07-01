using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    [System.Serializable]
    public class EnemyState
    {
        public string enemyId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private Dictionary<string, List<EnemyState>> sceneEnemies = new Dictionary<string, List<EnemyState>>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RespawnAllEnemies();
    }

    public void RegisterDeath(AutoCombat enemy)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneEnemies.ContainsKey(sceneName))
            sceneEnemies[sceneName] = new List<EnemyState>();

        sceneEnemies[sceneName].Add(new EnemyState
        {
            enemyId = enemy.name,
            position = enemy.transform.position,
            rotation = enemy.transform.rotation,
            scale = enemy.transform.localScale
        });
    }

    public void RespawnAllEnemies()
    {
        AutoCombat[] enemies = FindObjectsByType<AutoCombat>(FindObjectsInactive.Exclude);
        foreach (AutoCombat enemy in enemies)
        {
            if (enemy.team != CombatTeam.Enemy) continue;
            RespawnEnemy(enemy);
        }
    }

    void RespawnEnemy(AutoCombat enemy)
    {
        if (!enemy.IsDead) return;

        enemy.enabled = false;

        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        enemy.ResetHealth();
        enemy.enabled = true;
    }
}
