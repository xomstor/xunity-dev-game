using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TrashEmanatorUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;
    public GameObject itemPrefab;
    public GameObject panel;
    public TextMeshProUGUI infoText;
    public Button closeButton;
    public Button openButton;

    [Header("Auto-create fallback")]
    public bool autoCreate = true;

    TrashEmanator emanator;
    List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        emanator = GetComponent<TrashEmanator>();
        if (emanator == null)
            emanator = TrashEmanator.Instance;
        if (emanator == null)
            emanator = FindAnyObjectByType<TrashEmanator>();

        if (emanator != null)
            emanator.OnListChanged += RefreshList;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (openButton != null)
            openButton.onClick.AddListener(Open);

        if (autoCreate && contentParent == null)
            CreateFallbackUI();

        if (panel != null)
            panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (emanator != null)
            emanator.OnListChanged -= RefreshList;
    }

    public void Open()
    {
        if (panel != null)
            panel.SetActive(true);
        RefreshList();
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void RefreshList()
    {
        if (emanator == null || contentParent == null) return;
        emanator.CleanupDestroyedItems();

        foreach (var go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();

        Inventory inv = FindAnyObjectByType<Inventory>();
        PlayerStats stats = FindAnyObjectByType<PlayerStats>();

        for (int i = 0; i < emanator.trashedItems.Count; i++)
        {
            TrashedItem item = emanator.trashedItems[i];
            TrashedItemState state = emanator.GetItemState(item);
            int index = i;

            GameObject go = CreateItemRow(item, state, index, inv, stats);
            if (go != null)
                spawnedItems.Add(go);
        }

        if (infoText != null)
            infoText.text = emanator.trashedItems.Count == 0 ? "Нет предметов" : $"Предметов: {emanator.trashedItems.Count}";
    }

    GameObject CreateItemRow(TrashedItem item, TrashedItemState state, int index, Inventory inv, PlayerStats stats)
    {
        if (itemPrefab == null) return null;

        GameObject go = Instantiate(itemPrefab, contentParent, false);
        TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>();
        Button btn = go.GetComponentInChildren<Button>();

        string status = state switch
        {
            TrashedItemState.Recoverable => "бесплатно",
            TrashedItemState.Payable => $"{emanator.GetBuyBackPrice()} gold",
            _ => "уничтожен"
        };

        foreach (var t in texts)
        {
            if (t.name.Contains("Name") || t.name.Contains("Title"))
                t.text = item.itemName;
            else if (t.name.Contains("Qty") || t.name.Contains("Count"))
                t.text = $"x{item.quantity}";
            else
                t.text = $"{item.itemName} x{item.quantity} ({status})";
        }

        if (btn != null)
        {
            btn.interactable = state != TrashedItemState.Destroyed;
            btn.onClick.AddListener(() => OnRecover(index, inv, stats));
        }

        return go;
    }

    void OnRecover(int index, Inventory inv, PlayerStats stats)
    {
        if (emanator == null) return;
        emanator.RecoverItem(index, inv, stats);
        RefreshList();
    }

    void CreateFallbackUI()
    {
        if (panel == null)
        {
            panel = new GameObject("TrashEmanatorPanel");
            panel.transform.SetParent(transform, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600, 500);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        }

        if (contentParent == null)
        {
            GameObject scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(panel.transform, false);
            RectTransform scrollRt = scrollGO.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.1f, 0.15f);
            scrollRt.anchorMax = new Vector2(0.9f, 0.85f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);
            viewport.AddComponent<Mask>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = cRt;
            contentParent = cRt;
        }

        if (itemPrefab == null)
        {
            GameObject prefab = new GameObject("TrashItemPrefab");
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50);
            Image img = prefab.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            Button btn = prefab.AddComponent<Button>();
            btn.targetGraphic = img;
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(prefab.transform, false);
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            itemPrefab = prefab;
            itemPrefab.SetActive(false);
        }
    }
}
