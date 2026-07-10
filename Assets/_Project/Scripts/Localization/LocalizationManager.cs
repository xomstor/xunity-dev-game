using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public static event System.Action OnLanguageChanged;

    public const string PrefKey = "SelectedLanguage";

    public static readonly Dictionary<string, string> LanguageDisplayNames = new Dictionary<string, string>
    {
        { "ru", "Обычный" },
        { "en", "English" },
        { "ua", "Ukrainian" }
    };

    private Dictionary<string, string> currentTexts = new Dictionary<string, string>();
    private Dictionary<string, string> fallbackTexts = new Dictionary<string, string>();
    public string CurrentLanguage { get; private set; } = "ru";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        string saved = PlayerPrefs.GetString(PrefKey, "ru");
        SetLanguage(saved, false);
    }

    public void SetLanguage(string languageCode, bool save = true)
    {
        if (!LanguageDisplayNames.ContainsKey(languageCode))
            languageCode = "ru";

        if (CurrentLanguage == languageCode && currentTexts.Count > 0)
            return;

        LoadLanguage(languageCode);
        CurrentLanguage = languageCode;

        if (save)
        {
            PlayerPrefs.SetString(PrefKey, languageCode);
            PlayerPrefs.Save();
        }

        OnLanguageChanged?.Invoke();
    }

    void LoadLanguage(string languageCode)
    {
        currentTexts.Clear();
        fallbackTexts.Clear();

        TextAsset asset = Resources.Load<TextAsset>($"Localization/{languageCode}");
        if (asset == null)
        {
            Debug.LogWarning($"[LocalizationManager] Missing localization file for '{languageCode}'");
        }
        else
        {
            currentTexts = ParseSimpleJson(asset.text);
            Debug.Log($"[LocalizationManager] Loaded '{languageCode}' with {currentTexts.Count} entries.");
        }

        TextAsset ruAsset = Resources.Load<TextAsset>("Localization/ru");
        if (ruAsset != null)
        {
            fallbackTexts = ParseSimpleJson(ruAsset.text);
            Debug.Log($"[LocalizationManager] Loaded Russian fallback with {fallbackTexts.Count} entries.");
        }
        else
        {
            Debug.LogWarning("[LocalizationManager] Missing Russian fallback file.");
        }
    }

    public string Get(string key, string fallback = null)
    {
        if (currentTexts.TryGetValue(key, out string value))
            return value;

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        if (fallbackTexts.TryGetValue(key, out string defaultValue))
            return defaultValue;

        return key;
    }

    public static string GetText(string key, string fallback = null)
    {
        EnsureInstance();
        return Instance != null ? Instance.Get(key, fallback) : (fallback ?? key);
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("LocalizationManager");
        go.AddComponent<LocalizationManager>();
    }

    static Dictionary<string, string> ParseSimpleJson(string json)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        var matches = System.Text.RegularExpressions.Regex.Matches(
            json,
            @"""([^""]+)""\s*:\s*""((?:\\.|[^""\\])*)""",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string key = match.Groups[1].Value.Trim();
            string value = UnescapeJsonString(match.Groups[2].Value);
            if (!string.IsNullOrEmpty(key))
                result[key] = value;
        }

        return result;
    }

    static string UnescapeJsonString(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }
}
