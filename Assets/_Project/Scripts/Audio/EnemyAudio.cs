using UnityEngine;

/// <summary>
/// Звуки моба: стоит (idle), идёт (walk), атакует, смерть.
/// Повесь на моба рядом с AutoCombat, назначь клипы в инспекторе.
/// AutoCombat сам найдёт этот компонент и будет дергать методы.
/// </summary>
public class EnemyAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip idleClip;
    public AudioClip walkClip;
    public AudioClip attackClip;
    public AudioClip deathClip;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("Макс. дистанция слышимости")]
    public float maxDistance = 15f;
    [Tooltip("Минимальный интервал между idle-звуками (сек)")]
    public float idleInterval = 4f;

    private AudioSource loopSource;   // idle/walk (зацикленные)
    private AudioSource oneShotSource; // attack/death
    private float idleTimer;
    private int currentState = -1;    // 0 = idle, 1 = walk, -1 = none

    void Awake()
    {
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
        loopSource.maxDistance = maxDistance;
        loopSource.rolloffMode = AudioRolloffMode.Linear;
        loopSource.volume = volume;

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 1f;
        oneShotSource.maxDistance = maxDistance;
        oneShotSource.rolloffMode = AudioRolloffMode.Linear;
        oneShotSource.volume = volume;
    }

    /// <summary>state: 0 = стоит, 1 = идёт</summary>
    public void SetMovementState(int state)
    {
        if (state == currentState) return;
        currentState = state;

        if (state == 1 && walkClip != null)
        {
            loopSource.clip = walkClip;
            loopSource.loop = true;
            loopSource.Play();
        }
        else
        {
            loopSource.Stop();
            idleTimer = idleInterval * Random.Range(0.3f, 1f);
        }
    }

    void Update()
    {
        // Периодический idle-звук когда моб стоит
        if (currentState == 0 && idleClip != null)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                oneShotSource.PlayOneShot(idleClip, volume);
                idleTimer = idleInterval * Random.Range(0.8f, 1.5f);
            }
        }
    }

    public void PlayAttack()
    {
        if (attackClip != null)
            oneShotSource.PlayOneShot(attackClip, volume);
    }

    public void PlayDeath()
    {
        loopSource.Stop();
        if (deathClip != null)
            oneShotSource.PlayOneShot(deathClip, volume);
    }
}
