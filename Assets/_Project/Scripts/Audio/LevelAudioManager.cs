using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelAudioManager : MonoBehaviour
{
    public static LevelAudioManager Instance { get; private set; }

    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip musicClip;
        [Range(0f, 1f)] public float volume = 0.5f;
        public bool loop = true;
    }

    [Header("Music")]
    public SceneMusic[] sceneMusicList;

    [Header("Fallback")]
    public AudioClip defaultMusic;
    [Range(0f, 1f)] public float defaultVolume = 0.5f;

    [Header("Crossfade")]
    [Range(0f, 5f)] public float crossfadeDuration = 1.5f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource inactiveSource => activeSource == sourceA ? sourceB : sourceA;

    private string currentScene;
    private Coroutine crossfadeRoutine;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceA.playOnAwake = false;
        sourceA.spatialBlend = 0f;

        sourceB.loop = true;
        sourceB.playOnAwake = false;
        sourceB.spatialBlend = 0f;

        activeSource = sourceA;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        if (currentScene == sceneName) return;
        currentScene = sceneName;

        SceneMusic sceneMusic = FindMusicForScene(sceneName);

        AudioClip clip = sceneMusic != null ? sceneMusic.musicClip : defaultMusic;
        float volume = sceneMusic != null ? sceneMusic.volume : defaultVolume;
        bool loop = sceneMusic != null ? sceneMusic.loop : true;

        PlayMusic(clip, volume, loop);
    }

    SceneMusic FindMusicForScene(string sceneName)
    {
        if (sceneMusicList == null) return null;

        foreach (SceneMusic sceneMusic in sceneMusicList)
        {
            if (sceneMusic.sceneName == sceneName)
                return sceneMusic;
        }
        return null;
    }

    void PlayMusic(AudioClip clip, float volume, bool loop)
    {
        if (clip == null) return;
        if (activeSource.clip == clip && activeSource.isPlaying && activeSource.loop == loop)
        {
            activeSource.volume = volume;
            return;
        }

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        if (crossfadeDuration <= 0f)
        {
            sourceA.Stop();
            sourceB.Stop();
            activeSource.clip = clip;
            activeSource.volume = volume;
            activeSource.loop = loop;
            activeSource.Play();
            return;
        }

        crossfadeRoutine = StartCoroutine(CrossfadeTo(clip, volume, loop));
    }

    IEnumerator CrossfadeTo(AudioClip clip, float targetVolume, bool loop)
    {
        AudioSource next = inactiveSource;
        next.Stop();
        next.clip = clip;
        next.loop = loop;
        next.volume = 0f;
        next.Play();

        float startVolume = activeSource.volume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            float t = elapsed / crossfadeDuration;
            activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            next.volume = Mathf.Lerp(0f, targetVolume, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
        next.volume = targetVolume;
        activeSource = next;

        crossfadeRoutine = null;
    }

    public void StopMusic()
    {
        if (sourceA != null) sourceA.Stop();
        if (sourceB != null) sourceB.Stop();
    }

    // ===== Floor override music (used by FloorMusicZone) =====
    private AudioClip overrideClip;

    public void PlayOverrideMusic(AudioClip clip, float volume, bool loop = true)
    {
        if (clip == null) return;
        if (overrideClip == clip && activeSource.isPlaying) return;

        overrideClip = clip;
        PlayMusic(clip, volume, loop);
    }

    public void ClearOverrideMusic()
    {
        if (overrideClip == null) return;
        overrideClip = null;

        // Return to scene music
        string sceneName = SceneManager.GetActiveScene().name;
        SceneMusic sceneMusic = FindMusicForScene(sceneName);
        AudioClip clip = sceneMusic != null ? sceneMusic.musicClip : defaultMusic;
        float volume = sceneMusic != null ? sceneMusic.volume : defaultVolume;

        if (clip == null) { StopMusic(); return; }
        PlayMusic(clip, volume, true);
    }
}
