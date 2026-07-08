using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Запись в бестиарии с иконкой моба, именем и информацией
/// </summary>
public class BestiaryEntry : MonoBehaviour
{
    public Image enemyIcon;
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI killCount;
    public TextMeshProUGUI description;

    public void SetData(string name, int kills, Sprite icon, string desc = "")
    {
        if (enemyName != null)
            enemyName.text = name;
        
        if (killCount != null)
            killCount.text = $"Убито: {kills}";
        
        if (enemyIcon != null)
            enemyIcon.sprite = icon;
        
        if (description != null)
            description.text = desc;
    }
}
