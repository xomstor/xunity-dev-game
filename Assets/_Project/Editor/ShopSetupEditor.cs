#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public class ShopSetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup Working Shop")]
    public static void ShowWindow()
    {
        GetWindow<ShopSetupEditor>("Shop Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Setup Working Shop", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Setup Shop in Scene", GUILayout.Height(40)))
        {
            SetupShop();
        }

        GUILayout.Space(10);
        GUILayout.Label("This will:", EditorStyles.wordWrappedLabel);
        GUILayout.Label("- Fix Merchant_NPC sprite if missing", EditorStyles.wordWrappedLabel);
        GUILayout.Label("- Add ShopManager to ShopCounter", EditorStyles.wordWrappedLabel);
        GUILayout.Label("- Add ShopNPC to Merchant_NPC", EditorStyles.wordWrappedLabel);
        GUILayout.Label("- Create ShopUIController in Canvas", EditorStyles.wordWrappedLabel);
        GUILayout.Label("- Assign items, references and prompt", EditorStyles.wordWrappedLabel);
    }

    static void SetupShop()
    {
        Undo.SetCurrentGroupName("Setup Working Shop");
        int group = Undo.GetCurrentGroup();

        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in scene.", "OK");
            return;
        }

        GameObject shopCounter = GameObject.Find("ShopCounter");
        if (shopCounter == null)
        {
            EditorUtility.DisplayDialog("Error", "ShopCounter GameObject not found.", "OK");
            return;
        }

        GameObject merchant = GameObject.Find("Merchant_NPC");
        if (merchant == null)
        {
            EditorUtility.DisplayDialog("Error", "Merchant_NPC GameObject not found.", "OK");
            return;
        }

        FixMerchantSprite(merchant);

        ShopManager shopManager = SetupShopManager(shopCounter);
        if (shopManager == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to set up ShopManager.", "OK");
            return;
        }

        ShopUIController controller = SetupShopUIController(canvas, shopManager);
        SetupShopNPC(merchant, controller);

        if (controller != null)
        {
            Selection.activeGameObject = controller.gameObject;
            EditorUtility.SetDirty(controller);
        }

        Undo.CollapseUndoOperations(group);
        EditorUtility.DisplayDialog("Success", "Shop setup complete. Press Play to test.", "OK");
    }

    static Canvas FindCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                return c;
        }
        return canvases.Length > 0 ? canvases[0] : null;
    }

    static ShopManager SetupShopManager(GameObject shopCounter)
    {
        ShopManager shopManager = shopCounter.GetComponent<ShopManager>();
        if (shopManager == null)
        {
            shopManager = Undo.AddComponent<ShopManager>(shopCounter);
        }

        string[] itemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Custom/Items" });
        System.Collections.Generic.List<ShopItem> itemList = new System.Collections.Generic.List<ShopItem>();

        for (int i = 0; i < itemGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(itemGuids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;
            if (IsSoulItem(item)) continue;

            itemList.Add(new ShopItem { itemData = item, price = item.price > 0 ? item.price : 1, quantity = -1 });
        }

        shopManager.shopItems = itemList.ToArray();
        EditorUtility.SetDirty(shopManager);
        return shopManager;
    }

    static bool IsSoulItem(ItemData item)
    {
        string[] keywords = new[] { "soul", "tail", "loot", "drop", "essence" };
        string name = item.itemName.ToLower();
        string id = item.itemId.ToLower();
        foreach (string keyword in keywords)
        {
            if (name.Contains(keyword) || id.Contains(keyword))
                return true;
        }
        return false;
    }

    static void FixMerchantSprite(GameObject merchant)
    {
        SpriteRenderer sr = merchant.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        string spritePath = "Assets/_Project/Custom/Merchant_NPC/Idle_spritesheet.png";
        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath))
            return;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().ToArray();
        if (sprites.Length == 0) return;

        Sprite bodySprite = System.Array.Find(sprites, s => s.name == "Idle_spritesheet_0") ?? sprites[0];
        if (sr.sprite != bodySprite)
        {
            Undo.RecordObject(sr, "Fix Merchant Sprite");
            sr.sprite = bodySprite;
            EditorUtility.SetDirty(sr);
        }

        Transform tr = merchant.transform;
        if (tr.localScale.x < 0.5f || tr.localScale.y < 0.5f)
        {
            Undo.RecordObject(tr, "Fix Merchant Scale");
            tr.localScale = new Vector3(2.5f, 3f, 1f);
            EditorUtility.SetDirty(tr);
        }
    }

    static void SetupShopNPC(GameObject merchant, ShopUIController controller)
    {
        ShopNPC shopNPC = merchant.GetComponent<ShopNPC>();
        if (shopNPC == null)
        {
            shopNPC = Undo.AddComponent<ShopNPC>(merchant);
        }
        shopNPC.shopUI = controller;

        DialogueTrigger dt = merchant.GetComponent<DialogueTrigger>();
        if (dt != null)
        {
            shopNPC.interactPrompt = dt.interactPrompt;
            dt.shopNPC = shopNPC;
            EditorUtility.SetDirty(dt);
        }

        EditorUtility.SetDirty(shopNPC);
    }

    static ShopUIController SetupShopUIController(Canvas canvas, ShopManager shopManager)
    {
        Transform existing = canvas.transform.Find("ShopUIController");
        GameObject controllerGO;
        if (existing != null)
        {
            controllerGO = existing.gameObject;
        }
        else
        {
            controllerGO = new GameObject("ShopUIController");
            Undo.RegisterCreatedObjectUndo(controllerGO, "Create ShopUIController");
            controllerGO.transform.SetParent(canvas.transform, false);
        }

        ShopUIController controller = controllerGO.GetComponent<ShopUIController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<ShopUIController>(controllerGO);
        }

        controller.shopManager = shopManager;
        controller.playerStats = FindAnyObjectByType<PlayerStats>();
        controller.playerInventory = FindAnyObjectByType<Inventory>();

        controller.soulItems = new System.Collections.Generic.List<ItemData>();
        string[] allItemGuids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Custom/Items" });
        foreach (string guid in allItemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && IsSoulItem(item))
                controller.soulItems.Add(item);
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }
}
#endif
