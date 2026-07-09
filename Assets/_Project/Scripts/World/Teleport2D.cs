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

            Transform player = other.transform;
            Vector3 destination = teleportTarget.position;
            string targetName = teleportTarget.name;

            TeleportEffect.Play(
                () =>
                {
                    player.position = destination;
                    Debug.Log($"[Teleport2D] {player.name} teleported to {targetName}");

                    if (respawnEnemies)
                        InvokeRespawn();
                },
                null);
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
        yield return new WaitForSecondsRealtime(respawnDelay);
        mgr.RespawnAllEnemies();
        Debug.Log($"[Teleport2D] Enemies respawned after delay {respawnDelay}s");
    }
}