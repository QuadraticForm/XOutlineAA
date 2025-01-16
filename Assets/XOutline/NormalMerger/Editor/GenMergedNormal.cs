using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace xoutline
{
	static class GenMergedNormal
	{
		private const int BATCH_COUNT = 256; // How many vertices a job processes
		private const int MAX_COINCIDENT_VERTICES = 32;

		private struct CollectWeightedNormalJob : IJobParallelFor
		{
			[ReadOnly] internal NativeArray<int> indices;
			[ReadOnly] internal NativeArray<Vector3> vertices;

			[NativeDisableParallelForRestriction] internal NativeArray<UnsafeParallelHashMap<Vector3, float3>.ParallelWriter> 
				outPositionNormalHashMaps;

			void IJobParallelFor.Execute(int vim)
			{
				// vim = vertex index in sub-mesh
				int vit = vim % 3; // vertex index in triangle

				var position = vertices[indices[vim]];

				var p1 = vertices[indices[vim - vit]];
				var p2 = vertices[indices[vim - vit + 1]];
				var p3 = vertices[indices[vim - vit + 2]];

				CalculateWeightedAngle(p1, p2, p3, vit, out var normal, out var angle);
				var angleWeightedNormal = normal * angle;

				for (int i = 0; i < outPositionNormalHashMaps.Length + 1; i++)
				{
					if (i == outPositionNormalHashMaps.Length)
					{
						Debug.LogError(
							$"[GenMergedNormal] The number of coincident vertices ({i}) exceeds the limit ({MAX_COINCIDENT_VERTICES})!");
						break;
					}

					if (outPositionNormalHashMaps[i].TryAdd(position, angleWeightedNormal))
					{
						break;
					}
				}
			}
		}

		private struct GenMergedNormalJob : IJobParallelFor
		{
			[ReadOnly] internal NativeArray<int> indices;
			[ReadOnly] internal NativeArray<Vector3> vertices, normals;
			[ReadOnly] internal NativeArray<Vector4> tangents;
			[ReadOnly] internal NativeArray<UnsafeParallelHashMap<Vector3, float3>> positionNormalHashMaps;

			[NativeDisableParallelForRestriction] internal NativeArray<Vector2> mergedNormalsTs; // in tangent space, so only xy

			void IJobParallelFor.Execute(int vertexIndexInSubMesh)
			{
				var vertexIndex = indices[vertexIndexInSubMesh];
				var position = vertices[vertexIndex];

				// Merge all collected normals

				float3 mergedNormal = 0;
				{
					for (int i = 0; i < positionNormalHashMaps.Length; i++)
					{
						if (positionNormalHashMaps[i].TryGetValue(position, out var angleWeightedNormal))
							mergedNormal += angleWeightedNormal;
						else
							break;
					}
					mergedNormal = math.normalizesafe(mergedNormal);
				}

				// convert merged normal to tangent space

				float3 mergedNormalTs = 0;
				{
					float3 normal = math.normalizesafe(normals[vertexIndex]);
					float4 tangent = tangents[vertexIndex];
					tangent.xyz = math.normalizesafe(tangent.xyz);
					var binormal = math.normalizesafe(math.cross(normal, tangent.xyz) * tangent.w);

					var tangentToObject = new float3x3(
												tangent.xyz,
												binormal,
												normal);

					var objectToTangent = math.transpose(tangentToObject);
					mergedNormalTs = math.normalizesafe(math.mul(objectToTangent, mergedNormal));
				}

				mergedNormalsTs[vertexIndex] = mergedNormalTs.xy;
			}
		}

		internal static void GenAndSave(List<Mesh> meshes, int uvChannel)
		{
			for (int i = 0; i < meshes.Count; i++)
			{
				GenAndSave(meshes[i], uvChannel);
			}
		}

		internal static void GenAndSave(Mesh mesh, int uvChannel)
		{
			var vertices = mesh.vertices;
			var normals = mesh.normals;
			var tangents = mesh.tangents;
			var vertexCount = mesh.vertexCount;

			NativeArray<Vector3> nativeVertices = new(vertices, Allocator.TempJob);
			NativeArray<Vector3> nativeNormals = new(normals, Allocator.TempJob);
			NativeArray<Vector4> nativeTangents = new(tangents, Allocator.TempJob);
			NativeArray<Vector2> mergedNormalTsArray = new(vertexCount, Allocator.TempJob);

			for (int j = 0; j < mesh.subMeshCount; j++)
			{
				var indices = mesh.GetIndices(j);
				var subMeshVertexCount = indices.Length;

				NativeArray<int> nativeIndices = new(indices, Allocator.TempJob);

				NativeArray<UnsafeParallelHashMap<Vector3, float3>> 
					nativePositionNormalHashMapArray = new(MAX_COINCIDENT_VERTICES, Allocator.TempJob);

				NativeArray<UnsafeParallelHashMap<Vector3, float3>.ParallelWriter> 
					nativePositionNormalHashMapWriterArray = new(MAX_COINCIDENT_VERTICES, Allocator.TempJob);

				for (int k = 0; k < MAX_COINCIDENT_VERTICES; k++)
				{
					UnsafeParallelHashMap<Vector3, float3> nativePositionNormalHashMap = new(subMeshVertexCount, Allocator.TempJob);
					nativePositionNormalHashMapArray[k] = nativePositionNormalHashMap;
					nativePositionNormalHashMapWriterArray[k] = nativePositionNormalHashMap.AsParallelWriter();
				}

				// Collect weighed normals
				JobHandle collectNormalJobHandle;
				{
					var collectSmoothedNormalJobData = new CollectWeightedNormalJob
					{
						indices = nativeIndices,
						vertices = nativeVertices,
						outPositionNormalHashMaps = nativePositionNormalHashMapWriterArray
					};
					collectNormalJobHandle = collectSmoothedNormalJobData.Schedule(subMeshVertexCount, BATCH_COUNT);
				}

				// Bake smoothed normal TS to vertex color
				var bakeNormalJobData = new GenMergedNormalJob
				{
					indices = nativeIndices,
					vertices = nativeVertices,
					normals = nativeNormals,
					tangents = nativeTangents,
					positionNormalHashMaps = nativePositionNormalHashMapArray,
					mergedNormalsTs = mergedNormalTsArray
				};
				bakeNormalJobData.Schedule(subMeshVertexCount, BATCH_COUNT, collectNormalJobHandle).Complete();

				// Clear
				for (int k = 0; k < MAX_COINCIDENT_VERTICES; k++)
				{
					nativePositionNormalHashMapArray[k].Dispose();
				}
				nativeIndices.Dispose();
				nativePositionNormalHashMapArray.Dispose();
				nativePositionNormalHashMapWriterArray.Dispose();
			}

			// Save
			SaveToMesh(mesh, uvChannel, ref mergedNormalTsArray);
			mesh.MarkModified();

			// Clear
			nativeVertices.Dispose();
			nativeNormals.Dispose();
			nativeTangents.Dispose();
			mergedNormalTsArray.Dispose();
		}

		private static void SaveToMesh(Mesh mesh, int uvChannel, ref NativeArray<Vector2> smoothedNormalTangentSpace)
		{
			// UV1 is mesh.uv2, UV2 is mesh.uv3, UV3 is mesh.uv4, thank you unity for the inconsistency
			if (uvChannel == 1)
				mesh.uv2 = smoothedNormalTangentSpace.ToArray();
			else if (uvChannel == 2)
				mesh.uv3 = smoothedNormalTangentSpace.ToArray();
			else if (uvChannel == 3)
				mesh.uv4 = smoothedNormalTangentSpace.ToArray();
		}

		// https://tajourney.games/7689/
		private static void CalculateWeightedAngle(float3 p1, float3 p2, float3 p3, int currentIndexInTriganle,
			out float3 outNormal, out float outAngle)
		{
			float3 d1 = 0;
			float3 d2 = 0;

			switch (currentIndexInTriganle)
			{
				case 0:
					d1 = p1 - p3;
					d2 = p2 - p1;
					break;
				case 1:
					d1 = p2 - p1;
					d2 = p3 - p2;
					break;
				case 2:
					d1 = p3 - p2;
					d2 = p1 - p3;
					break;
			}

			d1 = math.normalizesafe(d1);
			d2 = math.normalizesafe(d2);

			outNormal = math.normalizesafe(math.cross(p1 - p3, p2 - p1));
			outAngle = math.acos(math.clamp(math.dot(d1, -d2), -1, 1));
		}
	}
}