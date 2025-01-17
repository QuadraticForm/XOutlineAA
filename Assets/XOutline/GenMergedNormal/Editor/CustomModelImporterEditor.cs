using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;

using Newtonsoft.Json;
//using Unity.Plastic.Newtonsoft.Json;

namespace xoutline
{
	class UserDataUtil
	{
		public class Property<T>
		{
			public string key;
			public string displayName;
			public T defaultValue;
			public T value;
		}

		public static void ReadFromUserData<T>(string userData, Property<T> prop)
		{
			if (string.IsNullOrEmpty(userData))
			{
				prop.value = prop.defaultValue;
				return;
			}

			var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(userData);
			if (properties.TryGetValue(prop.key, out object value))
			{
				if (value is long && typeof(T) == typeof(int))
				{
					prop.value = (T)(object)Convert.ToInt32(value);
				}
				else
				{
					prop.value = (T)value;
				}
				return;
			}

			prop.value = prop.defaultValue;
		}

		public static void WriteToUserData<T>(ref string userData, Property<T> prop)
		{
			var properties = string.IsNullOrEmpty(userData) ?
				new Dictionary<string, object>() :
				JsonConvert.DeserializeObject<Dictionary<string, object>>(userData);

			properties[prop.key] = prop.value;

			userData = JsonConvert.SerializeObject(properties);
		}
	}

	class CustomModelProps
	{
		public UserDataUtil.Property<bool>
			genMergedNormal = new UserDataUtil.Property<bool>
			{
				key = "genMergedNormal",
				displayName = "Gen Merged Normal",
				defaultValue = false,
				value = false,
			};

		// "UV1" as in shader's TEXCOORD1, corresponds to mesh.uv2, and so on, thanks unity for the confusion
		public UserDataUtil.Property<int>
			mergedNormalUVChannel = new UserDataUtil.Property<int>
			{
				key = "mergedNormalUVChannel",
				displayName = "UV Channel",
				defaultValue = 3,
				value = 3
			};

		public void Reset()
		{
			genMergedNormal.value = genMergedNormal.defaultValue;
			mergedNormalUVChannel.value = mergedNormalUVChannel.defaultValue;
		}

		// TODO, handle multi-edit better
		public void ReadFromUserData(string userData)
		{
			UserDataUtil.ReadFromUserData(userData, genMergedNormal);
			UserDataUtil.ReadFromUserData(userData, mergedNormalUVChannel);
		}

		// there is no WriteToUserData, because it's done in CustomModelImporterEditor
		// why? cuz multi-edit can be done only in editor, and it's easier to do it there
	}


	[CustomEditor(typeof(ModelImporter))]
	[CanEditMultipleObjects]
	class CustomModelImporterEditor : Editor
	{
		private AssetImporterEditor defaultEditor;

		CustomModelProps customModelProps = new();

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

			// defaultEditor.OnEnable();
		}

		void OnDisable()
		{
			if (defaultEditor != null)
				defaultEditor.OnDisable();
		}

		void OnDestroy()
		{
			/*
			if (defaultEditor != null)
			{
				DestroyImmediate(defaultEditor);
				defaultEditor = null;
			}
			*/
		}

		private void DrawBoolProperty(UserDataUtil.Property<bool> property)
		{
			EditorGUI.showMixedValue = !MultiEditIsAllSameAsThis(property);

			var newValue = EditorGUILayout.Toggle(property.displayName, property.value);

			if (newValue != property.value)
			{
				property.value = newValue;
				MultiEditWriteToUserData(property);
			}

			EditorGUI.showMixedValue = false;
		}

		private void DrawIntPopupProperty(UserDataUtil.Property<int> property, string[] displayOptions, int[] optionValues)
		{
			EditorGUI.showMixedValue = !MultiEditIsAllSameAsThis(property);

			var newValue = EditorGUILayout.IntPopup(property.displayName, property.value, displayOptions, optionValues);

			if (newValue != property.value)
			{
				property.value = newValue;
				MultiEditWriteToUserData(property);
			}

			EditorGUI.showMixedValue = false;
		}

		public override void OnInspectorGUI()
		{
			defaultEditor.OnInspectorGUI();

			// read from user data (from the first target)
			customModelProps.ReadFromUserData(((ModelImporter)target).userData);

			// Draw: Header

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("XOutline", EditorStyles.boldLabel);

			// Draw: Gen Merged Normal

			DrawBoolProperty(customModelProps.genMergedNormal);

			// Draw: Merged Normal UV Channel

			DrawIntPopupProperty(customModelProps.mergedNormalUVChannel,
				new string[] { "UV1", "UV2", "UV3" }, new int[] { 1, 2, 3 });

			// Draw: Info about UVs

			string infoAboutUVs = "UV1, UV2, UV3 correspond to mesh.uv2, mesh.uv3, mesh.uv4";
			string infoAboutUVs2 = "UV1, UV2 might be used for lightmap by unity";

			EditorGUILayout.LabelField(infoAboutUVs, EditorStyles.miniLabel);
			EditorGUILayout.LabelField(infoAboutUVs2, EditorStyles.miniLabel);

			// Apply changes

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// multi-edit read. this is a little special, we don't need data from all targets,
		/// just the first one, and wether it's the same for all targets.
		/// </summary>
		/// <returns>if all values are the same</returns>
		bool MultiEditReadFromUserDatas<T>(UserDataUtil.Property<T> prop)
		{
			var firstImporter = (ModelImporter)targets[0];
			UserDataUtil.ReadFromUserData(firstImporter.userData, prop);

			return MultiEditIsAllSameAsThis(prop);
		}

		bool MultiEditIsAllSameAsThis<T>(UserDataUtil.Property<T> prop)
		{
			var firstValue = prop.value;

			for (int i = 1; i < targets.Length; i++)
			{
				var importer = (ModelImporter)targets[i];
				UserDataUtil.ReadFromUserData(importer.userData, prop);
				if (!EqualityComparer<T>.Default.Equals(firstValue, prop.value))
				{
					prop.value = firstValue;
					return false;
				}
			}
			return true;
		}

		void MultiEditWriteToUserData<T>(UserDataUtil.Property<T> prop)
		{
			for (int i = 0; i < targets.Length; i++)
			{
				var importer = (ModelImporter)targets[i];
				string json = importer.userData;
				UserDataUtil.WriteToUserData(ref json, prop);
				importer.userData = json;
			}
		}
	}
}
