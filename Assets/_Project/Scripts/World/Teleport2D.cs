using UnityEngine;

public class Teleport2D : MonoBehaviour
{
    [Header("Телепортация")]
    [Tooltip("Куда телепортировать (перетащите объект из сцены)")]
    public Transform teleportTarget;

    [Tooltip("Тег объекта, который можно телепортировать")]
    public string targetTag = "Player";

    [Header("Респавн врагов при телепорте")]
    [Tooltip("Вызывать RespawnAllEnemies?")]
    public bool respawnEnemies = true;

    [Tooltip("Задержка перед респавном (сек)")]
    public float respawnDelay = 0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            SaveManager.Instance?.AutoSave();

            // 1. Телепортируем игрока
            other.transform.position = teleportTarget.position;
            Debug.Log($"✨ {other.name} телепортирован в {teleportTarget.name}");

            // 2. Респавним врагов
            if (respawnEnemies)
            {
                InvokeRespawn();
            }
        }
    }

    void InvokeRespawn()
    {
        EnemyRespawnManager manager = FindAnyObjectByType<EnemyRespawnManager>();

        if (manager != null)
        {
            if (respawnDelay > 0)
            {
                StartCoroutine(RespawnWithDelay(manager));
                Debug.Log($"⏳ Респавн через {respawnDelay} сек...");
            }
            else
            {
                manager.RespawnAllEnemies();
                Debug.Log($"🔄 Враги респавнены!");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ EnemyRespawnManager не найден!");
        }
    }

    System.Collections.IEnumerator RespawnWithDelay(EnemyRespawnManager mgr)
    {
        yield return new WaitForSeconds(respawnDelay);
        mgr.RespawnAllEnemies();
        Debug.Log($"🔄 Враги респавнены (задержка {respawnDelay} сек)");
    }
}