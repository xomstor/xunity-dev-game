using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Leave empty to use the current text as the localization key.")]
    public string key;

    private TextMeshProUGUI tmpText;
    private Text legacyText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        legacyText = GetComponent<Text>();
    }

    void Start()
    {
        if (string.IsNullOrEmpty(key))
        {
            if (tmpText != null)
                key = tmpText.text;
            else if (legacyText != null)
                key = legacyText.text;
        }

        Refresh();
        LocalizationManager.OnLanguageChanged += Refresh;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        LocalizationManager.EnsureInstance();
        if (LocalizationManager.Instance == null) return;

        string localized = LocalizationManager.Instance.Get(key, null);
        if (string.IsNullOrEmpty(localized)) localized = key;

        if (tmpText != null)
            tmpText.text = localized;
        else if (legacyText != null)
            legacyText.text = localized;
    }
}
