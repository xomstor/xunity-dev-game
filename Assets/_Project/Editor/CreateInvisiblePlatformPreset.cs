#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CreateInvisiblePlatformPreset
{
    [MenuItem("Tools/Create Invisible Platform Preset")]
    public static void Create()
    {
        GameObject go = new GameObject("InvisiblePlatform");

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2f, 0.2f);
        box.usedByEffector = true;

        PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 1f, 0.5f, 0.25f);
        sr.sortingOrder = -100;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            go.layer = groundLayer;

        go.AddComponent<InvisiblePlatform>();

        string prefabPath = "Assets/_Project/Prefabs/InvisiblePlatform.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log($"InvisiblePlatform prefab saved to {prefabPath}");
    }
}
#endif
