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
        ShopUIController controller = FindBestController();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "No ShopUIController found in scene. Run Tools/Setup Working Shop first.", "OK");
            return;
        }

        // Remove old shop UI objects anywhere in the scene
        foreach (GameObject root in controller.gameObject.scene.GetRootGameObjects())
        {
            DestroyOldShopObjects(root.transform);
        }
        // Also clear old panel reference on this controller if it is still alive
        if (controller.shopPanel != null && (controller.shopPanel.name == "ShopPanel" || controller.shopPanel.name == "ShopCanvas"))
        {
            Undo.DestroyObjectImmediate(controller.shopPanel);
        }

        // Ensure controller is on a Canvas object so the hierarchy is clean
        Canvas mainCanvas = controller.GetComponent<Canvas>();
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.isRootCanvas);
            if (mainCanvas == null)
                mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                controller.transform.SetParent(mainCanvas.transform, false);
            }
        }

        // Create separate shop canvas on Foreground layer, using same scaler as main canvas
        GameObject shopCanvasGO = new GameObject("ShopCanvas", typeof(RectTransform));
        shopCanvasGO.transform.SetParent(controller.transform, false);
        Undo.RegisterCreatedObjectUndo(shopCanvasGO, "Build Shop UI");
        Canvas shopCanvas = shopCanvasGO.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 100;
        shopCanvas.overrideSorting = true;
        shopCanvas.sortingLayerID = SortingLayer.NameToID("Foreground");
        shopCanvasGO.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = shopCanvasGO.AddComponent<CanvasScaler>();
        CanvasScaler mainScaler = mainCanvas?.GetComponent<CanvasScaler>();
        if (mainScaler != null)
        {
            scaler.uiScaleMode = mainScaler.uiScaleMode;
            scaler.referenceResolution = mainScaler.referenceResolution;
            scaler.screenMatchMode = mainScaler.screenMatchMode;
            scaler.matchWidthOrHeight = mainScaler.matchWidthOrHeight;
            scaler.scaleFactor = mainScaler.scaleFactor;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Main shop panel: stretches with screen, leaving 5% margin
        GameObject shopPanel = CreatePanel("ShopPanel", shopCanvasGO.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, controller.panelColor);

        // --- Header ---
        GameObject header = CreatePanel("Header", shopPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -35), new Vector2(0, 70), new Color(0, 0, 0, 0.4f));
        GameObject titleText = CreateText("TitleText", header.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(300, 50), controller.titleText, 36, TextAnchor.MiddleLeft, Color.white);
        GameObject goldText = CreateText("GoldText", header.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(180, 50), controller.goldPrefix + "0", 28, TextAnchor.MiddleRight, Color.yellow);
        GameObject closeButton = CreateButton("CloseButton", header.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(60, 40), controller.closeButtonText, controller.closeButtonColor, 22);

        // --- Body ---
        GameObject body = CreatePanel("Body", shopPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -35), new Vector2(0, -140), new Color(0, 0, 0, 0));
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 20;
        bodyLayout.padding = new RectOffset(20, 20, 10, 10);
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = false;

        // Left: item list
        GameObject leftPanel = CreatePanel("LeftPanel", body.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Color(0, 0, 0, 0.25f));
        LayoutElement leftLE = leftPanel.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 0.38f;
        leftLE.flexibleHeight = 1;
        GameObject listLabel = CreateText("ListLabel", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(200, 30), "Items", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f, 1f));
        GameObject itemContainer = CreatePanel("ItemContainer", leftPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 20), new Vector2(-10, -40), new Color(0, 0, 0, 0));
        VerticalLayoutGroup vlg = itemContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        itemContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Right: details
        GameObject rightPanel = CreatePanel("RightPanel", body.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Color(0, 0, 0, 0.25f));
        LayoutElement rightLE = rightPanel.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 0.62f;
        rightLE.flexibleHeight = 1;
        GameObject detailsLabel = CreateText("DetailsLabel", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(200, 30), "Details", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f, 1f));
        GameObject itemIcon = CreateImage("ItemIcon", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(100, 100));
        GameObject itemNameText = CreateText("ItemNameText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(360, 40), "Item Name", 28, TextAnchor.MiddleCenter, Color.white);
        GameObject itemDescriptionText = CreateText("ItemDescriptionText", rightPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(360, 150), controller.noItemSelectedMessage, 20, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f, 1f));
        TextMeshProUGUI descTmp = itemDescriptionText.GetComponent<TextMeshProUGUI>();
        descTmp.overflowMode = TextOverflowModes.Overflow;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        GameObject itemPriceText = CreateText("ItemPriceText", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(360, 40), "Price: --", 24, TextAnchor.MiddleCenter, Color.yellow);
        GameObject buyButton = CreateButton("BuyButton", rightPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(180, 45), controller.buyButtonText, controller.buyButtonColor, 24);

        // --- Footer ---
        GameObject footer = CreatePanel("Footer", shopPanel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 35), new Vector2(0, 70), new Color(0, 0, 0, 0.4f));
        GameObject sellButton = CreateButton("SellButton", footer.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(250, 50), controller.sellButtonText, controller.sellButtonColor, 22);

        // --- Tooltip ---
        GameObject tooltipPanel = CreatePanel("TooltipPanel", shopCanvasGO.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(250, 80), controller.tooltipColor);
        tooltipPanel.SetActive(false);
        GameObject tooltipText = CreateText("TooltipText", tooltipPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0), "Tooltip", 18, TextAnchor.MiddleCenter, Color.white);
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

        // Link Merchant_NPC to this controller
        GameObject merchant = GameObject.Find("Merchant_NPC");
        if (merchant != null)
        {
            ShopNPC shopNPC = merchant.GetComponent<ShopNPC>();
            if (shopNPC == null)
                shopNPC = Undo.AddComponent<ShopNPC>(merchant);
            shopNPC.shopUI = controller;
            EditorUtility.SetDirty(shopNPC);

            DialogueTrigger dt = merchant.GetComponent<DialogueTrigger>();
            if (dt != null)
            {
                dt.shopNPC = shopNPC;
                if (shopNPC.interactPrompt == null)
                    shopNPC.interactPrompt = dt.interactPrompt;
                EditorUtility.SetDirty(dt);
            }
        }

        Selection.activeGameObject = shopPanel;
        EditorUtility.DisplayDialog("Done", "Shop UI created under ShopCanvas (Foreground). Disabled by default. Merchant_NPC now uses this controller.", "OK");
    }

    static ShopUIController FindBestController()
    {
        ShopUIController[] controllers = FindObjectsByType<ShopUIController>();
        if (controllers.Length == 0) return null;
        if (controllers.Length == 1) return controllers[0];

        ShopUIController best = controllers[0];
        int bestScore = CountRefs(best);
        foreach (ShopUIController c in controllers)
        {
            int score = CountRefs(c);
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }
        return best;
    }

    static int CountRefs(ShopUIController c)
    {
        int count = 0;
        if (c.shopPanel != null) count++;
        if (c.itemContainer != null) count++;
        if (c.goldText != null) count++;
        if (c.itemNameText != null) count++;
        if (c.itemDescriptionText != null) count++;
        if (c.itemPriceText != null) count++;
        if (c.itemIcon != null) count++;
        if (c.buyButton != null) count++;
        if (c.sellButton != null) count++;
        if (c.closeButton != null) count++;
        if (c.tooltipPanel != null) count++;
        if (c.tooltipText != null) count++;
        return count;
    }

    static void DestroyOldShopObjects(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name == "ShopCanvas" || child.name == "ShopPanel")
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
            else
            {
                DestroyOldShopObjects(child);
            }
        }
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
