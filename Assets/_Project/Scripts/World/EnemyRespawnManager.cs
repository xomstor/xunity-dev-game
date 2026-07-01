using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    [Header("Respawn Settings")]
    public float respawnDelay = 8f;

    [System.Serializable]
    public class EnemyState
    {
        public string enemyId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float deathTime;
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

    void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneEnemies.ContainsKey(sceneName)) return;

        List<EnemyState> states = sceneEnemies[sceneName];
        for (int i = states.Count - 1; i >= 0; i--)
        {
            if (Time.time - states[i].deathTime >= respawnDelay)
            {
                AutoCombat enemy = FindEnemyByName(states[i].enemyId);
                if (enemy != null && enemy.IsDead)
                {
                    RespawnEnemy(enemy);
                }
                states.RemoveAt(i);
            }
        }

        if (sceneEnemies[sceneName].Count == 0)
            sceneEnemies.Remove(sceneName);
    }

    AutoCombat FindEnemyByName(string enemyName)
    {
        AutoCombat[] enemies = FindObjectsByType<AutoCombat>(FindObjectsInactive.Include);
        foreach (AutoCombat enemy in enemies)
        {
            if (enemy.name == enemyName && enemy.team == CombatTeam.Enemy)
                return enemy;
        }
        return null;
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
            scale = enemy.transform.localScale,
            deathTime = Time.time
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

        Animator animator = enemy.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = true;

        SpriteRenderer[] sprites = enemy.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            if (sr != null)
                sr.enabled = true;
        }
    }
}
