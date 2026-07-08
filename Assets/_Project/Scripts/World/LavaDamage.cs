using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
public class LavaDamage : MonoBehaviour
{
    [Header("Урон")]
    public int damagePerTick = 20;          // Урон за одно "тик"
    public float tickInterval = 0.5f;       // Как часто наносить (сек)
    public bool instantKill = false;        // Мгновенная смерть?

    [Header("Респаун")]
    public Transform respawnPoint;          // Точка респауна (создайте пустой объект!)
    public float respawnDelay = 2f;         // Задержка перед респауном

    [Header("Эффекты (опционально)")]
    public ParticleSystem steamVFX;         // Пар/дым
    public AudioClip sizzleSFX;            // Звук шипения

    [Header("Движение лавы")]
    public bool enableLavaRise = true;      // Включить поднятие лавы?
    public float riseHeight = 3f;           // На сколько поднимается лава
    public float riseSpeed = 2f;            // Скорость поднятия
    public float stayUpDuration = 3f;       // Как долго лава остаётся поднятой
    public float fallSpeed = 1.5f;          // Скорость опускания
    public float cycleDuration = 8f;        // Полный цикл (поднятие + опускание)

    private float damageTimer;
    private GameObject playerInLava;
    private Vector3 startPosition;
    private float lavaTimer;
    private bool isRising;
    private bool isAtTop;

    void Start()
    {
        startPosition = transform.position;
        isRising = true;
        isAtTop = false;
        lavaTimer = 0f;
    }

