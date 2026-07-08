using UnityEngine;

/// <summary>
/// Звуки НПС: начало разговора, завершение квеста, idle (периодический).
/// Повесь на НПС рядом с DialogueTrigger / QuestNPC, назначь клипы в инспекторе.
/// </summary>
public class NPCAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip dialogueStartClip;
    public AudioClip questCompleteClip;
    public AudioClip idleClip;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    public float maxDistance = 12f;
    [Tooltip("Интервал между idle-звуками (сек)")]
    public float idleInterval = 8f;
    [Tooltip("Играть idle только когда игрок рядом")]
    public bool idleOnlyWhenPlayerNear = true;
    public float idleHearRange = 8f;

    private AudioSource source;
    private float idleTimer;
    private Transform player;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = volume;

        idleTimer = idleInterval * Random.Range(0.5f, 1.5f);
    }

    void Update()
    {
        if (idleClip == null) return;

        idleTimer -= Time.deltaTime;
        if (idleTimer > 0f) return;

        idleTimer = idleInterval * Random.Range(0.8f, 1.5f);

        if (idleOnlyWhenPlayerNear)
        {
            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (player == null) return;
            if (Vector2.Distance(transform.position, player.position) > idleHearRange) return;
        }

        source.PlayOneShot(idleClip, volume);
    }

    public void PlayDialogueStart()
    {
        if (dialogueStartClip != null)
            source.PlayOneShot(dialogueStartClip, volume);
    }

    public void PlayQuestComplete()
    {
        if (questCompleteClip != null)
            source.PlayOneShot(questCompleteClip, volume);
    }
}
