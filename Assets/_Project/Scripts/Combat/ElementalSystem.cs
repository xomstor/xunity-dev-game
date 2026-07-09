using UnityEngine;

public static class ElementalSystem
{
    public static readonly int ElementCount = System.Enum.GetValues(typeof(ElementalType)).Length;

    /// <summary>
    /// Возвращает множитель урона (1 = обычно, 0 = иммунитет, 2 = удвоение и т.д.).
    /// </summary>
    public static float GetMultiplier(ElementalType attackElement, float[] defenderResistances)
    {
        if (defenderResistances == null || defenderResistances.Length == 0)
            return 1f;

        int index = (int)attackElement;
        if (index < 0 || index >= defenderResistances.Length)
            return 1f;

        // resistance = доля поглощаемого урона: 0 = нет резиста, 1 = иммунитет, >1 = лечит
        // отрицательное = уязвимость
        return 1f - defenderResistances[index];
    }

    /// <summary>
    /// Применяет элементальный множитель к чистому урону.
    /// </summary>
    public static int ApplyElemental(int damage, ElementalType attackElement, float[] defenderResistances)
    {
        if (attackElement == ElementalType.Physical && (defenderResistances == null || defenderResistances.Length == 0))
            return damage;

        float multiplier = GetMultiplier(attackElement, defenderResistances);
        return Mathf.RoundToInt(damage * multiplier);
    }

    public static float[] CreateEmptyResistances()
    {
        return new float[ElementCount];
    }
}