    void Update()
    {
        // Движение лавы вверх и вниз
        if (enableLavaRise)
        {
            UpdateLavaPosition();
        }

        // Наносим периодический урон если игрок в лаве
        if (playerInLava != null && !instantKill)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= tickInterval)
            {
                ApplyDamageToPlayer(playerInLava);
                damageTimer = 0f;
            }
        }
    }

    void UpdateLavaPosition()
    {
        lavaTimer += Time.deltaTime;
        Vector3 currentPos = transform.position;
        float targetY = startPosition.y;

        if (isRising)
        {
            // Поднимаем лаву
            targetY = startPosition.y + riseHeight;
            currentPos.y = Mathf.Lerp(startPosition.y, targetY, lavaTimer * riseSpeed / riseHeight);
            transform.position = currentPos;

            // Проверяем достигли ли верхнюю точку
            if (currentPos.y >= targetY - 0.1f)
            {
                isRising = false;
                isAtTop = true;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава поднялась!");
            }
        }
        else if (isAtTop)
        {
            // Ждём на верхней точке
            if (lavaTimer >= stayUpDuration)
            {
                isAtTop = false;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава начинает опускаться!");
            }
        }
        else
        {
            // Опускаем лаву
            targetY = startPosition.y;
            currentPos.y = Mathf.Lerp(startPosition.y + riseHeight, targetY, lavaTimer * fallSpeed / riseHeight);
            transform.position = currentPos;

            // Проверяем достигли ли нижнюю точку
            if (currentPos.y <= targetY + 0.1f)
            {
                isRising = true;
                lavaTimer = 0f;
                Debug.Log("🌊 Лава опустилась!");
            }
        }
    }

    // Игрок вошёл в лаву
    void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null) // Нашли PlayerStats = это игрок!
        {
            EnterLava(other.gameObject, stats);
        }
    }

    // Игрок вышел из лавы
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == playerInLava)
        {
            ExitLava();
        }
    }

    void EnterLava(GameObject player, PlayerStats stats)
    {
        playerInLava = player;
        damageTimer = 0f;

        Debug.Log($"🔥 {player.name} упал в лаву! HP: {stats.hp}/{stats.maxHp}");

        // Эффекты входа
        SpawnSteamEffect(player.transform.position);
        PlaySizzleSound(player.transform.position);

        if (instantKill)
        {
            // Мгновенная смерть
            KillPlayer(player, stats);
        }
        else
        {
            // Первый урон сразу
            ApplyDamageToPlayer(player);
        }
    }

    void ExitLava()
    {
        if (playerInLava != null)
        {
            Debug.Log($"💨 {playerInLava.name} выбрался из лавы!");
        }
        playerInLava = null;
        damageTimer = 0f;
    }

    void ApplyDamageToPlayer(GameObject player)
    {
        if (player == null) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        // Проверяем не мёртв ли уже
        if (stats.hp <= 0) return;

        // Наносим урон через PlayerStats.TakeDamage!
        stats.TakeDamage(damagePerTick);

        Debug.Log($"💥 Лава нанесла {damagePerTick} урона! HP осталось: {stats.hp}/{stats.maxHp}");

        // Эффекты урона
        SpawnSteamEffect(player.transform.position);

        // Проверяем смерть
        if (stats.hp <= 0)
        {
            KillPlayer(player, stats);
        }
    }

    void KillPlayer(GameObject player, PlayerStats stats)
    {
        Debug.Log($"☠️ {player.name} СГОРЕЛ В ЛАВЕ! Применяем штраф...");

        // Штраф за смерть (из PlayerStats)
        stats.ApplyDeathPenalty();

        // Отключаем игрока визуально
        player.SetActive(false);

        // Эффекты смерти
        PlayDeathEffects(player.transform.position);

        // Показываем промпт смерти
        ShowDeathPrompt(player, stats);
    }

    void ShowDeathPrompt(GameObject player, PlayerStats stats)
    {
        // Создаём UI для промпта
        GameObject canvasGO = new GameObject("DeathPromptCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;

        // Полупрозрачный фон
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Панель с кнопками
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        RectTransform panelRt = panelGO.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600, 300);

        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30;
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        titleGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "☠️ ВЫ ПОГИБЛИ";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.2f, 0.2f, 1f);
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.sizeDelta = new Vector2(0, 80);

        // Кнопка "Воскреснуть"
        GameObject resurrectBtnGO = new GameObject("ResurrectButton");
        resurrectBtnGO.transform.SetParent(panelGO.transform, false);
        resurrectBtnGO.AddComponent<CanvasRenderer>();
        Image resurrectImg = resurrectBtnGO.AddComponent<Image>();
        resurrectImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        Button resurrectBtn = resurrectBtnGO.AddComponent<Button>();
        resurrectBtn.targetGraphic = resurrectImg;
        RectTransform resurrectRt = resurrectBtnGO.GetComponent<RectTransform>();
        resurrectRt.sizeDelta = new Vector2(0, 70);

        GameObject resurrectTextGO = new GameObject("Text");
        resurrectTextGO.transform.SetParent(resurrectBtnGO.transform, false);
        resurrectTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI resurrectText = resurrectTextGO.AddComponent<TextMeshProUGUI>();
        resurrectText.text = "Воскреснуть в городе";
        resurrectText.fontSize = 36;
        resurrectText.alignment = TextAlignmentOptions.Center;
        resurrectText.color = Color.black;
        RectTransform resurrectTextRt = resurrectTextGO.GetComponent<RectTransform>();
        resurrectTextRt.anchorMin = Vector2.zero;
        resurrectTextRt.anchorMax = Vector2.one;
        resurrectTextRt.offsetMin = Vector2.zero;
        resurrectTextRt.offsetMax = Vector2.zero;

        resurrectBtn.onClick.AddListener(() =>
        {
            Destroy(canvasGO);
            ResurrectInCity(player, stats);
        });

        // Кнопка "Загрузить"
        GameObject loadBtnGO = new GameObject("LoadButton");
        loadBtnGO.transform.SetParent(panelGO.transform, false);
        loadBtnGO.AddComponent<CanvasRenderer>();
        Image loadImg = loadBtnGO.AddComponent<Image>();
        loadImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        Button loadBtn = loadBtnGO.AddComponent<Button>();
        loadBtn.targetGraphic = loadImg;
        RectTransform loadRt = loadBtnGO.GetComponent<RectTransform>();
        loadRt.sizeDelta = new Vector2(0, 70);

        GameObject loadTextGO = new GameObject("Text");
        loadTextGO.transform.SetParent(loadBtnGO.transform, false);
        loadTextGO.AddComponent<CanvasRenderer>();
        TextMeshProUGUI loadText = loadTextGO.AddComponent<TextMeshProUGUI>();
        loadText.text = "Загрузить сохранение";
        loadText.fontSize = 36;
        loadText.alignment = TextAlignmentOptions.Center;
        loadText.color = Color.black;
        RectTransform loadTextRt = loadTextGO.GetComponent<RectTransform>();
        loadTextRt.anchorMin = Vector2.zero;
        loadTextRt.anchorMax = Vector2.one;
        loadTextRt.offsetMin = Vector2.zero;
        loadTextRt.offsetMax = Vector2.zero;

        loadBtn.onClick.AddListener(() =>
        {
            Destroy(canvasGO);
            OpenLoadMenu();
        });

        Time.timeScale = 0f; // Паузим игру
    }

    void ResurrectInCity(GameObject player, PlayerStats stats)
    {
        Time.timeScale = 1f; // Возобновляем игру
        
        // Телепортируем в город
        if (respawnPoint != null)
        {
            player.SetActive(true);
            player.transform.position = respawnPoint.position;
            stats.hp = stats.maxHp;
            Debug.Log($"🔄 {player.name} воскрешён в городе!");
        }
    }

    void OpenLoadMenu()
    {
        Time.timeScale = 1f; // Возобновляем игру
        
        // Открываем меню загрузки через PauseMenu
        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>();
        if (pauseMenu != null)
        {
            pauseMenu.OpenSavePanel();
        }
        else
        {
            Debug.LogWarning("PauseMenu не найден!");
        }
    }

    IEnumerator RespawnPlayer(GameObject player, PlayerStats stats)
    {
        yield return new WaitForSeconds(respawnDelay);

        // Перемещаем в точку респауна
        player.transform.position = respawnPoint.position;

        // Восстанавливаем здоровье
        stats.hp = stats.maxHp;

        // Показываем игрока
        player.SetActive(true);

        Debug.Log($"🔄 {player.name} возродился! HP восстановлено: {stats.hp}/{stats.maxHp}");
    }

    #region EFFECTS

    void SpawnSteamEffect(Vector3 position)
    {
        if (steamVFX == null) return;

        var ps = Instantiate(steamVFX, position, Quaternion.identity);
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    void PlaySizzleSound(Vector3 position)
    {
        if (sizzleSFX == null) return;

        AudioSource.PlayClipAtPoint(sizzleSFX, position);
    }

    void PlayDeathEffects(Vector3 position)
    {
        Debug.Log("💀 Эффекты смерти от лавы (можно добавить взрыв и т.д.)");
        // Здесь можно добавить:
        // - Взрывную анимацию
        // - Звук смерти
        // - Экранная тряска (Screen Shake)
    }

    #endregion

    #region DEBUG

    // Рисуем зону лавы в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.5f); // Оранжево-красный полупрозрачный

        var collider = GetComponent<Collider2D>();
        if (collider is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }

        // Рисуем иконку в Scene view
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, "🔥 LAVA");
#endif
    }

    #endregion
}