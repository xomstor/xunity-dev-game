#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FixHealerPotionIcon
{
    [MenuItem("Tools/Fix HealerPotion Icon")]
    static void Fix()
    {
        ItemData potion = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Custom/Items/HealerPotion.asset");
        if (potion == null)
        {
            Debug.LogError("HealerPotion.asset not found!");
            return;
        }

        // Load all sub-assets from the PNG to find the Sprite
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Custom/Items/HealerPotion.png");
        Sprite sprite = null;
        foreach (Object sub in subAssets)
        {
            if (sub is Sprite s)
            {
                sprite = s;
                break;
            }
        }

        if (sprite == null)
        {
            Debug.LogError("No Sprite found in HealerPotion.png! Sub-assets: " + subAssets.Length);
            foreach (Object sub in subAssets)
                Debug.Log($"  - {sub?.GetType().Name}: {sub?.name}");
            return;
        }

        Debug.Log($"Found Sprite: {sprite.name}, fileID likely correct. Assigning to HealerPotion.asset");
        potion.icon = sprite;
        EditorUtility.SetDirty(potion);
        AssetDatabase.SaveAssets();
        Debug.Log("HealerPotion icon fixed!");
    }
}
#endif
