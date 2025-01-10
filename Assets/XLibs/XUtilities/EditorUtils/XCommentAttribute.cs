using UnityEngine;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class XCommentAttribute : PropertyAttribute
{
	public string comment;
	public int height = 1;
	public Color color;

	public static Color defaultCommentColor = new Color(0.1f, 0.5f, 0.1f, 1.0f);

	public XCommentAttribute(string comment)
	{
		this.comment = comment;
		this.height = 1;
		this.color = defaultCommentColor;
	}

	public XCommentAttribute(string comment, int height)
	{
		this.comment = comment;
		this.height = height;
		this.color = defaultCommentColor;
	}

	public XCommentAttribute(string comment, int height, Color color)
	{
		this.comment = comment;
		this.height = height;
		this.color = color;
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(XCommentAttribute))]
public class XCommentDrawer : DecoratorDrawer
{
	public int CommentHeight()
	{
		var commentAttribute = (XCommentAttribute)attribute;
		return commentAttribute.height * (int)EditorGUIUtility.singleLineHeight;
	}

	public override float GetHeight() => CommentHeight();

	public override void OnGUI(Rect position)
	{
		// draw comment
		var commentAttribute = (XCommentAttribute)attribute;

		var commentHeight = CommentHeight();
		position.height = commentHeight;

		var style = new GUIStyle(EditorStyles.label);
		style.wordWrap = true;
		style.normal.textColor = commentAttribute.color;

		// EditorGUI.LabelField(position, commentAttribute.comment, style); // this will change the GUIContent label above, very strange

		var guiContent = new GUIContent(commentAttribute.comment);
		EditorGUI.LabelField(position, guiContent, style);
	}
}
#endif