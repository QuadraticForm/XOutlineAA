using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[CustomEditor(typeof(ModelImporter))]
[CanEditMultipleObjects]
public class CustomModelImporterEditor : Editor
{
	private AssetImporterEditor defaultEditor;

	class CustomProperty<T>
	{
		public string key;
		public string displayName;
		public Type type = typeof(T);
		public T defaultValue;
		public T value;
	}

	CustomProperty<bool> genMergedNormal = new CustomProperty<bool>
	{
		key = "GenMergedNormal",
		displayName = "Gen Merged Normal",
		defaultValue = false,
		value = false
	};

	// "UV1" as in shader's TEXCOORD1, corresponds to mesh.uv2, thanks unity for the confusion
	CustomProperty<bool> writeToUV1 = new CustomProperty<bool>
	{
		key = "writeToUV1",
		displayName = "Write To UV1",
		defaultValue = false,
		value = false
	};

	// "UV2" as in shader's TEXCOORD2, corresponds to mesh.uv3, thanks unity for the confusion
	CustomProperty<bool> writeToUV2 = new CustomProperty<bool>
	{
		key = "writeToUV2",
		displayName = "Write To UV2",
		defaultValue = false,
		value = false
	};

	// "UV3" as in shader's TEXCOORD3, corresponds to mesh.uv4, thanks unity for the confusion
	CustomProperty<bool> writeToUV3 = new CustomProperty<bool>
	{
		key = "writeToUV3",
		displayName = "Write To UV3",
		defaultValue = false,
		value = false
	};


	void OnEnable()
	{
		// hack to get unity's default editor for ModelImporter
		if (defaultEditor == null)
		{
			defaultEditor = (AssetImporterEditor)AssetImporterEditor.CreateEditor(targets,
				Type.GetType("UnityEditor.ModelImporterEditor, UnityEditor"));

			MethodInfo dynMethod = Type.GetType("UnityEditor.ModelImporterEditor, UnityEditor")
				.GetMethod("InternalSetAssetImporterTargetEditor", BindingFlags.NonPublic | BindingFlags.Instance);

			dynMethod.Invoke(defaultEditor, new object[] { this });
		}
	}

	void OnDisable()
	{
		defaultEditor.OnDisable();
	}

	void OnDestroy()
	{
		defaultEditor.OnEnable();
		DestroyImmediate(defaultEditor);
	}


	private void DrawBoolProperty(CustomProperty<bool> property)
	{
		ReadFromUserData(property);

		var toggleResult = EditorGUILayout.Toggle(property.displayName, property.value);

		if (toggleResult != property.value)
		{
			property.value = toggleResult;
			WriteToUserData(property);
		}
	}

	public override void OnInspectorGUI()
	{
		defaultEditor.OnInspectorGUI();

		EditorGUILayout.Separator();

		EditorGUILayout.LabelField("XOutline", EditorStyles.boldLabel);

		// Draw boolean properties
		DrawBoolProperty(genMergedNormal);

		string infoAboutUVs = "UV1, UV2, UV3 correspond to mesh.uv2, mesh.uv3, mesh.uv4";
		string infoAboutUVs2 = "UV1, UV2 might be used for lightmap by unity";

		EditorGUILayout.LabelField(infoAboutUVs, EditorStyles.miniLabel);
		EditorGUILayout.LabelField(infoAboutUVs2, EditorStyles.miniLabel);

		DrawBoolProperty(writeToUV1);
		DrawBoolProperty(writeToUV2);
		DrawBoolProperty(writeToUV3);

		serializedObject.ApplyModifiedProperties();
	}

	private void ReadFromUserData<T>(CustomProperty<T> prop)
	{
		string json = ((ModelImporter)target).userData;
		if (string.IsNullOrEmpty(json))
		{
			prop.value = prop.defaultValue;
			return;
		}

		Dictionary<string, object> properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
		if (properties.TryGetValue(prop.key, out object value))
		{
			prop.value = (T)value;
			return;
		}

		prop.value = prop.defaultValue;
	}

	private void WriteToUserData<T>(CustomProperty<T> prop)
	{
		for (int i = 0; i < targets.Length; i++)
		{
			ModelImporter importer = (ModelImporter)targets[i];
			string json = importer.userData;
			Dictionary<string, object> properties = string.IsNullOrEmpty(json)
				? new Dictionary<string, object>()
				: JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

			properties[prop.key] = prop.value;

			importer.userData = JsonConvert.SerializeObject(properties);
		}
	}
}
