using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Stretch To")]
public class XStretchToConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]
	public Direction trackDirection = Direction.Y;

	[Tooltip("1:grow, 0:none, -1:shrink")]
	public Vector3 scaleFactor = Vector3.one;

#if UNITY_EDITOR
	[Header("Debug")]
	public bool _debug = false;

	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public float restDistance = 0;
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public Vector3 restScale = Vector3.one;
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public float _currentDistance = 0;
#else

	public float restDistance = 0;
	public Vector3 restScale = Vector3.one;

#endif

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

	public override void Resolve()
	{
		if (!restRecorded)
		{
			Debug.LogError("XConstraint: Execute While Rest Not Recorded!");
			return;
		}

		XDampedTrackConstraint.ApplyTo(Influence, Source, target.position, trackDirection);

		XScaleByDistanceConstraint.ApplyTo(Influence, Source, target.position, scaleFactor, restDistance, restScale);
	}

#if UNITY_EDITOR
	protected override void OnEditorUpdate()
	{
		base.OnEditorUpdate();

		if (!_debug)
			return;

		if (target != null && source != null)
			_currentDistance = (target.position - Source.position).magnitude;
	}
#endif

}
