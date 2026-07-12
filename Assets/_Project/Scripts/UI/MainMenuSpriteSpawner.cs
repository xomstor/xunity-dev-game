using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuSpriteSpawner : MonoBehaviour
{
    [Header("Resources")]
    public string spritesPath = "Custom/MainMenu/Sprites";
    public string particlesPath = "Custom/MainMenu/Particles";

    [Header("Spawn Limits")]
    public int maxCharacters = 2;
    public int maxParticles = 10;

    [Header("Spawn Intervals")]
    public float characterSpawnInterval = 2.2f;
    public float particleSpawnInterval = 0.35f;

    [Header("Character Movement")]
    public float characterApproachDuration = 1.2f;
    public float characterHoverDuration = 0.6f;
    public float characterExitDuration = 1.0f;
    public float characterSideOffset = 0.55f;
    public float characterVerticalSpread = 0.35f;
    public float characterExitExtra = 0.45f;
    public float characterBottomSpawnOffset = 0.6f;

    [Header("Particle Movement")]
    public float particleMinSpeed = 140f;
    public float particleMaxSpeed = 300f;
    public float particleMinLifetime = 2f;
    public float particleMaxLifetime = 4.5f;
    public float particleHorizontalSpread = 0.75f;
    public float particleTopReach = 0.55f;
    public float particleBottomSpawnOffset = 0.6f;

    [Header("Visual")]
    public float characterScale = 1f;
    public float particleMinScale = 0.25f;
    public float particleMaxScale = 0.7f;
    public Color characterColor = new Color(1f, 1f, 1f, 0.95f);
    public Color nearParticleColor = new Color(1f, 1f, 1f, 0.85f);
    public Color farParticleColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
    public float particleRotationMin = -200f;
    public float particleRotationMax = 200f;
    public float characterExitRotationSpeed = 360f;
    public float particleFadeStart = 0.75f;

    private Canvas canvas;
    private Transform container;
    private Sprite[] characterSprites;
    private Sprite[] particleSprites;

    private float characterTimer;
    private float particleTimer;
    private readonly List<FlyingObject> activeObjects = new List<FlyingObject>();

    class FlyingObject
    {
        public RectTransform rectTransform;
        public Image image;
        public bool isCharacter;

        public float elapsed;
        public Color startColor;
        public float startScale;
        public float rotationSpeed;
        public float currentRotation;

        public Vector2 startPosition;
        public Vector2 hoverPosition;
        public Vector2 exitPosition;
        public float approachDuration;
        public float hoverDuration;
        public float exitDuration;

        public Vector2 particleTarget;
        public float lifetime;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        CreateContainer();
        LoadSprites();
    }

    void CreateContainer()
    {
        GameObject go = new GameObject("FlyingSpritesContainer", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        container = go.transform;
        container.SetAsFirstSibling();
    }

    void LoadSprites()
    {
        characterSprites = Resources.LoadAll<Sprite>(spritesPath);
        particleSprites = Resources.LoadAll<Sprite>(particlesPath);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        characterTimer += dt;
        particleTimer += dt;

        if (characterTimer >= characterSpawnInterval && CountCharacters() < maxCharacters && characterSprites.Length > 0)
        {
            characterTimer = 0f;
            Spawn(true);
        }

        if (particleTimer >= particleSpawnInterval && CountParticles() < maxParticles && particleSprites.Length > 0)
        {
            particleTimer = 0f;
            Spawn(false);
        }

        UpdateObjects(dt);
        CleanupObjects();
    }

    int CountCharacters()
    {
        int count = 0;
        foreach (var obj in activeObjects)
            if (obj.isCharacter) count++;
        return count;
    }

    int CountParticles()
    {
        int count = 0;
        foreach (var obj in activeObjects)
            if (!obj.isCharacter) count++;
        return count;
    }

    void Spawn(bool isCharacter)
    {
        Sprite[] pool = isCharacter ? characterSprites : particleSprites;
        if (pool.Length == 0) return;

        Sprite sprite = pool[Random.Range(0, pool.Length)];
        GameObject go = new GameObject(isCharacter ? "MenuCharacter" : "MenuParticle", typeof(RectTransform));
        go.transform.SetParent(container, false);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.SetNativeSize();
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float halfWidth = canvas.pixelRect.width * 0.5f;
        float halfHeight = canvas.pixelRect.height * 0.5f;
        float direction = Random.value > 0.5f ? 1f : -1f;

        if (isCharacter)
        {
            float startX = Random.Range(-halfWidth * 0.1f, halfWidth * 0.1f);
            float startY = -halfHeight * (1f + characterBottomSpawnOffset);
            Vector2 start = new Vector2(startX, startY);

            float hoverX = direction * halfWidth * characterSideOffset;
            float hoverY = Random.Range(-halfHeight * characterVerticalSpread, halfHeight * characterVerticalSpread);
            Vector2 hover = new Vector2(hoverX, hoverY);

            float exitX = direction * halfWidth * (characterSideOffset + characterExitExtra);
            Vector2 exit = new Vector2(exitX, hoverY);

            rt.localScale = new Vector3(characterScale, characterScale, 1f);
            img.color = characterColor;

            activeObjects.Add(new FlyingObject
            {
                rectTransform = rt,
                image = img,
                isCharacter = true,
                startPosition = start,
                hoverPosition = hover,
                exitPosition = exit,
                approachDuration = characterApproachDuration,
                hoverDuration = characterHoverDuration,
                exitDuration = characterExitDuration,
                elapsed = 0f,
                startColor = characterColor,
                startScale = characterScale,
                rotationSpeed = 0f,
                currentRotation = 0f
            });

            rt.anchoredPosition = start;
        }
        else
        {
            float startX = Random.Range(-halfWidth * 0.15f, halfWidth * 0.15f);
            float startY = -halfHeight * (1f + particleBottomSpawnOffset);
            Vector2 start = new Vector2(startX, startY);

            float targetX = direction * Random.Range(halfWidth * 0.2f, halfWidth * (0.2f + particleHorizontalSpread));
            float targetY = halfHeight * particleTopReach;
            Vector2 target = new Vector2(targetX, targetY);

            float depth = Random.value;
            float scale = Mathf.Lerp(particleMaxScale, particleMinScale, depth);
            rt.localScale = new Vector3(scale, scale, 1f);

            Color color = Color.Lerp(nearParticleColor, farParticleColor, depth);
            img.color = color;

            float speed = Mathf.Lerp(particleMaxSpeed, particleMinSpeed, depth);
            float distance = Vector2.Distance(start, target);
            float lifetime = distance / Mathf.Max(1f, speed);
            lifetime = Mathf.Clamp(lifetime, particleMinLifetime, particleMaxLifetime);

            activeObjects.Add(new FlyingObject
            {
                rectTransform = rt,
                image = img,
                isCharacter = false,
                startPosition = start,
                particleTarget = target,
                lifetime = lifetime,
                elapsed = 0f,
                startColor = color,
                startScale = scale,
                rotationSpeed = Random.Range(particleRotationMin, particleRotationMax),
                currentRotation = Random.Range(0f, 360f)
            });

            rt.anchoredPosition = start;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }
    }

    void UpdateObjects(float dt)
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            var obj = activeObjects[i];
            obj.elapsed += dt;

            if (obj.isCharacter)
            {
                UpdateCharacter(obj, dt);
                if (obj.elapsed >= obj.approachDuration + obj.hoverDuration + obj.exitDuration)
                {
                    Destroy(obj.rectTransform.gameObject);
                    activeObjects.RemoveAt(i);
                }
            }
            else
            {
                UpdateParticle(obj, dt);
                if (obj.elapsed >= obj.lifetime)
                {
                    Destroy(obj.rectTransform.gameObject);
                    activeObjects.RemoveAt(i);
                }
            }
        }
    }

    void UpdateCharacter(FlyingObject obj, float dt)
    {
        float t = obj.elapsed;
        Vector2 pos;

        if (t < obj.approachDuration)
        {
            float p = Mathf.SmoothStep(0, 1, t / obj.approachDuration);
            pos = Vector2.Lerp(obj.startPosition, obj.hoverPosition, p);
        }
        else if (t < obj.approachDuration + obj.hoverDuration)
        {
            pos = obj.hoverPosition;
            obj.currentRotation = 0f;
        }
        else
        {
            float exitT = t - obj.approachDuration - obj.hoverDuration;
            float p = Mathf.SmoothStep(0, 1, exitT / obj.exitDuration);
            pos = Vector2.Lerp(obj.hoverPosition, obj.exitPosition, p);

            obj.currentRotation += characterExitRotationSpeed * dt;
            if (obj.currentRotation >= 360f) obj.currentRotation -= 360f;
        }

        obj.rectTransform.anchoredPosition = pos;
        obj.rectTransform.localRotation = Quaternion.Euler(0, 0, obj.currentRotation);

        float total = obj.approachDuration + obj.hoverDuration + obj.exitDuration;
        float fade = 1f;
        float fadePoint = total * particleFadeStart;
        if (obj.elapsed > fadePoint)
            fade = 1f - Mathf.InverseLerp(fadePoint, total, obj.elapsed);

        Color c = obj.startColor;
        c.a *= fade;
        obj.image.color = c;
    }

    void UpdateParticle(FlyingObject obj, float dt)
    {
        float t = Mathf.Clamp01(obj.elapsed / obj.lifetime);
        float smooth = Mathf.SmoothStep(0, 1, t);
        obj.rectTransform.anchoredPosition = Vector2.Lerp(obj.startPosition, obj.particleTarget, smooth);

        obj.currentRotation += obj.rotationSpeed * dt;
        obj.rectTransform.localRotation = Quaternion.Euler(0, 0, obj.currentRotation);

        float fade = 1f;
        if (t > particleFadeStart)
            fade = 1f - Mathf.InverseLerp(particleFadeStart, 1f, t);

        Color c = obj.startColor;
        c.a *= fade;
        obj.image.color = c;
    }

    void CleanupObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i].rectTransform == null)
                activeObjects.RemoveAt(i);
        }
    }

    void OnDestroy()
    {
        foreach (var obj in activeObjects)
        {
            if (obj.rectTransform != null)
                Destroy(obj.rectTransform.gameObject);
        }
        activeObjects.Clear();
    }
}
