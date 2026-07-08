using UnityEngine;
using System.Collections;
public class LavaDamage : MonoBehaviour
{
    [Header("Урон")]
    public int damagePerTick = 20;          // Урон за одно "тик"
    public float tickInterval = 0.5f;       // Как часто наносить (сек)
    public bool instantKill = false;        // Мгновенная смерть?

    [Header("Респаун")]
    public Transform respawnPoint;          // Точка респауна (создайте пустой объект!)
    public float respawnDelay = 2f;         // Задержка перед респауном

    [Header("Эффекты (опционально)")]
    public ParticleSystem steamVFX;         // Пар/дым
    public AudioClip sizzleSFX;            // Звук шипения

    private float damageTimer;
    private GameObject playerInLava;

    void Update()
    {
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
        stats.TakeDamage(damagePerTick);

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

        // Респаун через задержку
        if (respawnPoint != null)
        {
            StartCoroutine(RespawnPlayer(player, stats));
        }
        else
        {
            Debug.LogWarning("⚠️ Не назначена точка респауна! Игрок отключён навсегда.");
        }
    }

    IEnumerator RespawnPlayer(GameObject player, PlayerStats stats)
    {
        yield return new WaitForSeconds(respawnDelay);

        // Перемещаем в точку респауна
        player.transform.position = respawnPoint.position;

        // Восстанавливаем здоровье
        stats.hp = stats.maxHp;

        // Показываем игрока
        player.SetActive(true);

        Debug.Log($"🔄 {player.name} возродился! HP восстановлено: {stats.hp}/{stats.maxHp}");
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