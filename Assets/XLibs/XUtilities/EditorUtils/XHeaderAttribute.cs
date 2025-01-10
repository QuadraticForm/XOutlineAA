using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XHeaderAttribute : PropertyAttribute
{
	public string content = "";
	public int fontSize = 18;

	/// <summary>
	/// when custom decorator is used with custom property drawer,
	/// it will be drawn to the left compared to when custom decorator is used alone
	/// so add this to adjust the left margin as a workaround
	/// TODO: find a better way to adjust the left margin
	/// </summary>
	public int leftMargin = 0;

	public XHeaderAttribute(string content, int leftMargin = 0, int fontSize = 18)
	{
		this.content = content;
		this.fontSize = fontSize;
		this.leftMargin = leftMargin;
	}

	/// <summary>
	/// when custom decorator is used with custom property drawer,
	/// it will be drawn to the left compared to when custom decorator is used alone
	/// so add this to adjust the left margin as a workaround
	/// TODO: find a better way to adjust the left margin
	/// </summary>
	public XHeaderAttribute(string content, bool addLeftMargin, int fontSize = 18)
	{
		this.content = content;
		this.fontSize = fontSize;

		this.leftMargin = addLeftMargin ? 15 : 0; // 15 is the default left margin for Unity's header, TODO: find a way to get the default left margin
	}
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(XHeaderAttribute))]
public class XHeaderAttributeDrawer : DecoratorDrawer
{
	int spaceAboveHeader = 5;
	int spaceBelowHeader = 1;

	public override void OnGUI(Rect position)
	{
		// draw header

		var headerAttribute = (XHeaderAttribute)attribute;

		var style = new GUIStyle(EditorStyles.label)
		{
			fontSize = headerAttribute.fontSize,
			fontStyle = FontStyle.Bold
		};

		var originalY = position.y;

		position.y = originalY + EditorGUIUtility.standardVerticalSpacing * spaceAboveHeader;
		position.height = headerAttribute.fontSize;
		position.x += headerAttribute.leftMargin;

		// EditorGUI.LabelField(position, headerAttribute.Content, style); // see this comment, this line changed the _label's text, seems strange

		var headerContent = new GUIContent(headerAttribute.content);
		EditorGUI.LabelField(position, headerContent, style);
	}

	public override float GetHeight()
	{
		var headerAttribute = (XHeaderAttribute)attribute;
		return headerAttribute.fontSize + (int)EditorGUIUtility.standardVerticalSpacing * (spaceAboveHeader + spaceBelowHeader); // vertical spacing 2 above and 1 below header
	}
}

#endif