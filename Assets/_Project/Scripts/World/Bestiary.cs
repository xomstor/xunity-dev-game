using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр убитых мобов. Хранится в PlayerPrefs, переживает сцены и перезапуски.
/// AutoCombat.Die() вызывает RegisterKill автоматически.
/// </summary>
public static class Bestiary
{
    private const string PrefsKey = "BestiaryData";

    private static Dictionary<string, int> kills;

    public static event System.Action OnBestiaryChanged;

    static void EnsureLoaded()
    {
        if (kills != null) return;
        kills = new Dictionary<string, int>();

        string raw = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(raw)) return;

        foreach (string entry in raw.Split('|'))
        {
            int sep = entry.LastIndexOf(':');
            if (sep <= 0) continue;
            string name = entry.Substring(0, sep);
            if (int.TryParse(entry.Substring(sep + 1), out int count))
                kills[name] = count;
        }
    }

    public static void RegisterKill(string enemyName)
    {
        if (string.IsNullOrEmpty(enemyName)) return;
        EnsureLoaded();

        // Убираем "(Clone)" и цифры-суффиксы
        string cleanName = enemyName.Replace("(Clone)", "").Trim();

        kills.TryGetValue(cleanName, out int current);
        kills[cleanName] = current + 1;
        Save();
        OnBestiaryChanged?.Invoke();
    }

    public static IReadOnlyDictionary<string, int> GetAll()
    {
        EnsureLoaded();
        return kills;
    }

    public static int GetKillCount(string enemyName)
    {
        EnsureLoaded();
        kills.TryGetValue(enemyName, out int count);
        return count;
    }

    public static void Clear()
    {
        kills = new Dictionary<string, int>();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
        OnBestiaryChanged?.Invoke();
    }

    static void Save()
    {
        List<string> parts = new List<string>();
        foreach (var kv in kills)
            parts.Add($"{kv.Key}:{kv.Value}");
        PlayerPrefs.SetString(PrefsKey, string.Join("|", parts));
        PlayerPrefs.Save();
    }
}
