using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class XSmoothDampedFloat
{
	public float value = 0;
	public float targetValue = 0;
	public float velocity = 0;
	public float smoothTime = 0.1f;
	public float maxSpeed = Mathf.Infinity;

	public static implicit operator float(XSmoothDampedFloat smoothDampedFloat)
	{
		return smoothDampedFloat.value;
	}

	public XSmoothDampedFloat(float _initialValue, float _smoothTime, float _maxSpeed = Mathf.Infinity)
	{
		value = _initialValue;
		targetValue = _initialValue;
		velocity = 0f;
		smoothTime = _smoothTime;
		maxSpeed = _maxSpeed;
	}

	public void Update()
	{
		Update(Time.deltaTime);
	}

	public void Update(float deltaTime)
	{
		value = Mathf.SmoothDamp(value, targetValue, ref velocity, smoothTime, maxSpeed, deltaTime);
	}
}

[Serializable]
public class XSmoothDampedVector2
{
	public Vector2 value = Vector2.zero;
	public Vector2 targetValue = Vector2.zero;
	public Vector2 velocity = Vector2.zero;
	public float smoothTime = 0.1f;
	public float maxSpeed = Mathf.Infinity;

	public static implicit operator Vector2(XSmoothDampedVector2 smoothDampedVector2)
	{
		return smoothDampedVector2.value;
	}

	public XSmoothDampedVector2(Vector2 _initialValue, float _smoothTime, float _maxSpeed = Mathf.Infinity)
	{
		value = _initialValue;
		targetValue = _initialValue;
		velocity = Vector2.zero;
		smoothTime = _smoothTime;
		maxSpeed = _maxSpeed;
	}

	public void Update()
	{
		Update(Time.deltaTime);
	}

	public void Update(float deltaTime)
	{
		value = Vector2.SmoothDamp(value, targetValue, ref velocity, smoothTime, maxSpeed, deltaTime);
	}
}

[Serializable]
public class XSmoothDampedVector3
{
	public Vector3 value = Vector3.zero;
	public Vector3 targetValue = Vector3.zero;
	public Vector3 velocity = Vector3.zero;
	public float smoothTime = 0.1f;
	public float maxSpeed = Mathf.Infinity;

	public static implicit operator Vector3(XSmoothDampedVector3 smoothDampedVector3)
	{
		return smoothDampedVector3.value;
	}

	public XSmoothDampedVector3(Vector3 _initialValue, float _smoothTime, float _maxSpeed = Mathf.Infinity)
	{
		value = _initialValue;
		targetValue = _initialValue;
		velocity = Vector3.zero;
		smoothTime = _smoothTime;
		maxSpeed = _maxSpeed;
	}

	public void Update()
	{
		Update(Time.deltaTime);
	}

	public void Update(float deltaTime)
	{
		value = Vector3.SmoothDamp(value, targetValue, ref velocity, smoothTime, maxSpeed, deltaTime);
	}
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(XSmoothDampedFloat))]
public class XSmoothDampedFloatDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		EditorGUI.BeginProperty(position, label, property);

        EditorGUI.PropertyField(position, property, label, true);

		// Get the target object
		var smoothDampedFloat = fieldInfo.GetValue(property.serializedObject.targetObject) as XSmoothDampedFloat;

		if (smoothDampedFloat == null)
			return;

		// Draw value as label to the right
		int valueLabelWidth = 75;

		var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, valueLabelWidth, EditorGUIUtility.singleLineHeight);

		EditorGUI.LabelField(valueRect, smoothDampedFloat.value.ToString("F2"));

		EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}


[CustomPropertyDrawer(typeof(XSmoothDampedVector2))]
public class XSmoothDampedVector2Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        // Get the target object
        var smoothDampedVector2 = fieldInfo.GetValue(property.serializedObject.targetObject) as XSmoothDampedVector2;

        if (smoothDampedVector2 == null)
            return;

        // Draw value as label to the right
        int valueLabelWidth = 150;

        var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, valueLabelWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(valueRect, smoothDampedVector2.value.ToString("F2"));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}


[CustomPropertyDrawer(typeof(XSmoothDampedVector3))]
public class XSmoothDampedVector3Drawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.PropertyField(position, property, label, true);

		// Get the target object
		var smoothDampedVector3 = fieldInfo.GetValue(property.serializedObject.targetObject) as XSmoothDampedVector3;

		if (smoothDampedVector3 == null)
			return;

		// Draw value as label to the right
		int valueLabelWidth = 150;

		var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, valueLabelWidth, EditorGUIUtility.singleLineHeight);

		EditorGUI.LabelField(valueRect, smoothDampedVector3.value.ToString("F2"));
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}
}
#endif