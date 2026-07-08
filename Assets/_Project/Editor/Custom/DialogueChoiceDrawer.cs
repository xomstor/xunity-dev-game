using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueChoice))]
public class DialogueChoiceDrawer : PropertyDrawer
{
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight + Spacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceText"), true) + Spacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("responseLines"), true) + Spacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("subChoices"), true) + Spacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("startsQuest"), true) + Spacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("opensTrashEmanator"), true) + Spacing;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;

            DrawProperty(ref y, position, property.FindPropertyRelative("choiceText"));
            DrawProperty(ref y, position, property.FindPropertyRelative("responseLines"));
            DrawProperty(ref y, position, property.FindPropertyRelative("subChoices"));
            DrawProperty(ref y, position, property.FindPropertyRelative("startsQuest"));
            DrawProperty(ref y, position, property.FindPropertyRelative("opensTrashEmanator"));

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    void DrawProperty(ref float y, Rect position, SerializedProperty property)
    {
        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect rect = new Rect(position.x, y, position.width, height);
        EditorGUI.PropertyField(rect, property, true);
        y += height + Spacing;
    }
}
