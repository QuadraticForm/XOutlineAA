#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(XConditionalHideAttribute))]
public class XConditionalHidePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var conditionalHideAttribute = (XConditionalHideAttribute)attribute;
        bool conditionMet = EvaluateCondition(property, conditionalHideAttribute);

        bool wasEnabled = GUI.enabled;
        GUI.enabled = !conditionMet || !conditionalHideAttribute.Disable;

        if (!conditionalHideAttribute.HideInInspector || !conditionMet)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        GUI.enabled = wasEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var conditionalHideAttribute = (XConditionalHideAttribute)attribute;
        bool conditionMet = EvaluateCondition(property, conditionalHideAttribute);

        if (!conditionalHideAttribute.HideInInspector || !conditionMet)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        return 0;
    }

    private bool EvaluateCondition(SerializedProperty property, XConditionalHideAttribute conditionalHideAttribute)
    {
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(conditionalHideAttribute.ConditionFieldName);

        if (conditionProperty == null)
        {
            Debug.LogWarning($"Cannot find property with name {conditionalHideAttribute.ConditionFieldName}");
            return false;
        }

        object actualValue = GetActualValue(conditionProperty);

		if (conditionalHideAttribute.CompareValue == null)
			// return actualValue == null;
			return CompareValues(actualValue, null, conditionalHideAttribute.Comparison);
		else 
		{
			object compareValue = Convert.ChangeType(conditionalHideAttribute.CompareValue, actualValue.GetType());

			return CompareValues(actualValue, compareValue, conditionalHideAttribute.Comparison);
		}
    }

    private object GetActualValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return property.boolValue;
            case SerializedPropertyType.Enum:
                return property.enumValueIndex;
            case SerializedPropertyType.Float:
                return property.floatValue;
            case SerializedPropertyType.Integer:
                return property.intValue;
			case SerializedPropertyType.ObjectReference:
				return property.objectReferenceValue;
            default:
                Debug.LogWarning($"Unsupported property type {property.propertyType}");
                return null;
        }
    }

    private bool CompareValues(object actualValue, object compareValue, XConditionalHideAttribute.CompareMethod comparison)
    {
		if (comparison == XConditionalHideAttribute.CompareMethod.Equal)
		{
			return Equals(actualValue, compareValue); 
		}
		else if (comparison == XConditionalHideAttribute.CompareMethod.NotEqual)
		{
			return !Equals(actualValue, compareValue);
		}
		else 
		{
			IComparable comparableActualValue = (IComparable)actualValue;

			switch (comparison)
			{
				case XConditionalHideAttribute.CompareMethod.GreaterThan:
					if (actualValue == null) return false;
					return ((IComparable)actualValue).CompareTo(compareValue) > 0;
				case XConditionalHideAttribute.CompareMethod.GreaterThanOrEqual:
					if (actualValue == null) return false;
					return ((IComparable)actualValue).CompareTo(compareValue) >= 0;
				case XConditionalHideAttribute.CompareMethod.LessThan:
					if (actualValue == null) return false;
					return ((IComparable)actualValue).CompareTo(compareValue) < 0;
				case XConditionalHideAttribute.CompareMethod.LessThanOrEqual:
					if (actualValue == null) return false;
					return ((IComparable)actualValue).CompareTo(compareValue) <= 0;
				default:
					Debug.LogWarning($"Unsupported comparison method {comparison}");
					return false;
			}
		}
    }
}

#endif