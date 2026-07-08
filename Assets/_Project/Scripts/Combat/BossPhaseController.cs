using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Контроллер фаз босса для AutoCombat
/// Изменяет поведение босса при определённом HP
/// </summary>
public class BossPhaseController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент AutoCombat на боссе")]
    public AutoCombat bossCombat;

    [Header("Фазы боя")]
    [Tooltip("Настраиваются от последней к первой (Ярость → Агрессия → Спокойствие)")]
    public Phase[] phases;

    [Header("События")]
    public UnityEvent OnPhaseChange;      // При смене фазы
    public UnityEvent OnBattleStart;      // Начало боя
    public UnityEvent OnBossDefeated;     // Победа над боссом

    // Внутреннее состояние
    private int currentPhaseIndex = -1;
    private Phase currentPhase;
    private bool battleStarted = false;
    private bool bossDefeated = false;

    /// <summary>
    /// Настройки одной фазы
    /// </summary>
    [System.Serializable]
    public class Phase
    {
        [Header("Информация")]
        [Tooltip("Название для логов")]
        public string phaseName = "Новая Фаза";

        [Tooltip("Активируется когда HP <= этого % (0.3 = 30%)")]
        public float hpThreshold = 0.5f;

        [Header("⚔️ Combat настройки")]
        [Tooltip("Множитель кулдауна атак\n(<1 = атакует ЧАЩЕ, >1 = реже)")]
        [Range(0.3f, 3f)]
        public float attackCooldownMult = 1f;

        [Tooltip("Множитель урона\n(1.5 = +50% урона)")]
        [Range(0.5f, 3f)]
        public float damageMultiplier = 1f;

        [Tooltip("Множитель дистанции атаки\n(1.5 = бьёт издалека)")]
        [Range(0.5f, 3f)]
        public float attackRangeMult = 1f;

        [Header("🏃 Движение")]
        [Tooltip("Множитель скорости\n(1.5 = на 50% быстрее)")]
        [Range(0.5f, 2.5f)]
        public float speedMultiplier = 1f;

        [Tooltip("Множитель радиуса обнаружения\n(1.5 = видит дальше)")]
        [Range(0.5f, 2f)]
        public float detectionRadiusMult = 1f;

        [Header("🦘 Прыжковые атаки")]
        [Tooltip("Разрешить прыжковые атаки в этой фазе?")]
        public bool enableJumpAttacks = true;

        [Tooltip("Шанс прыжковой атаки (0.0 - 1.0)\n(0.3 = 30% шанс)")]
        [Range(0f, 1f)]
        public float jumpAttackChance = 0.05f;

        [Header("🎨 Визуальные эффекты")]
        [Tooltip("Цвет подсветки босса")]
        public Color glowColor = Color.white;

        [Tooltip("Тряска камеры при входе в фазу?")]
        public bool screenShakeOnEnter = false;

        [Tooltip("Сила тряски (если включена)")]
        [Range(0.1f, 2f)]
        public float shakeIntensity = 0.5f;

        [Header("🔊 Звук")]
        [Tooltip("Звук при входе в фазу")]
        public AudioClip phaseStartSound;

        [Tooltip("Громкость звука (0-1)")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;
    }

    // ═══════════════════════════════════════
    // ИНИЦИАЛИЗАЦИЯ
    // ═══════════════════════════════════════

    void Awake()
    {
        if (bossCombat == null)
            bossCombat = GetComponent<AutoCombat>();
    }

    void Start()
    {
        if (bossCombat == null)
        {
            Debug.LogError("❌ BossPhaseController: AutoCombat не найден!", this);
            enabled = false;
            return;
        }

        // Сортируем фазы: от высокой к низкой (1.0 → 0.6 → 0.3)
        if (phases != null && phases.Length > 0)
        {
            System.Array.Sort(phases, (a, b) => a.hpThreshold.CompareTo(b.hpThreshold));
            System.Array.Reverse(phases);

            LogSetup();
        }
        else
        {
            Debug.LogWarning("⚠️ Фазы не настроены!", this);
        }
    }

    void LogSetup()
    {
        Debug.Log($"═".PadRight(50, '═'), this);
        Debug.Log($"✅ BOSS PHASE CONTROLLER ИНИЦИАЛИЗИРОВАН", this);
        Debug.Log($"═".PadRight(50, '═'), this);
        Debug.Log($"📊 Кол-во фаз: {phases.Length}", this);
        Debug.Log($"❤️ MaxHP босса: {bossCombat.maxHealth}", this);
        Debug.Log($"⚔️ Базовый урон: {bossCombat.damage}", this);
        Debug.Log($"🏃 Базовая скорость: {bossCombat.moveSpeed}", this);
        Debug.Log($"⏱️ Базовый кулдаун: {bossCombat.attackCooldown} сек", this);
        Debug.Log($"👁️ Радиус обзора: {bossCombat.detectionRadius}", this);
        Debug.Log($"═".PadRight(50, '═'), this);
    }
    // ═══════════════════════════════════════
    // ОБНОВЛЕНИЕ
    // ═══════════════════════════════════════

    void Update()
    {
        if (!battleStarted || bossDefeated || bossCombat == null) return;
        if (bossCombat.IsDead)
        {
            DefeatBoss();
            return;
        }

        CheckPhaseTransition();
    }

    void CheckPhaseTransition()
    {
        if (phases == null || phases.Length == 0) return;

        float hpPercent = CalculateHPPercent();  // ← ИЗМЕНЕНО ЗДЕСЬ!

        for (int i = 0; i < phases.Length; i++)
        {
            if (hpPercent <= phases[i].hpThreshold)
            {
                if (i != currentPhaseIndex)
                {
                    EnterPhase(i, hpPercent);
                }
                return;
            }
        }
    }

    /// <summary> Рассчитать % HP (внутренний метод) </summary>
    float CalculateHPPercent()  // ← ПЕРЕИМЕНОВАНО!
    {
        if (bossCombat.maxHealth <= 0) return 0f;
        return (float)bossCombat.CurrentHealth / bossCombat.maxHealth;
    }

    // ═══════════════════════════════════════
    // ФАЗЫ
    // ═══════════════════════════════════════

    void EnterPhase(int index, float hpPercent)
    {
        currentPhaseIndex = index;
        currentPhase = phases[index];

        // Логирование
        Debug.Log($"═".PadRight(50, '═'), this);
        Debug.Log($"⚔️═".PadRight(50, '═'), this);
        Debug.Log($"⚔️ ФАЗА: {currentPhase.phaseName.ToUpper()}", this);
        Debug.Log($"❤️ HP: {bossCombat.CurrentHealth}/{bossCombat.maxHealth} ({hpPercent * 100:F1}%)", this);
        Debug.Log($"═".PadRight(50, '═'), this);

        // Применяем модификаторы
        ApplyAllModifiers();

        // Эффекты
        PlaySound();
        ApplyVisuals();

        // Событие
        OnPhaseChange?.Invoke();
    }

    void ApplyAllModifiers()
    {
        if (currentPhase == null || bossCombat == null) return;

        Debug.Log($"📊 Применяю модификаторы:", this);

        // 1. Кулдаун атак
        ModifyPublicField("attackCooldown", currentPhase.attackCooldownMult, "⏱️ Кулдаун");

        // 2. Урон
        ModifyPublicField("damage", currentPhase.damageMultiplier, "💪 Урон");

        // 3. Скорость
        ModifyPublicField("moveSpeed", currentPhase.speedMultiplier, "🏃 Скорость");

        // 4. Радиус обнаружения
        ModifyPublicField("detectionRadius", currentPhase.detectionRadiusMult, "👁️ Обзор");

        // 5. Дистанция атаки
        ModifyPublicField("attackRange", currentPhase.attackRangeMult, "⚔️ Дист. атаки");

        // 6. Прыжковые атаки
        bossCombat.canJumpAttack = currentPhase.enableJumpAttacks;
        bossCombat.jumpAttackChance = currentPhase.jumpAttackChance;
        Debug.Log($"  🦘 Прыжки: {(currentPhase.enableJumpAttacks ? "ВКЛ" : "ВЫКЛ")} (шанс: {currentPhase.jumpAttackChance:P0})", this);

        Debug.Log($"✅ Модификаторы применены!", this);
    }

    /// <summary>
    /// Изменяет публичное поле AutoCombat с логированием
    /// </summary>
    void ModifyPublicField(string fieldName, float multiplier, string label)
    {
        if (Mathf.Approximately(multiplier, 1f)) return; // Пропускаем если x1

        var field = typeof(AutoCombat).GetField(fieldName);
        if (field == null)
        {
            Debug.LogWarning($"  ⚠️ Поле {fieldName} не найдено!", this);
            return;
        }

        object value = field.GetValue(bossCombat);
        float oldValue = 0f;
        float newValue = 0f;

        if (value is int intValue)
        {
            oldValue = intValue;
            newValue = Mathf.RoundToInt(intValue * multiplier);
            field.SetValue(bossCombat, (int)newValue);
        }
        else if (value is float floatValue)
        {
            oldValue = floatValue;
            newValue = floatValue * multiplier;
            field.SetValue(bossCombat, newValue);
        }
        else
        {
            return;
        }

        Debug.Log($"  {label}: {oldValue:F2} → {newValue:F2} (x{multiplier})", this);
    }

    void PlaySound()
    {
        if (currentPhase?.phaseStartSound == null) return;

        AudioSource.PlayClipAtPoint(
            currentPhase.phaseStartSound,
            transform.position,
            currentPhase.soundVolume
        );
    }

    void ApplyVisuals()
    {
        if (currentPhase == null) return;

        // Цвет спрайта
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = currentPhase.glowColor;
        }

        // Screen Shake
        if (currentPhase.screenShakeOnEnter)
        {
            var camShake = Camera.main?.GetComponent<CameraShake>();
            if (camShake != null)
            {
               camShake.Shake(currentPhase.shakeIntensity, 0.5f);
                Debug.Log($"📺 Screen Shake: интенсивность {currentPhase.shakeIntensity}", this);
            }
        }
    }

    // ═══════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════

    /// <summary>
    /// Запустить бой (вызвать из триггера/диалога)
    /// </summary>
    public void StartBattle()
    {
        if (battleStarted) return;

        battleStarted = true;
        currentPhaseIndex = -1;

        Debug.Log($"═".PadRight(50, '═'), this);
        Debug.Log($"⚔️═".PadRight(50, '═'), this);
        Debug.Log($"⚔️ БОСС БИТВА НАЧАЛАСЬ!", this);
        Debug.Log($"❤️ HP: {bossCombat.CurrentHealth}/{bossCombat.maxHealth}", this);
        Debug.Log($"═".PadRight(50, '═'), this);

        OnBattleStart?.Invoke();

        // Первая проверка фазы
        CheckPhaseTransition();
    }

    void DefeatBoss()
    {
        if (bossDefeated) return;

        bossDefeated = true;

        Debug.Log($"═".PadRight(50, '═'), this);
        Debug.Log($"🏆═".PadRight(50, '═'), this);
        Debug.Log($"🏆 БОСС ПОБЕЖДЁН!", this);
        Debug.Log($"═".PadRight(50, '═'), this);

        OnBossDefeated?.Invoke();
        enabled = false;
    }

    // ═══════════════════════════════════════
    // ГЕТТЕРЫ
    // ═══════════════════════════════════════

    /// <summary> Индекс текущей фазы (0, 1, 2...) </summary>
    public int GetCurrentPhaseIndex() => currentPhaseIndex;

    /// <summary> Название текущей фазы </summary>
    public string GetCurrentPhaseName() => currentPhase?.phaseName ?? "Нет фазы";

    /// <summary> Бой идёт? </summary>
    public bool IsBattleActive() => battleStarted && !bossDefeated;

    /// <summary> Босс мёртв? </summary>
    public bool IsBossDead() => bossDefeated;

    /// <summary> HP в процентах (0.0 - 1.0) </summary>
    public float GetHPPercent() => battleStarted ? GetHPPercent() : 1f;

    /// <summary> Текущий HP </summary>
    public int GetCurrentHP() => bossCombat?.CurrentHealth ?? 0;

    /// <summary> Максимальный HP </summary>
    public int GetMaxHP() => bossCombat?.maxHealth ?? 0;

    // ═══════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || currentPhase == null || bossCombat == null) return;

        // Индикатор фазы над боссом
        Gizmos.color = currentPhase.glowColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);

#if UNITY_EDITOR
        // Информационный текст
        string info = $"【{currentPhase.phaseName}】\n" +
                     $"HP: {bossCombat.CurrentHealth}/{bossCombat.maxHealth}\n" +
                     $"({GetHPPercent()*100:F0}%)";
                     
        UnityEditor.Handles.Label(transform.position + Vector3.up * 4f, info);
        
        // Радиус обнаружения (если изменился)
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, bossCombat.detectionRadius);
#endif
    }
}