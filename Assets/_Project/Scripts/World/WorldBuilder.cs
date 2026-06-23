using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldBuilder : MonoBehaviour
{
    [Header("Layout Settings")]
    public float levelWidth = 80f;
    public float levelHeight = 8f;
    public float hubHeight = 12f;
    public float gapBetweenLevels = 1f;
    public int levelCount = 5;

    [Header("Colors")]
    public Color hubColor = new Color(0.3f, 0.5f, 0.8f, 1f);
    public Color levelColorA = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color levelColorB = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color npcColor = new Color(1f, 0.8f, 0f, 1f);
    public Color exitColor = new Color(0f, 1f, 0.4f, 1f);
    public Color merchantColor = new Color(1f, 0.4f, 0f, 1f);

#if UNITY_EDITOR
    [ContextMenu("Build World Layout")]
    public void BuildWorldLayout()
    {
        ClearChildren();

        float startY = 0f;

        // Hub
        CreateZone("[ HUB - Магазин ]", new Vector2(0, startY), new Vector2(levelWidth, hubHeight), hubColor, "Hub");
        CreatePlaceholder("Merchant_NPC", new Vector2(-levelWidth * 0.35f, startY), new Vector2(1.5f, 2.5f), merchantColor, "Hub");
        CreatePlaceholder("ShopCounter", new Vector2(-levelWidth * 0.3f, startY - 1f), new Vector2(4f, 1f), new Color(0.6f, 0.4f, 0.2f), "Hub");
        CreateExitTrigger("EnterLevel_1", new Vector2(levelWidth * 0.45f, startY), "Hub");
        CreateSpawnPoint("SpawnPoint_Hub", new Vector2(-levelWidth * 0.45f, startY));

        float currentY = startY - hubHeight * 0.5f - gapBetweenLevels;

        for (int i = 1; i <= levelCount; i++)
        {
            float centerY = currentY - levelHeight * 0.5f;
            Color col = (i % 2 == 0) ? levelColorA : levelColorB;
            string levelName = "Level_" + i;

            CreateZone($"[ Уровень {i} ]", new Vector2(0, centerY), new Vector2(levelWidth, levelHeight), col, levelName);

            // NPC в начале уровня (слева)
            CreatePlaceholder($"NPC_Level{i}", new Vector2(-levelWidth * 0.45f, centerY), new Vector2(1f, 1.8f), npcColor, levelName);

            // Враги (3 штуки по уровню)
            for (int e = 0; e < 3; e++)
            {
                float ex = Mathf.Lerp(-levelWidth * 0.2f, levelWidth * 0.3f, e / 2f);
                CreatePlaceholder($"Enemy_L{i}_{e + 1}", new Vector2(ex, centerY), new Vector2(1f, 1f), Color.red, levelName);
            }

            // Спавн поинт в начале уровня
            CreateSpawnPoint($"SpawnPoint_Level{i}", new Vector2(-levelWidth * 0.4f, centerY));

            // Выход в следующий уровень (справа)
            if (i < levelCount)
                CreateExitTrigger($"EnterLevel_{i + 1}", new Vector2(levelWidth * 0.45f, centerY), levelName);
            else
                CreatePlaceholder("BOSS_ZONE", new Vector2(levelWidth * 0.35f, centerY), new Vector2(2f, 2f), Color.magenta, levelName);

            currentY -= levelHeight + gapBetweenLevels;
        }

        Debug.Log("World layout built!");
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    private GameObject CreateZone(string label, Vector2 pos, Vector2 size, Color color, string parentName)
    {
        GameObject zone = new GameObject($"Zone_{parentName}");
        zone.transform.SetParent(transform);
        zone.transform.position = new Vector3(pos.x, pos.y, 0);

        SpriteRenderer sr = zone.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = new Color(color.r, color.g, color.b, 0.15f);
        sr.sortingOrder = -10;
        zone.transform.localScale = new Vector3(size.x, size.y, 1);

        BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Label
        GameObject labelObj = new GameObject("Label_" + label);
        labelObj.transform.SetParent(zone.transform);
        labelObj.transform.localPosition = new Vector3(-0.4f, 0.4f, 0);
        labelObj.transform.localScale = new Vector3(1f / size.x * 8f, 1f / size.y * 8f, 1);

        return zone;
    }

    private void CreatePlaceholder(string name, Vector2 pos, Vector2 size, Color color, string parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.localScale = new Vector3(size.x, size.y, 1);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = color;
        sr.sortingOrder = 0;

        obj.AddComponent<BoxCollider2D>();
    }

    private void CreateExitTrigger(string name, Vector2 pos, string parent)
    {
        GameObject obj = new GameObject("Exit_" + name);
        obj.transform.SetParent(transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.localScale = new Vector3(2f, 4f, 1);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = new Color(exitColor.r, exitColor.g, exitColor.b, 0.6f);
        sr.sortingOrder = 1;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void CreateSpawnPoint(string name, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.tag = "Respawn";

        // Визуальный маркер (маленький зелёный ромб)
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = new Color(0f, 1f, 0.8f, 0.9f);
        sr.sortingOrder = 2;
        obj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        obj.transform.rotation = Quaternion.Euler(0, 0, 45f);
    }

    private Sprite GetSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
#endif
}
