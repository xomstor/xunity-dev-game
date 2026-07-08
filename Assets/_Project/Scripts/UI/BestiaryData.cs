using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ScriptableObject для хранения информации о враге в бестиарии
/// </summary>
[CreateAssetMenu(fileName = "NewBestiaryEntry", menuName = "Bestiary/Enemy Entry")]
public class BestiaryData : ScriptableObject
{
    public string enemyId;
    public string enemyName;
    [TextArea(3, 6)]
    public string description;
    public Sprite enemyIcon;  // PNG иконка моба (рекомендуемый размер 256x256)
    public int baseLevel = 1;
    public int baseHealth = 50;
    public int baseDamage = 10;
    
    /// <summary>
    /// Получить данные врага по имени (без цифр в скобках)
    /// </summary>
    public static BestiaryData GetByName(string cleanEnemyName)
    {
        // Ищем в папке Resources/Bestiary
        BestiaryData[] allData = Resources.LoadAll<BestiaryData>("Bestiary");
        
        // Если не найдено, ищем везде в Resources
        if (allData.Length == 0)
        {
            allData = Resources.LoadAll<BestiaryData>("");
        }
        
        // Если всё ещё не найдено, ищем везде в проекте (медленнее, но надёжнее)
        if (allData.Length == 0)
        {
            allData = FindObjectsByType<BestiaryData>(FindObjectsInactive.Include);
        }
        
        foreach (var data in allData)
        {
            if (data != null && data.enemyName == cleanEnemyName)
            {
                Debug.Log($"[BestiaryData] Found data for: {cleanEnemyName}");
                return data;
            }
        }
        
        Debug.LogWarning($"[BestiaryData] No data found for enemy: {cleanEnemyName}");
        return null;
    }
}
