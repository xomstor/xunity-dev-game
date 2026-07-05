using UnityEngine;

public class WorldLevelManager : MonoBehaviour
{
    public static WorldLevelManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("WorldLevelManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<WorldLevelManager>();
        }
    }

    [Header("World Level")]
    public int currentWorldLevel = 1;
    public int maxWorldLevel = 10;

    [Header("Scaling Per World Level")]
    public float enemyHealthMultiplier = 1.15f;
    public float enemyDamageMultiplier = 1.1f;
    public float rewardMultiplier = 1.2f;

    [Header("Level Progression")]
    public int currentLevelIndex = 1;
    public int bossLevelIndex = 5;
    public int worldLevelOfferLevelIndex = 4;

    public float CurrentEnemyHealthMultiplier => Mathf.Pow(enemyHealthMultiplier, currentWorldLevel - 1);
    public float CurrentEnemyDamageMultiplier => Mathf.Pow(enemyDamageMultiplier, currentWorldLevel - 1);
    public float CurrentRewardMultiplier => Mathf.Pow(rewardMultiplier, currentWorldLevel - 1);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void IncreaseWorldLevel()
    {
        if (currentWorldLevel >= maxWorldLevel) return;
        currentWorldLevel++;
        Save();
    }

    public void CompleteLevel(int levelIndex)
    {
        currentLevelIndex = Mathf.Max(currentLevelIndex, levelIndex);
        Save();
    }

    public bool ShouldOfferWorldLevelIncrease(int completedLevelIndex)
    {
        return completedLevelIndex == worldLevelOfferLevelIndex
            && currentLevelIndex < completedLevelIndex
            && currentWorldLevel < maxWorldLevel;
    }

    public void Save()
    {
        PlayerPrefs.SetInt("WorldLevel", currentWorldLevel);
        PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        currentWorldLevel = PlayerPrefs.GetInt("WorldLevel", 1);
        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 1);
    }

    public void Reset()
    {
        currentWorldLevel = 1;
        currentLevelIndex = 1;
        Save();
    }
}
