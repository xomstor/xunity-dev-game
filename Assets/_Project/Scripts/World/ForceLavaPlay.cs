using UnityEngine;

public class ForceLavaPlay : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Запускаем анимацию принудительно
        animator.Play("HealthGlobe", 0, 0f);

        // Обязательно обновляем
        animator.Update(0f);
    }

    void Update()
    {
        // Если анимация остановилась - рестартуем
        if (!animator.GetCurrentAnimatorStateInfo(0).loop ||
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            animator.Play("HealthGlobe", 0, 0f);
        }
    }
}
