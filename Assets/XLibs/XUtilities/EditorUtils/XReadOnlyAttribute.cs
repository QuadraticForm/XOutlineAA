using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XReadOnlyAttribute : PropertyAttribute
{
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(XReadOnlyAttribute))]
public class XReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 保存原来的 GUI.enabled 状态
        bool previousGUIState = GUI.enabled;

        // 设置 GUI.enabled 为 false 禁止编辑
        GUI.enabled = false;

        // 绘制默认的属性字段
        EditorGUI.PropertyField(position, property, label);

        // 恢复原来的 GUI.enabled 状态
        GUI.enabled = previousGUIState;
    }

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label);
	}
}

#endif