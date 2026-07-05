#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ItemIconGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Item Icons")]
    public static void ShowWindow()
    {
        GetWindow<ItemIconGenerator>("Item Icon Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate colored square icons for all ItemData assets", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Icons"))
        {
            GenerateAllIcons();
        }
    }

    static void GenerateAllIcons()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null) continue;
            if (item.icon != null) continue;

            Texture2D tex = CreateColorTexture(item.GetRarityColor());
            string iconPath = path.Replace(".asset", "_icon.png");
            System.IO.File.WriteAllBytes(iconPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(iconPath);

            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null)
            {
                item.icon = sprite;
                EditorUtility.SetDirty(item);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Generated {count} item icons.");
    }

    static Texture2D CreateColorTexture(Color color)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
#endif
