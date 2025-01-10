using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XDrawEventListenerCountAttribute : PropertyAttribute
{
	public bool drawZero = true;
	public string format = "{0} Listeners";
	public int width = 100;

	public XDrawEventListenerCountAttribute(bool drawZero = true, string format = "{0} Listeners", int width = 75)
	{
		this.drawZero = drawZero;
		this.format = format;
		this.width = width;
	}
}

#if UNITY_EDITOR

// Custom PropertyDrawer for the XFoldoutAttribute
[CustomPropertyDrawer(typeof(XDrawEventListenerCountAttribute))]
public class XDrawEventListenerCountAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.PropertyField(position, property, label, true);

		// this is dangerous if the property is not a field in the target object
		// for example, if the property is an array element, this will throw an exception
		/*
		// the XDrawEventListenerCountAttribute is an attribute that can be applied to UnityEvent fields
		// get its unity event field here
		var unityEvent = fieldInfo.GetValue(property.serializedObject.targetObject) as UnityEventBase;

		if (unityEvent == null)
			return;

		// Get the number of persistent listeners
		var listenerCount = unityEvent.GetPersistentEventCount();
		*/

		// get the listener count form the serialized property
		var listenerCount = property.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;

		var attribute = this.attribute as XDrawEventListenerCountAttribute;

		// if draw zero is false and there are no persistent listeners, return
		var drawZero = attribute.drawZero;
		if (!drawZero && listenerCount == 0)
			return;

		var rect = new Rect(
			position.xMax - attribute.width, 
			position.y, 
			attribute.width, 
			EditorGUIUtility.singleLineHeight);

		// Draw the number of persistent listeners according to the format string
		var format = attribute.format;

		if (string.IsNullOrEmpty(format))
			EditorGUI.LabelField(rect, listenerCount + " Listeners");
		else
			EditorGUI.LabelField(rect, string.Format(format, listenerCount));
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}
}

#endif
