using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace xoutline
{
	public class GenMergedNormalPostprocessor : AssetPostprocessor
	{
		CustomModelProps customModelProps = new();

		private void OnPostprocessModel(GameObject go)
		{
			ReadCustomModelProps();

			if (!customModelProps.genMergedNormal.value)
				return;

			var sharedMeshes = GetSharedMeshes(go);

			GenMergedNormal.GenAndSave(sharedMeshes, customModelProps.mergedNormalUVChannel.value);
		}

		private void ReadCustomModelProps()
		{
			var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
			if (importer == null)
			{
				customModelProps.Reset();
				return;
			}

			customModelProps.ReadFromUserData(importer.userData);
		}

		internal static List<Mesh> GetSharedMeshes(GameObject go)
		{
			List<Mesh> meshes = new();

			if (go == null)
				return meshes;

			foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>())
			{
				meshes.Add(meshFilter.sharedMesh);
			}

			foreach (var skinnedMeshRenderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				meshes.Add(skinnedMeshRenderer.sharedMesh);
			}

			return meshes;
		}
	}
}