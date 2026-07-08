using UnityEngine;

/// <summary>
/// Применяет пользовательское смещение камеры (X/Y) из PlayerPrefs
/// поверх базового generalOffset у dg_simpleCamFollow.
/// Добавляется на камеру автоматически из PauseMenu.
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraOffsetApplier : MonoBehaviour
{
    public const string OffsetXKey = "CameraOffsetX";
    public const string OffsetYKey = "CameraOffsetY";

    private dg_simpleCamFollow camFollow;
    private Vector3 baseOffset;
    private bool baseCaptured;

    void Start()
    {
        camFollow = GetComponent<dg_simpleCamFollow>();
        if (camFollow == null) return;

        // Захватываем базовый offset после Start камеры (takeOffsetFromInitialPos)
        baseOffset = camFollow.generalOffset;
        baseCaptured = true;

        ApplySavedOffset();
    }

    public void ApplySavedOffset()
    {
        SetOffset(PlayerPrefs.GetFloat(OffsetXKey, 0f), PlayerPrefs.GetFloat(OffsetYKey, 0f));
    }

    public void SetOffset(float x, float y)
    {
        if (camFollow == null || !baseCaptured) return;
        
        // Ограничиваем смещение, чтобы камера не выходила за границы экрана
        float maxOffsetX = 3f;  // Максимальное смещение по X
        float maxOffsetY = 2f;  // Максимальное смещение по Y
        
        x = Mathf.Clamp(x, -maxOffsetX, maxOffsetX);
        y = Mathf.Clamp(y, -maxOffsetY, maxOffsetY);
        
        camFollow.generalOffset = baseOffset + new Vector3(x, y, 0f);
    }

    public static void SaveOffset(float x, float y)
    {
        PlayerPrefs.SetFloat(OffsetXKey, x);
        PlayerPrefs.SetFloat(OffsetYKey, y);
        PlayerPrefs.Save();
    }

    /// <summary>Находит/создаёт аплаер на главной камере и применяет смещение.</summary>
    public static CameraOffsetApplier EnsureOnMainCamera()
    {
        dg_simpleCamFollow follow = Object.FindAnyObjectByType<dg_simpleCamFollow>();
        if (follow == null) return null;

        CameraOffsetApplier applier = follow.GetComponent<CameraOffsetApplier>();
        if (applier == null)
            applier = follow.gameObject.AddComponent<CameraOffsetApplier>();
        return applier;
    }

    /// <summary>Применяет сохранённое смещение из PlayerPrefs к камере.</summary>
    public static void ApplyOffset()
    {
        CameraOffsetApplier applier = EnsureOnMainCamera();
        if (applier != null)
            applier.ApplySavedOffset();
    }
}
