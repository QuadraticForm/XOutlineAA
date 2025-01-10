# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ArrayMaker))]
public class ArrayMakerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var arrayMaker = target as ArrayMaker;

		if (GUILayout.Button("Clear"))
		{
			arrayMaker.Clear();
		}

		if (GUILayout.Button("Create Array"))
		{
			arrayMaker.CreateArray();
		}
	}
}
#endif
