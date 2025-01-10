using UnityEngine;

[ExecuteAlways]

public class SkeletonSync : MonoBehaviour
{
#if UNITY_EDITOR

	[XQuickButton(new[] {"Build", "Clean"})]
	public bool _buildCleanButtonDummy;


#endif

	static void AddTransformReferenceForHierarchy(GameObject gameObject)
    {
        for (int i = 0; i < gameObject.transform.childCount; ++i)
        {
            var child = gameObject.transform.GetChild(i).gameObject;
            if (child.GetComponent<TransformSync>() == null)
                AddTransformReferenceForHierarchy(child);
        }
    }

    public Transform target = null;

    static void UpdateTransformSyncs(Transform targetSkeletonRoot, GameObject gameObject, GameObject exclude, bool forceTarget = false)
	{
        if (targetSkeletonRoot == null || gameObject == null || gameObject == exclude) return;

        var reference = gameObject.GetComponent<TransformSync>() ?? gameObject.AddComponent<TransformSync>();

        if (forceTarget || targetSkeletonRoot.name == gameObject.name)
            reference.target = targetSkeletonRoot;
        else
            reference.target = targetSkeletonRoot.Find(gameObject.name);

        for (int i = 0; i < gameObject.transform.childCount; ++i)
            UpdateTransformSyncs(reference.target, gameObject.transform.GetChild(i).gameObject, exclude);
    }

    public void Build()
	{
        UpdateTransformSyncs(target, gameObject, null, true);
    }

    static void DeleteTransformSyncs(GameObject gameObject)
	{
        var transformSyncs = gameObject.GetComponents<TransformSync>();

        foreach (var e in transformSyncs)
        {
#if UNITY_EDITOR
            Component.DestroyImmediate(e);
#else
            Component.Destroy(e);
#endif
        }

        for (int i = 0; i < gameObject.transform.childCount; ++i)
            DeleteTransformSyncs(gameObject.transform.GetChild(i).gameObject);
    }

    public void Clean()
	{
        DeleteTransformSyncs(gameObject);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void LateUpdate()
	{
    }
}
