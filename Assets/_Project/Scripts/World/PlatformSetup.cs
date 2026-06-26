using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlatformSetup : MonoBehaviour
{
    void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
    }
}
