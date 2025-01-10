// A field that's not drawn,
// used for a workaround that:
// Header's left margin becomes 0 when used with custom drawn property (drawn with PropertyDrawer.OnGUI),
// We can use Header attribute with this field to make the header margin correct.
//
// 一个不会被绘制的字段，
// 用于解决这个问题：
// 当与自定义绘制的属性一起使用时，Header 的左边距会变为 0（由 PropertyDrawer.OnGUI 绘制），
// 我们可以使用 Header 属性与这个字段一起，来使 Header 的左边距正确。

using System;
using UnityEngine;
using UnityEngine.UIElements;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A field that's not drawn,
/// used for a workaround that:
/// Header's left margin becomes 0 when used with custom drawn property (drawn with PropertyDrawer.OnGUI),
/// We can use Header attribute with this field to make the header margin correct.
/// 
/// 一个不会被绘制的字段，
/// 用于解决这个问题：
/// 当与自定义绘制的属性一起使用时，Header 的左边距会变为 0（由 PropertyDrawer.OnGUI 绘制），
/// 我们可以使用 Header 属性与这个字段一起，来使 Header 的左边距正确。
/// </summary>
[Serializable]
public class XEmptyField
{
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(XEmptyField))]
public class XEmptyFieldDrawer : PropertyDrawer
{
	public override VisualElement CreatePropertyGUI(SerializedProperty property)
	{
		// Return an empty VisualElement
		// this will make Header on the top of the property to be drawn correctly
		return new VisualElement();
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return 0;
	}
}

#endif
