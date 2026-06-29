using UnityEngine;

public class NameTagDetector : MonoBehaviour
{
    public NameTagUI nameTag;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && nameTag != null)
        {
            nameTag.Show();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && nameTag != null)
        {
            nameTag.Hide();
        }
    }
}
