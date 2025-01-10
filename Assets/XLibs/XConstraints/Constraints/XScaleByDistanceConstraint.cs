using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Scale By Distance")]
public class XScaleByDistanceConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]
	[Tooltip("1:grow, 0:none, -1:shrink")]
    public Vector3 scaleFactor = Vector3.one;

	[XReadOnly]
    public float restDistance = 0;
	[XReadOnly]
	public Vector3 restScale = Vector3.one;
	[XReadOnly]
	public float _currentDistance = 0;


	public override void RecordRest()
	{
		base.RecordRest();

		if (CanResolve)
		{
			restDistance = (target.position - Source.position).magnitude;
			restScale = Source.localScale;
		}

		restRecorded = true;
	}

	static public void ApplyTo(float influence, Transform source, Vector3 targetPosition, Vector3 scaleFactor, float restDistance, Vector3 restScale)
	{
		var currentDistance = (targetPosition - source.position).magnitude;

		// scale by distance
		var scale = currentDistance / (restDistance + 0.000001f);

		// scale axis
		var scaleVec = new Vector3(Mathf.Pow(scale, scaleFactor.x), Mathf.Pow(scale, scaleFactor.y), Mathf.Pow(scale, scaleFactor.z));

		// scale * rest
		scaleVec = Vector3.Scale(scaleVec, restScale);

		// apply influence
		scaleVec = Vector3.Lerp(restScale, scaleVec, influence);

		source.localScale = scaleVec;
	}

	public override void Resolve()
	{
		if (!restRecorded)
		{
			Debug.LogError("XConstraint: Execute While Rest Not Recorded!");
			return;
		}

		ApplyTo(Influence, Source, target.position, scaleFactor, restDistance, restScale);
	}

#if UNITY_EDITOR
	protected override void OnEditorUpdate()
	{
		base.OnEditorUpdate();

		if (CanResolve)
			_currentDistance = (target.position - transform.position).magnitude;
	}
#endif
}
