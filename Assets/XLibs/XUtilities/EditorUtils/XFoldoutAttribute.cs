using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class XFoldoutAttribute : PropertyAttribute
{
	public string foldoutName;

	public XFoldoutAttribute()
	{
		this.foldoutName = null;
	}

	public XFoldoutAttribute(string foldoutName)
	{
		this.foldoutName = foldoutName;
	}
}

#if UNITY_EDITOR

// Custom PropertyDrawer for the XFoldoutAttribute
[CustomPropertyDrawer(typeof(XFoldoutAttribute))]
public class XFoldoutAttributeDrawer : PropertyDrawer
{
	private bool foldout;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		XFoldoutAttribute foldoutAttribute = (XFoldoutAttribute)attribute;

		var foldoutName = foldoutAttribute.foldoutName ?? label.text;

		var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

		foldout = EditorGUI.Foldout(foldoutRect, foldout, foldoutName);
		if (foldout)
		{
			var propertyRect = new Rect(position.x + 10, position.y + EditorGUIUtility.singleLineHeight, position.width - 10, position.height);

			EditorGUI.PropertyField(propertyRect, property, label, true);
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		var height = EditorGUIUtility.singleLineHeight;

		if (foldout)
			height += EditorGUI.GetPropertyHeight(property, label, true);

		return height;
	}
}

#endif
