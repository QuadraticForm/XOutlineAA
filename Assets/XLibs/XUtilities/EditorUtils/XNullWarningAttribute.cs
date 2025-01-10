using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XNullWarningAttribute : PropertyAttribute
{
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(XNullWarningAttribute))]
public class XNullWarningAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.objectReferenceValue == null)
		{
			var prevColor = GUI.color;
			GUI.color = Color.red;
			EditorGUI.PropertyField(position, property, label);
			GUI.color = prevColor;
		}
		else
		{
			EditorGUI.PropertyField(position, property, label);
		}
	}
}

#endif