using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int PlayerGold { get; private set; }
    public int PlayerExp { get; private set; }
    public int PlayerLevel { get; private set; } = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddGold(int amount)
    {
        PlayerGold += amount;
    }

    public void AddExp(int amount)
    {
        PlayerExp += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int expNeeded = PlayerLevel * 100;
        if (PlayerExp >= expNeeded)
        {
            PlayerExp -= expNeeded;
            PlayerLevel++;
            Debug.Log($"Level Up! Now level {PlayerLevel}");
        }
    }
}
