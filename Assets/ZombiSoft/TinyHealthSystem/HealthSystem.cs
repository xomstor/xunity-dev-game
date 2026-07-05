using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance;

    public Image currentHealthBar;
    public Image currentHealthGlobe;
    public Text healthText;
    public float hitPoint = 100f;
    public float maxHitPoint = 100f;

    public Image currentManaBar;
    public Image currentManaGlobe;
    public Text manaText;
    public float manaPoint = 100f;
    public float maxManaPoint = 100f;

    public bool Regenerate = true; // <--- НЕ ЗАБУДЬ УБРАТЬ ГАЛОЧКУ В ИНСПЕКТОРЕ!
    public float regen = 0.1f;
    private float timeleft = 0.0f;
    public float regenUpdateInterval = 1f;

    public bool GodMode;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateGraphics();
        timeleft = regenUpdateInterval;
    }

    void Update()
    {
        if (Regenerate)
            Regen();
    }

    private void Regen()
    {
        timeleft -= Time.deltaTime;

        if (timeleft <= 0.0)
        {
            if (GodMode)
            {
                HealDamage(maxHitPoint);
                RestoreMana(maxManaPoint);
            }
            else
            {
                HealDamage(regen);
                RestoreMana(regen);
            }

            UpdateGraphics();
            timeleft = regenUpdateInterval;
        }
    }

    // <--- ИСПРАВЛЕНО: Добавлены проверки if (... != null) --->
    private void UpdateHealthBar()
    {
        if (currentHealthBar == null) return; // Если удалил бар - просто пропускаем

        float ratio = hitPoint / maxHitPoint;
        currentHealthBar.rectTransform.localPosition = new Vector3(currentHealthBar.rectTransform.rect.width * ratio - currentHealthBar.rectTransform.rect.width, 0, 0);

        if (healthText != null)
            healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
    }

    private void UpdateHealthGlobe()
    {
        if (currentHealthGlobe == null) return; // Если удалил глоб - просто пропускаем

        float ratio = hitPoint / maxHitPoint;
        currentHealthGlobe.rectTransform.localPosition = new Vector3(0, currentHealthGlobe.rectTransform.rect.height * ratio - currentHealthGlobe.rectTransform.rect.height, 0);

        if (healthText != null)
            healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
    }
    // <--------------------------------------------------------->

    public void TakeDamage(float Damage)
    {
        hitPoint -= Damage;
        if (hitPoint < 1)
            hitPoint = 0;

        UpdateGraphics();
        StartCoroutine(PlayerHurts());
    }

    public void HealDamage(float Heal)
    {
        hitPoint += Heal;
        if (hitPoint > maxHitPoint)
            hitPoint = maxHitPoint;

        UpdateGraphics();
    }

    public void SetMaxHealth(float max)
    {
        maxHitPoint += (int)(maxHitPoint * max / 100);
        UpdateGraphics();
    }

    // <--- ИСПРАВЛЕНО: Добавлены проверки if (... != null) для маны --->
    private void UpdateManaBar()
    {
        if (currentManaBar == null) return;

        float ratio = manaPoint / maxManaPoint;
        currentManaBar.rectTransform.localPosition = new Vector3(currentManaBar.rectTransform.rect.width * ratio - currentManaBar.rectTransform.rect.width, 0, 0);

        if (manaText != null)
            manaText.text = manaPoint.ToString("0") + "/" + maxManaPoint.ToString("0");
    }

    private void UpdateManaGlobe()
    {
        if (currentManaGlobe == null) return;

        float ratio = manaPoint / maxManaPoint;
        currentManaGlobe.rectTransform.localPosition = new Vector3(0, currentManaGlobe.rectTransform.rect.height * ratio - currentManaGlobe.rectTransform.rect.height, 0);

        if (manaText != null)
            manaText.text = manaPoint.ToString("0") + "/" + maxManaPoint.ToString("0");
    }
    // <--------------------------------------------------------------->

    public void UseMana(float Mana)
    {
        manaPoint -= Mana;
        if (manaPoint < 1)
            manaPoint = 0;

        UpdateGraphics();
    }

    public void RestoreMana(float Mana)
    {
        manaPoint += Mana;
        if (manaPoint > maxManaPoint)
            manaPoint = maxManaPoint;

        UpdateGraphics();
    }

    public void SetMaxMana(float max)
    {
        maxManaPoint += (int)(maxManaPoint * max / 100);
        UpdateGraphics();
    }

    public void UpdateGraphics()
    {
        UpdateHealthBar();
        UpdateHealthGlobe(); // Теперь эта строка будет выполняться, даже если Bar удален!
        UpdateManaBar();
        UpdateManaGlobe();
    }

    IEnumerator PlayerHurts()
    {
        yield return null;
    }

    IEnumerator PlayerDied()
    {
        yield return null;
    }
}