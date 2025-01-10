using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class XTransformInfluencePair
{
	public Transform transform = null;
	[Range(0,1)]
	public float influence = 1.0f;

	public bool Valid => transform != null;

	public bool HasEffect => transform != null && influence > 0; 
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(XTransformInfluencePair))]
public class XTransformInfluencePairDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Remove label indentation
		EditorGUI.indentLevel = 0;

		// Calculate rects
		float halfWidth = position.width / 2f;
		Rect transformRect = new Rect(position.x, position.y, halfWidth - 2, position.height);
		Rect influenceRect = new Rect(position.x + halfWidth + 2, position.y, halfWidth - 2, position.height);

		// Fetch properties
		SerializedProperty transformProp = property.FindPropertyRelative("transform");
		SerializedProperty influenceProp = property.FindPropertyRelative("influence");

		// Draw fields
		EditorGUI.PropertyField(transformRect, transformProp, GUIContent.none);
		EditorGUI.PropertyField(influenceRect, influenceProp, GUIContent.none);
	}
}
#endif
