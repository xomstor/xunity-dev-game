using UnityEngine;

public class GameStatistics : MonoBehaviour
{
    public static GameStatistics Instance { get; private set; }

    [Header("Statistics")]
    public int steps = 0;  // Шаги = количество нажатий джойстика
    public float distanceTraveled = 0f;
    public int attacksPerformed = 0;
    public int enemiesKilled = 0;
    public int damageReceived = 0;
    public int goldSpent = 0;
    public int jumpsPerformed = 0;
    public int deathCount = 0;

    private Vector3 lastPosition;
    private PlayerController playerController;
    private PlayerStats playerStats;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("GameStatistics");
        go.AddComponent<GameStatistics>();
        Debug.Log("[GameStatistics] Auto-created singleton");
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
        Debug.Log("[GameStatistics] Initialized");
    }

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        playerStats = FindAnyObjectByType<PlayerStats>();
        
        if (playerController != null)
            lastPosition = playerController.transform.position;
    }

    void Update()
    {
        // Отслеживаем дистанцию
        if (playerController != null)
        {
            float distance = Vector3.Distance(lastPosition, playerController.transform.position);
            if (distance > 0.01f)
            {
                distanceTraveled += distance;
                lastPosition = playerController.transform.position;
            }
        }
    }

    public void RecordAttack()
    {
        attacksPerformed++;
        Debug.Log($"[GameStatistics] Attack recorded. Total: {attacksPerformed}");
    }

    public void RecordEnemyKilled()
    {
        enemiesKilled++;
        Debug.Log($"[GameStatistics] Enemy killed. Total: {enemiesKilled}");
    }

    public void RecordDamageReceived(int damage)
    {
        if (damage > 0)
        {
            damageReceived += damage;
            Debug.Log($"[GameStatistics] Damage received: {damage}. Total: {damageReceived}");
        }
    }

    public void RecordGoldSpent(int amount)
    {
        goldSpent += amount;
        Debug.Log($"[GameStatistics] Gold spent: {amount}. Total: {goldSpent}");
    }

    public void RecordJump()
    {
        jumpsPerformed++;
        Debug.Log($"[GameStatistics] Jump recorded. Total: {jumpsPerformed}");
    }

    public void RecordDeath()
    {
        deathCount++;
        Debug.Log($"[GameStatistics] Death recorded. Total: {deathCount}");
    }

    public void RecordStep()
    {
        steps++;
        Debug.Log($"[GameStatistics] Step recorded. Total: {steps}");
    }

    public void ResetStatistics()
    {
        steps = 0;
        distanceTraveled = 0f;
        attacksPerformed = 0;
        enemiesKilled = 0;
        damageReceived = 0;
        goldSpent = 0;
        jumpsPerformed = 0;
        deathCount = 0;
        Debug.Log("[GameStatistics] Statistics reset");
    }

    public string GetStatisticsText()
    {
        return $"СТАТИСТИКА\n\n" +
               $"Шаги: {steps}\n" +
               $"Дистанция: {distanceTraveled:F1} м\n" +
               $"Атак: {attacksPerformed}\n" +
               $"Убито мобов: {enemiesKilled}\n" +
               $"Получено урона: {damageReceived}\n" +
               $"Потрачено золота: {goldSpent}\n" +
               $"Прыжков: {jumpsPerformed}\n" +
               $"Смертей: {deathCount}";
    }
}
