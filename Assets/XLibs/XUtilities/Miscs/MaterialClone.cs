using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MaterialClone : MonoBehaviour
{
	public Transform source;

    public static void CloneMaterials(Transform source, Transform dest)
	{
        // clone materials

        var dsmr = dest.GetComponent<SkinnedMeshRenderer>();
        var ssmr = source.GetComponent<SkinnedMeshRenderer>();

        if (ssmr != null && dsmr != null)
		{
            dsmr.sharedMaterials = ssmr.sharedMaterials;
        }

        // traverse deeper down 

        for (int ci = 0; ci < source.childCount; ++ci)
        {
            var childSource = source.GetChild(ci);

            var childDest = dest.Find(childSource.name);
            if (childDest != null)
                CloneMaterials(childSource, childDest);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(MaterialClone))]
public class MaterialCloneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var materialClone = target as MaterialClone;

        if (materialClone.source == null)
            return;

        if (GUILayout.Button("Clone"))
        {
            MaterialClone.CloneMaterials(materialClone.source, materialClone.transform);
        }
    }
}
#endif