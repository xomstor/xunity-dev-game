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
    [Tooltip("Дополнительные звуки (например, hit, aggro, taunt). Используются через PlayExtra/PlayRandomExtra.")]
    public AudioClip[] extraClips;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("Макс. дистанция слышимости")]
    public float maxDistance = 15f;
    [Tooltip("Минимальный интервал между idle-звуками (сек)")]
    public float idleInterval = 4f;
    [Tooltip("Интервал между случайными доп. звуками (0 = отключено)")]
    public float extraSoundInterval = 0f;
    [Tooltip("Шанс проигрыша случайного доп. звука по таймеру")]
    [Range(0f, 1f)] public float extraSoundChance = 0.3f;

    private AudioSource loopSource;   // idle/walk (зацикленные)
    private AudioSource oneShotSource; // attack/death/extra
    private float idleTimer;
    private float extraTimer;
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

        extraTimer = extraSoundInterval * Random.Range(0.7f, 1.3f);
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

        // Случайные дополнительные звуки (для разнообразия)
        if (extraSoundInterval > 0f && extraClips != null && extraClips.Length > 0)
        {
            extraTimer -= Time.deltaTime;
            if (extraTimer <= 0f)
            {
                extraTimer = extraSoundInterval * Random.Range(0.7f, 1.3f);
                if (Random.value < extraSoundChance)
                {
                    PlayRandomExtra();
                }
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

    public void PlayExtra(int index)
    {
        if (extraClips == null || extraClips.Length == 0) return;
        if (index < 0 || index >= extraClips.Length) return;
        if (extraClips[index] != null)
            oneShotSource.PlayOneShot(extraClips[index], volume);
    }

    public void PlayRandomExtra()
    {
        if (extraClips == null || extraClips.Length == 0) return;
        int i = Random.Range(0, extraClips.Length);
        if (extraClips[i] != null)
            oneShotSource.PlayOneShot(extraClips[i], volume);
    }
}
