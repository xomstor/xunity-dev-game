using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public int hp = 100;
    public int maxHp = 100;
    public int atk = 10;
    public int def = 5;
    public int spd = 10;
    public int lck = 5;

    [Header("Progress")]
    public int level = 1;
    public int experience = 0;

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(1, amount - def);
        hp = Mathf.Max(0, hp - finalDamage);
    }

    public int GetDamage()
    {
        return atk;
    }
}
