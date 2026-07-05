#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SelectVirtualJoystick : EditorWindow
{
    [MenuItem("Tools/Select Virtual Joystick")]
    public static void Select()
    {
        VirtualJoystick[] joysticks = FindObjectsByType<VirtualJoystick>();
        if (joysticks.Length == 0)
        {
            Debug.Log("VirtualJoystick не найден на сцене.");
            return;
        }

        GameObject[] objects = new GameObject[joysticks.Length];
        for (int i = 0; i < joysticks.Length; i++)
            objects[i] = joysticks[i].gameObject;

        Selection.objects = objects;
        EditorGUIUtility.PingObject(objects[0]);
        Debug.Log($"Найдено VirtualJoystick: {joysticks.Length}. Выбраны в иерархии.");
    }
}
#endif
