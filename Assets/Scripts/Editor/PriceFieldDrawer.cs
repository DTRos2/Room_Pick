using UnityEditor;
using UnityEngine;

/// <summary>
/// [PriceField]가 붙은 int 필드를 그리고, 그 아래에 "150,000원"처럼 천 단위로 끊은 값을 보여 준다.
/// </summary>
[CustomPropertyDrawer(typeof(PriceFieldAttribute))]
public class PriceFieldDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return property.propertyType == SerializedPropertyType.Integer
            ? EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing
            : EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.LabelField(position, label.text, "[PriceField]는 int 필드에만 쓸 수 있습니다.");
            return;
        }

        var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(fieldRect, property, label);

        var valueRect = new Rect(
            position.x + EditorGUIUtility.labelWidth,
            fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
            position.width - EditorGUIUtility.labelWidth,
            EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(valueRect, $"{property.intValue:N0}원", EditorStyles.miniLabel);
    }
}
