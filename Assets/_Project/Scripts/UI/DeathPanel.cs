using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// Панель смерти: YOUR SOUL IS DEAD + кнопки ВЕРНУТЬСЯ В ГОРОД / ЗАГРУЗКА
/// </summary>
public static class DeathPanel
{
    private static GameObject currentPanel;

    public static void Show(GameObject player, PlayerStats stats, Transform respawnPoint = null)
    {
        if (player == null || stats == null) return;
        if (currentPanel != null) return;

        Time.timeScale = 0f;

        GameObject canvasGO = new GameObject("DeathPanelCanvas");
        currentPanel = canvasGO;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        // Фон
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.85f);
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Панель
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        RectTransform panelRt = panelGO.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(650, 420);

        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30;
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Заголовок "YOUR SOUL IS DEAD"
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "YOUR SOUL IS DEAD";
        title.fontSize = 56;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.15f, 0.15f, 1f);
        title.fontStyle = FontStyles.Bold;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 90);

        // Подзаголовок
        GameObject subtitleGO = new GameObject("Subtitle");
        subtitleGO.transform.SetParent(panelGO.transform, false);
        subtitleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI subtitle = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitle.text = "Вы погибли";
        subtitle.fontSize = 28;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        RectTransform subtitleRt = subtitleGO.GetComponent<RectTransform>();
        subtitleRt.sizeDelta = new Vector2(0, 40);

        // Кнопка "ВЕРНУТЬСЯ В ГОРОД"
        GameObject cityBtn = CreateButton(panelGO.transform, "ВЕРНУТЬСЯ В ГОРОД", new Color(0.2f, 0.75f, 0.25f, 1f), () =>
        {
            ResurrectInCity(player, stats, respawnPoint);
        });

        // Кнопка "ЗАГРУЗКА"
        GameObject loadBtn = CreateButton(panelGO.transform, "ЗАГРУЗКА", new Color(0.2f, 0.6f, 1f, 1f), () =>
        {
            OpenLoadMenu();
        });
    }

    static GameObject CreateButton(Transform parent, string label, Color color, System.Action onClick)
    {
        GameObject btnGO = new GameObject(label + "Button");
        btnGO.transform.SetParent(parent, false);
        btnGO.AddComponent<CanvasRenderer>();
        Image img = btnGO.AddComponent<Image>();
        img.color = color;
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 70);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        btn.onClick.AddListener(() => onClick?.Invoke());
        return btnGO;
    }

    static void ResurrectInCity(GameObject player, PlayerStats stats, Transform respawnPoint)
    {
        DestroyPanel();
        Time.timeScale = 1f;

        if (stats == null)
        {
            Debug.LogError("[DeathPanel] ResurrectInCity: PlayerStats is null!");
            return;
        }

        if (PlayerStateTransfer.Instance != null)
        {
            PlayerStateTransfer.Instance.overrideHp = stats.maxHp;
            PlayerStateTransfer.Instance.spawnAtHub = true;
        }

        Debug.Log("[DeathPanel] Returning to GameScene (hub)...");

        TeleportEffect.Play(
            () =>
            {
                SceneManager.LoadScene("GameScene");
            },
            null);
    }

    static void OpenLoadMenu()
    {
        DestroyPanel();

        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>();
        if (pauseMenu != null)
        {
            if (pauseMenu.pausePanel != null && !pauseMenu.pausePanel.activeSelf)
                pauseMenu.TogglePause();

            pauseMenu.OpenSavePanel();
        }
        else
        {
            Debug.LogWarning("PauseMenu не найден!");
            Time.timeScale = 1f;
        }
    }

    static void DestroyPanel()
    {
        if (currentPanel != null)
        {
            Object.Destroy(currentPanel);
            currentPanel = null;
        }
    }

    static Transform FindRespawnPoint()
    {
        // 1. Ищем SpawnPoint с isHub
        SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
        SpawnPoint hub = spawnPoints.FirstOrDefault(s => s.isHub);
        if (hub != null) return hub.transform;

        // 2. Любой SpawnPoint
        if (spawnPoints.Length > 0) return spawnPoints[0].transform;

        // 3. Ищем LavaDamage respawnPoint
        LavaDamage[] lava = Object.FindObjectsByType<LavaDamage>(FindObjectsInactive.Include);
        foreach (var l in lava)
        {
            if (l.respawnPoint != null)
                return l.respawnPoint;
        }

        // 4. Ищем по имени
        GameObject byName = GameObject.Find("RespawnPoint") ?? GameObject.Find("SpawnPoint") ?? GameObject.Find("Hub");
        if (byName != null) return byName.transform;

        return null;
    }
}
