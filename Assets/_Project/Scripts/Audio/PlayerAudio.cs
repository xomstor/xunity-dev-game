using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip walkClip;
    public AudioClip runClip;
    public AudioClip jumpClip;
    public AudioClip attack1Clip;
    public AudioClip attack2Clip;
    public AudioClip attack3Clip;
    public AudioClip hurtClip;
    public AudioClip rollClip;

    [Header("Settings")]
    public AudioSource audioSource;
    public float walkStepInterval = 0.35f;
    public float runStepInterval = 0.25f;

    private float stepTimer;
    private bool isMoving;
    private float currentMoveSpeed;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (isMoving && currentMoveSpeed > 0)
        {
            float interval = currentMoveSpeed > 4f ? runStepInterval : walkStepInterval;
            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayStep();
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    public void SetMovement(bool moving, float speed)
    {
        isMoving = moving;
        currentMoveSpeed = speed;
    }

    void PlayStep()
    {
        AudioClip clip = currentMoveSpeed > 4f ? runClip : walkClip;
        PlayOneShot(clip);
    }

    public void PlayJump()
    {
        PlayOneShot(jumpClip);
    }

    public void PlayAttack(int comboIndex)
    {
        AudioClip clip = comboIndex switch
        {
            2 => attack2Clip,
            3 => attack3Clip,
            _ => attack1Clip,
        };
        PlayOneShot(clip);
    }

    public void PlayHurt()
    {
        PlayOneShot(hurtClip);
    }

    public void PlayRoll()
    {
        PlayOneShot(rollClip);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}
