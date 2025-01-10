using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Three-dimensional boolean vector.
/// </summary>
[System.Serializable]
public struct XVector3Bool
{
    /// <summary>X component of the vector.</summary>
    public bool x;
    /// <summary>Y component of the vector.</summary>
    public bool y;
    /// <summary>Z component of the vector.</summary>
    public bool z;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="val">Boolean value for x, y and z.</param>
    public XVector3Bool(bool val)
    {
        x = y = z = val;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="x">Boolean value for x.</param>
    /// <param name="y">Boolean value for y.</param>
    /// <param name="z">Boolean value for z.</param>
    public XVector3Bool(bool x, bool y, bool z)
    {
        this.x = x; this.y = y; this.z = z;
    }

	public readonly bool IsAllTrue => x && y && z;
	public readonly bool IsAllFalse => !x && !y && !z;

    /// <summary>The vector with all components set to false.</summary>
    public static readonly XVector3Bool False = new XVector3Bool(false);

    /// <summary>The vector with all components set to true.</summary>
    public static readonly XVector3Bool True = new XVector3Bool(true);

    /// <summary>The vector with all components set to false.</summary>
    public static readonly XVector3Bool AllFalse = new XVector3Bool(false);

    /// <summary>The vector with all components set to true.</summary>
    public static readonly  XVector3Bool AllTrue = new XVector3Bool(true);

    /// <summary>The vector (false, false, false).</summary>
    public static readonly XVector3Bool Zero = new XVector3Bool(false, false, false);

    /// <summary>The vector (true, true, true).</summary>
    public static readonly XVector3Bool One = new XVector3Bool(true, true, true);

    /// <summary>The vector (true, false, false).</summary>
    public static readonly XVector3Bool UnitX = new XVector3Bool(true, false, false);

    /// <summary>The vector (false, true, false).</summary>
    public static readonly XVector3Bool UnitY = new XVector3Bool(false, true, false);

    /// <summary>The vector (false, false, true).</summary>
    public static readonly XVector3Bool UnitZ = new XVector3Bool(false, false, true);

    // Override the equality operator
    public static bool operator ==(XVector3Bool lhs, XVector3Bool rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }

    // Override the inequality operator
    public static bool operator !=(XVector3Bool lhs, XVector3Bool rhs)
    {
        return !(lhs == rhs);
    }

    // Override Equals
    public override bool Equals(object obj)
    {
        if (obj is XVector3Bool other)
        {
            return this == other;
        }
        return false;
    }
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(XVector3Bool))]
class Vector3BoolDrawer : PropertyDrawer
{
	private const int ButtonCount = 3;
	private const float Padding = 2f;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight + Padding * 2; // Slight padding for aesthetics
	}

	public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(rect, label, property);

		// Draw the main field label
		rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

		SerializedProperty m_X = property.FindPropertyRelative("x");
		SerializedProperty m_Y = property.FindPropertyRelative("y");
		SerializedProperty m_Z = property.FindPropertyRelative("z");

		float buttonWidth = (rect.width - (ButtonCount - 1) * Padding) / ButtonCount;

		Rect buttonRect = new Rect(rect.x, rect.y + Padding, buttonWidth, EditorGUIUtility.singleLineHeight);

		if (DrawButton(buttonRect, "X", m_X.boolValue))
			m_X.boolValue = !m_X.boolValue;

		buttonRect.x += buttonWidth + Padding;

		if (DrawButton(buttonRect, "Y", m_Y.boolValue))
			m_Y.boolValue = !m_Y.boolValue;

		buttonRect.x += buttonWidth + Padding;

		if (DrawButton(buttonRect, "Z", m_Z.boolValue))
			m_Z.boolValue = !m_Z.boolValue;

		if (GUI.changed)
			property.serializedObject.ApplyModifiedProperties();

		EditorGUI.EndProperty();
	}

	bool DrawButton(Rect rect, string axisLabel, bool state)
	{
		string buttonLabel = $"{axisLabel}: {(state ? "ON" : "OFF")}";
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
		{
			normal = { textColor = state ? Color.green : GUI.skin.button.normal.textColor }
		};

		return GUI.Button(rect, buttonLabel, buttonStyle);
	}
}

#endif