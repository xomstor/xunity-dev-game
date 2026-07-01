using UnityEngine;
using UnityEngine.SceneManagement;

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

    private AudioSource audioSource;
    private string currentScene;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

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

        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();
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

    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}
