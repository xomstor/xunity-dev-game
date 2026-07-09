using UnityEngine;
public class LavaDamage : MonoBehaviour
{
    [Header("Урон")]
    public int damagePerTick = 20;          // Урон за одно "тик"
    public float tickInterval = 0.5f;       // Как часто наносить (сек)
    public bool instantKill = false;        // Мгновенная смерть?

    [Header("Респаун")]
    public Transform respawnPoint;          // Точка респауна (создайте пустой объект!)

    [Header("Эффекты (опционально)")]
    public ParticleSystem steamVFX;         // Пар/дым
    public AudioClip sizzleSFX;            // Звук шипения

    [Header("Движение лавы")]
    public bool enableLavaRise = true;      // Включить поднятие лавы?
    public float riseHeight = 3f;           // На сколько поднимается лава
    public float riseSpeed = 2f;            // Скорость поднятия
    public float stayUpDuration = 3f;       // Как долго лава остаётся поднятой
    public float fallSpeed = 1.5f;          // Скорость опускания
    public float cycleDuration = 8f;        // Полный цикл (поднятие + опускание)

    private float damageTimer;
    private GameObject playerInLava;
    private Vector3 startPosition;
    private float lavaTimer;
    private bool isRising;
    private bool isAtTop;

    void Start()
    {
        startPosition = transform.position;
        isRising = true;
        isAtTop = false;
        lavaTimer = 0f;
    }

    void Update()
    {
        // Движение лавы вверх и вниз
        if (enableLavaRise)
        {
            UpdateLavaPosition();
        }

        // Наносим периодический урон если игрок в лаве
        if (playerInLava != null && !instantKill)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= tickInterval)
            {
                ApplyDamageToPlayer(playerInLava);
                damageTimer = 0f;
            }
        }
    }

    void UpdateLavaPosition()
    {
        lavaTimer += Time.deltaTime;
        Vector3 currentPos = transform.position;
        float targetY = startPosition.y;

        if (isRising)
        {
            // Поднимаем лаву
            targetY = startPosition.y + riseHeight;
            currentPos.y = Mathf.Lerp(startPosition.y, targetY, lavaTimer * riseSpeed / riseHeight);
            transform.position = currentPos;

            // Проверяем достигли ли верхнюю точку
            if (currentPos.y >= targetY - 0.1f)
            {
                isRising = false;
                isAtTop = true;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава поднялась!");
            }
        }
        else if (isAtTop)
        {
            // Ждём на верхней точке
            if (lavaTimer >= stayUpDuration)
            {
                isAtTop = false;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава начинает опускаться!");
            }
        }
        else
        {
            // Опускаем лаву
            targetY = startPosition.y;
            currentPos.y = Mathf.Lerp(startPosition.y + riseHeight, targetY, lavaTimer * fallSpeed / riseHeight);
            transform.position = currentPos;

            // Проверяем достигли ли нижнюю точку
            if (currentPos.y <= targetY + 0.1f)
            {
                isRising = true;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава опустилась!");
            }
        }
    }

    // Игрок вошёл в лаву
    void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null) // Нашли PlayerStats = это игрок!
        {
            EnterLava(other.gameObject, stats);
        }
    }

    // Игрок вышел из лавы
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == playerInLava)
        {
            ExitLava();
        }
    }

    void EnterLava(GameObject player, PlayerStats stats)
    {
        playerInLava = player;
        damageTimer = 0f;

        Debug.Log($"🔥 {player.name} упал в лаву! HP: {stats.hp}/{stats.maxHp}");

        // Эффекты входа
        SpawnSteamEffect(player.transform.position);
        PlaySizzleSound(player.transform.position);

        if (instantKill)
        {
            // Мгновенная смерть
            KillPlayer(player, stats);
        }
        else
        {
            // Первый урон сразу
            ApplyDamageToPlayer(player);
        }
    }

    void ExitLava()
    {
        if (playerInLava != null)
        {
            Debug.Log($"💨 {playerInLava.name} выбрался из лавы!");
        }
        playerInLava = null;
        damageTimer = 0f;
    }

    void ApplyDamageToPlayer(GameObject player)
    {
        if (player == null) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        // Проверяем не мёртв ли уже
        if (stats.hp <= 0) return;

        // Наносим урон через PlayerStats.TakeDamage!
        stats.TakeDamage(damagePerTick, 0, ElementalType.Fire);

        Debug.Log($"💥 Лава нанесла {damagePerTick} урона! HP осталось: {stats.hp}/{stats.maxHp}");

        // Эффекты урона
        SpawnSteamEffect(player.transform.position);

        // Проверяем смерть
        if (stats.hp <= 0)
        {
            KillPlayer(player, stats);
        }
    }

    void KillPlayer(GameObject player, PlayerStats stats)
    {
        Debug.Log($"☠️ {player.name} СГОРЕЛ В ЛАВЕ! Применяем штраф...");

        // Штраф за смерть (из PlayerStats)
        stats.ApplyDeathPenalty();

        // Отключаем игрока визуально
        player.SetActive(false);

        // Эффекты смерти
        PlayDeathEffects(player.transform.position);

        // Показываем панель смерти
        DeathPanel.Show(player, stats, respawnPoint);
    }

    #region EFFECTS

    void SpawnSteamEffect(Vector3 position)
    {
        if (steamVFX == null) return;

        var ps = Instantiate(steamVFX, position, Quaternion.identity);
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    void PlaySizzleSound(Vector3 position)
    {
        if (sizzleSFX == null) return;

        AudioSource.PlayClipAtPoint(sizzleSFX, position);
    }

    void PlayDeathEffects(Vector3 position)
    {
        Debug.Log("💀 Эффекты смерти от лавы (можно добавить взрыв и т.д.)");
        // Здесь можно добавить:
        // - Взрывную анимацию
        // - Звук смерти
        // - Экранная тряска (Screen Shake)
    }

    #endregion

    #region DEBUG

    // Рисуем зону лавы в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.5f); // Оранжево-красный полупрозрачный

        var collider = GetComponent<Collider2D>();
        if (collider is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }

        // Рисуем иконку в Scene view
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "🔥 LAVA");
#endif
    }

    #endregion
}