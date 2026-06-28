using UnityEngine;

public class Teleport2D : MonoBehaviour
{
    [Tooltip("Куда телепортировать (перетащите объект из сцены)")]
    public Transform teleportTarget;

    [Tooltip("Тег объекта, который можно телепортировать (обычно 'Player')")]
    public string targetTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что это нужный объект
        if (other.CompareTag(targetTag))
        {
            // Телепортируем!
            other.transform.position = teleportTarget.position;

            Debug.Log($"✨ {other.name} телепортирован в {teleportTarget.name}");
        }
    }
}