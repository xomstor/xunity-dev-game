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
        // ❌ АВТО-РЕСПАВН ОТКЛЮЧЕН!
        // Теперь только ручной через Teleport2D или вызов RespawnAllEnemies()
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
        int respawnedCount = 0;
        int skippedCount = 0;

        AutoCombat[] enemies = FindObjectsByType<AutoCombat>(FindObjectsInactive.Exclude);
        foreach (AutoCombat enemy in enemies)
        {
            if (enemy.team != CombatTeam.Enemy) continue;

            if (!enemy.IsDead)
            {
                skippedCount++;
                continue;
            }

            RespawnEnemy(enemy);
            respawnedCount++;
        }

        Debug.Log($"🔄 RespawnAllEnemies: {respawnedCount} респавнено, {skippedCount} пропущено");
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

        // ✅✅✅ ВОССТАНАВЛИВАЕМ HP BAR! ✅✅✅
        RestoreHealthBarForEnemy(enemy);

        Debug.Log($"✅ {enemy.name} респавнён!");
    }

    // ✅ НОВЫЙ МЕТОД: Включаем HP bar обратно!
    void RestoreHealthBarForEnemy(AutoCombat enemy)
    {
        // Ищем все HP bars (включая неактивные!)
        HealthBarUI[] allBars = FindObjectsByType<HealthBarUI>(FindObjectsInactive.Include); // ✅
        foreach (var bar in allBars)
        {
            if (bar.target == enemy)
            {
                // Включаем gameObject обратно!
                bar.gameObject.SetActive(true);

                Debug.Log($"   💚 HP Bar для {enemy.name} восстановлен!");
                return; // Нашли - выходим
            }
        }

        // Если бар не найден (мог быть уничтожен) - пробуем через Spawner
        if (AutoHealthBarSpawner.Instance != null)
        {
            AutoHealthBarSpawner.Instance.SpawnBarForEnemy(enemy);
            Debug.Log($"   🔨 HP Bar для {enemy.name} создан заново через Spawner");
        }
    }
}