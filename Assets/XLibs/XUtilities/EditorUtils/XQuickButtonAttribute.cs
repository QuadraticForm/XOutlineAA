using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XQuickButtonAttribute : PropertyAttribute
{
    public enum Alignment
    {
        Left,
        Center,
        Right
    }

    public string[] methodNames;
    public string[] labels;
    public float widthPercentage;
    public bool drawField;
    public bool fieldEditable;
    public Alignment alignment;

    // Constructor accepting arrays
    public XQuickButtonAttribute(string[] methodNames,
                                string[] labels = null,
                                float widthPercentage = 1.0f,
                                bool drawField = false,
                                bool fieldEditable = false,
                                Alignment alignment = Alignment.Right)
    {
        this.methodNames = methodNames;
        this.labels = labels ?? methodNames.Select(m => m).ToArray();
        this.widthPercentage = widthPercentage;
        this.drawField = drawField;
        this.fieldEditable = fieldEditable;
        this.alignment = alignment;
    }

    // Single-method constructor for convenience
    public XQuickButtonAttribute(string methodName,
                                string label = null,
                                float widthPercentage = 1.0f,
                                bool drawField = false,
                                bool fieldEditable = true,
                                Alignment alignment = Alignment.Right)
        : this(new[] { methodName }, new[] { label ?? methodName }, widthPercentage, drawField, fieldEditable, alignment)
    {
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(XQuickButtonAttribute))]
public class XQuickButtonDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
        var buttonAttribute = (XQuickButtonAttribute)attribute;

        // Calculate button width based on the percentage and number of buttons
        int buttonCount = buttonAttribute.methodNames.Length;
        var buttonWidth = (position.width * buttonAttribute.widthPercentage) / buttonCount;
        // Calculate the remaining field width if not drawing only the button
        var fieldWidth = position.width - (buttonWidth * buttonCount) - (4 * buttonCount); // Adjust_spacing_

        // Calculate rects based on display option
        var fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
        
        if (buttonAttribute.drawField)
        {
            // Draw field
            EditorGUI.BeginDisabledGroup(!buttonAttribute.fieldEditable);
            EditorGUI.PropertyField(fieldRect, property, label);
            EditorGUI.EndDisabledGroup();
        }

        // Draw multiple buttons
        for (int i = 0; i < buttonCount; i++)
        {
            var currentButtonXStart = position.x + fieldWidth + (buttonWidth + 4) * i;
            var buttonRect = new Rect(currentButtonXStart, position.y, buttonWidth, position.height);

            var currentLabel = buttonAttribute.labels.Length > i ? buttonAttribute.labels[i] : "Unknown";

            if (GUI.Button(buttonRect, currentLabel))
            {
                // Get the target object and call the method
                var target = property.serializedObject.targetObject;
                var type = target.GetType();
                var method = type.GetMethod(buttonAttribute.methodNames[i]);
                if (method != null)
                {
                    method.Invoke(target, null);
                }
                else
                {
                    Debug.LogWarning($"Method {buttonAttribute.methodNames[i]} not found in {type}");
                }
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Adjust height for the button and field
        return EditorGUIUtility.singleLineHeight + 2;
    }
}

#endif