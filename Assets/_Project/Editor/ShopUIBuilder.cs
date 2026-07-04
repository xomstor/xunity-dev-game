#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ShopUIBuilder : EditorWindow
{
    [MenuItem("Tools/Build Shop UI")]
    public static void ShowWindow()
    {
        GetWindow<ShopUIBuilder>("Shop UI Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Rebuild Shop UI in the scene", EditorStyles.boldLabel);
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
            EditorUtility.DisplayDialog("Error", "No ShopUIController found in scene. Add one to a Canvas first.", "OK");
            return;
        }

        // Remove old auto-generated UI
        foreach (Transform child in controller.transform.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && (child.name == "ShopCanvas" || child.name == "ShopPanel"))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        controller.shopPanel = null;
        controller.categoryContainer = null;
        controller.cardContainer = null;
        controller.goldText = null;
        controller.itemNameText = null;
        controller.itemDescriptionText = null;
        controller.itemPriceText = null;
        controller.itemIcon = null;
        controller.buyButton = null;
        controller.sellButton = null;
        controller.closeButton = null;
        controller.prevButton = null;
        controller.nextButton = null;
        controller.pageText = null;
        controller.tooltipPanel = null;
        controller.tooltipText = null;

        controller.CreateShopUI();
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

        Selection.activeGameObject = controller.shopPanel;
        EditorUtility.DisplayDialog("Done", "Shop UI rebuilt with new card-style layout.", "OK");
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
        if (c.cardContainer != null) count++;
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
}
#endif
