#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class ShopUIBuilder : EditorWindow
{
    [MenuItem("Tools/Build Shop UI")]
    public static void ShowWindow()
    {
        GetWindow<ShopUIBuilder>("Shop UI Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Create Shop UI elements in the scene", EditorStyles.boldLabel);
        if (GUILayout.Button("Build Shop UI"))
        {
            BuildShopUI();
        }
    }

    static void BuildShopUI()
    {
        ShopUIController controller = FindAnyObjectByType<ShopUIController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "No ShopUIController found in scene. Run Tools/Setup Working Shop first.", "OK");
            return;
        }

        Canvas canvas = controller.GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in scene.", "OK");
            return;
        }

        // Clear old auto-created shop UI
        if (controller.shopPanel != null)
        {
            Undo.DestroyObjectImmediate(controller.shopPanel);
        }

        GameObject shopPanel = CreatePanel("ShopPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(800, 600), controller.panelColor);
        Undo.RegisterCreatedObjectUndo(shopPanel, "Build Shop UI");

        GameObject titleText = CreateText("TitleText", shopPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -30), new Vector2(300, 50), controller.titleText, 36, TextAnchor.MiddleLeft, Color.white);

        GameObject closeButton = CreateButton("CloseButton", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -40), new Vector2(80, 40), controller.closeButtonText, controller.closeButtonColor, 24);

        GameObject goldText = CreateText("GoldText", shopPanel.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-280, -30), new Vector2(180, 50), controller.goldPrefix + "0", 28, TextAnchor.MiddleRight, Color.yellow);

        GameObject leftPanel = CreatePanel("ItemListPanel", shopPanel.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(320, 480), new Color(0, 0, 0, 0.3f));

        GameObject itemContainer = CreatePanel("ItemContainer", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(300, 460), new Color(0, 0, 0, 0));
        VerticalLayoutGroup vlg = itemContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        itemContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject rightPanel = CreatePanel("ItemDetailPanel", shopPanel.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-300, 20), new Vector2(360, 440), new Color(0, 0, 0, 0.3f));

        GameObject itemIcon = CreateImage("ItemIcon", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(100, 100));

        GameObject itemNameText = CreateText("ItemNameText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(340, 40), "", 30, TextAnchor.MiddleCenter, Color.white);

        GameObject itemDescriptionText = CreateText("ItemDescriptionText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(340, 120), controller.noItemSelectedMessage, 22, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f, 1f));
        TextMeshProUGUI descTmp = itemDescriptionText.GetComponent<TextMeshProUGUI>();
        descTmp.overflowMode = TextOverflowModes.Overflow;
        descTmp.textWrappingMode = TextWrappingModes.Normal;

        GameObject itemPriceText = CreateText("ItemPriceText", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(340, 40), "", 26, TextAnchor.MiddleCenter, Color.yellow);

        GameObject buyButton = CreateButton("BuyButton", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(200, 50), controller.buyButtonText, controller.buyButtonColor, 28);

        GameObject sellButton = CreateButton("SellButton", shopPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(250, 50), controller.sellButtonText, controller.sellButtonColor, 24);

        GameObject tooltipPanel = CreatePanel("TooltipPanel", shopPanel.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(250, 80), controller.tooltipColor);
        tooltipPanel.SetActive(false);
        GameObject tooltipText = CreateText("TooltipText", tooltipPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), "", 20, TextAnchor.MiddleCenter, Color.white);
        TextMeshProUGUI tooltipTmp = tooltipText.GetComponent<TextMeshProUGUI>();
        tooltipTmp.overflowMode = TextOverflowModes.Overflow;
        tooltipTmp.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tooltipTextRt = tooltipText.GetComponent<RectTransform>();
        tooltipTextRt.offsetMin = new Vector2(10, 10);
        tooltipTextRt.offsetMax = new Vector2(-10, -10);
        ContentSizeFitter tooltipFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        tooltipFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Assign references
        controller.shopPanel = shopPanel;
        controller.itemContainer = itemContainer.transform;
        controller.goldText = goldText.GetComponent<TextMeshProUGUI>();
        controller.itemNameText = itemNameText.GetComponent<TextMeshProUGUI>();
        controller.itemDescriptionText = descTmp;
        controller.itemPriceText = itemPriceText.GetComponent<TextMeshProUGUI>();
        controller.itemIcon = itemIcon.GetComponent<Image>();
        controller.buyButton = buyButton.GetComponent<Button>();
        controller.sellButton = sellButton.GetComponent<Button>();
        controller.closeButton = closeButton.GetComponent<Button>();
        controller.tooltipPanel = tooltipPanel;
        controller.tooltipText = tooltipTmp;

        shopPanel.SetActive(false);

        EditorUtility.SetDirty(controller);
        Selection.activeGameObject = shopPanel;
        EditorUtility.DisplayDialog("Done", "Shop UI elements created in scene. You can now drag them around in the Canvas.", "OK");
    }

    static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
        img.type = Image.Type.Simple;
        img.color = color;
        return go;
    }

    static GameObject CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string text, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = GetTextAlignmentOptions(alignment);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return go;
    }

    static GameObject CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        return go;
    }

    static GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string text, Color color, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        go.AddComponent<CanvasRenderer>();
        Image img = go.AddComponent<Image>();
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
        img.type = Image.Type.Simple;
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        textGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return go;
    }

    static TextAlignmentOptions GetTextAlignmentOptions(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }
}
#endif
