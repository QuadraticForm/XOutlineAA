using UnityEngine;

[ExecuteAlways]
public class XFlipMeshFaces : MonoBehaviour
{
	[Tooltip("a poor man's button, click this, FlipMeshFaces() will be called once")]
	public bool flipFaces = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	void FlipMeshFaces()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter != null)
		{
			// Destroy the previous mesh instance if it exists
			//if (meshFilter.mesh != null)
			//{
			//	DestroyImmediate(meshFilter.mesh);
			//}

			// Create a copy of the mesh to avoid modifying the shared mesh
			Mesh mesh = Instantiate(meshFilter.sharedMesh);
			meshFilter.mesh = mesh;

			Vector3[] normals = mesh.normals;
			for (int i = 0; i < normals.Length; i++)
			{
				normals[i] = -normals[i];
			}
			mesh.normals = normals;

			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				int[] triangles = mesh.GetTriangles(i);
				for (int j = 0; j < triangles.Length; j += 3)
				{
					int temp = triangles[j];
					triangles[j] = triangles[j + 1];
					triangles[j + 1] = temp;
				}
				mesh.SetTriangles(triangles, i);
			}
		}
		flipFaces = false;
	}

	// Update is called once per frame
	void Update()
	{
		// If the flipFaces flag is true, flip the mesh faces
		if (flipFaces)
			FlipMeshFaces();
	}
}
