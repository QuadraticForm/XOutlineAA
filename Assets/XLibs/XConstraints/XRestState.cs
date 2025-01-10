using UnityEngine;

public class XRestState : MonoBehaviour
{
	public bool resetToRestInUpdate = false;

	private Transform _transform; // cache transform for better performance

    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;

	public Matrix4x4 localToWorld; // TODO, this might be useless
	public Matrix4x4 localToParent;
	public Matrix4x4 parentToLocal;

	#region Better Names

	public Vector3 WorldPosition => position;
	public Quaternion WorldRotation => rotation;
	public Vector3 WorldScale => scale;

	public Vector3 PositionInParentSpace => localPosition;
	public Quaternion RotationInParentSpace => localRotation;

	public Vector3 ScaleInParentSpace => localScale;

	public Quaternion LocalToParentRotation => localRotation;

	public Quaternion ParentToLocalRotation => Quaternion.Inverse(localRotation);

	public Quaternion LocalToWorldRotation => rotation;

	public Quaternion WorldToLocalRotation => Quaternion.Inverse(rotation);

	#endregion

	#region Matrices
	// recalculated, slower than cached
	public Matrix4x4 _LocalToParent => Matrix4x4.TRS(localPosition, localRotation, localScale);
	// recalculated, slower than cached
	public Matrix4x4 _ParentToLocal => _LocalToParent.inverse;
	// recalculated, slower than cached
	public Matrix4x4 _LocalToWorld => Matrix4x4.TRS(position, rotation, scale);
	// recalculated, slower than cached
	public Matrix4x4 _WorldToLocal => localToWorld.inverse;

	#endregion

	private void Start()
	{
		Record();
	}

	public void Record()
    {
		_transform = transform;

        localPosition = _transform.localPosition;
        localRotation = _transform.localRotation.normalized;
        localScale = _transform.localScale;

		position = _transform.position;
		rotation = _transform.rotation.normalized;
		scale = _transform.lossyScale;

		// cache matrices

		localToWorld = _LocalToWorld;
		localToParent = _LocalToParent;
		parentToLocal = _ParentToLocal;
    }

	public void ResetToRest()
	{
		_transform.localPosition = localPosition;
		_transform.localRotation = localRotation;
		_transform.localScale = localScale;
	}

	private void Update()
	{
		if (resetToRestInUpdate)
			ResetToRest();
	}
}

public static class XRestStateTransformExt
{
	static public XRestState RecordRestState(this Transform transform, bool resetToRestInUpdate)
	{
		var restState = transform.GetComponent<XRestState>();

		if (restState == null)
		{
			restState = transform.gameObject.AddComponent<XRestState>();
			restState.Record();
		}

		restState.resetToRestInUpdate |= resetToRestInUpdate;

		return restState;
	}

	static public XRestState GetRestState(this Transform transform)
	{
		return transform.GetComponent<XRestState>();
	}
}