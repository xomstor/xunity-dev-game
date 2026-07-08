using UnityEngine;

/// <summary>
/// Место на этаже: при входе игрока в триггер включает свой саундтрек.
/// Повесь на GameObject с Collider2D (isTrigger = true), назначь клип в инспекторе.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FloorMusicZone : MonoBehaviour
{
    [Header("Floor Soundtrack")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    public bool loop = true;

    [Header("Behaviour")]
    [Tooltip("Вернуть музыку сцены при выходе из зоны")]
    public bool restoreOnExit = false;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (LevelAudioManager.Instance == null || musicClip == null) return;

        LevelAudioManager.Instance.PlayOverrideMusic(musicClip, volume, loop);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!restoreOnExit) return;
        if (!other.CompareTag("Player")) return;
        if (LevelAudioManager.Instance == null) return;

        LevelAudioManager.Instance.ClearOverrideMusic();
    }
}
