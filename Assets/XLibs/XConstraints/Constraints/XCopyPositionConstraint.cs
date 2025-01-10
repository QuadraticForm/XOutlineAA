using UnityEngine;

using PositionMixMethod = XConstraintsUtil.PositionMixMethod;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Copy Position")]
public class XCopyPositionConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]

	public XVector3Bool axis = new XVector3Bool(true, true, true);
	public XVector3Bool invert = new XVector3Bool(false, false, false);

	public PositionMixMethod mix = PositionMixMethod.Replace;

	public Space sourceSpace = Space.World;
	public TargetSpace targetSpace = TargetSpace.World;

	static public bool Resolve(
		float influence, Transform source, XRestState sourceRest, Space sourceSpace,
		Transform target, XRestState targetRest,
		TargetSpace targetSpace, XVector3Bool axis,
		XVector3Bool invert, PositionMixMethod mix)
	{
		if (source == null || target == null || sourceRest == null || targetRest == null)
			return false;

		// get target pos

		Space _targetSpace;
		bool sourceOrientation = false;

		if (targetSpace == TargetSpace.LocalRestWithSourceOrientation)
		{
			_targetSpace = Space.LocalRest;
			sourceOrientation = true;
		}
        else
        {
			_targetSpace = (Space)targetSpace;
        }

		var targetPos = XConstraintsUtil.GetPositionInSpace(target, _targetSpace, targetRest);

		// target pos source orientation correction

		if (sourceOrientation)
		{
			var targetToSource = sourceRest.parentToLocal * source.parent.worldToLocalMatrix * target.parent.localToWorldMatrix * targetRest.localToParent;

			targetPos = targetToSource.MultiplyVector(targetPos); // using multiply vector to ignore translation part in matrix
		}

		// invert

		targetPos = XConstraintsUtil.InvertChannels(targetPos, invert);

		// get source position and do mixing and influence

		var sourcePos = XConstraintsUtil.GetPositionInSpace(source, sourceSpace, sourceRest);
		
		var mixedPos = XConstraintsUtil.MixChannels(sourcePos, targetPos, mix, axis, influence);

		// set

		XConstraintsUtil.SetPositionInSpace(source, mixedPos, sourceSpace, sourceRest);

		return true;
	}

	public override void Resolve()
	{
		Resolve(Influence,
				Source, sourceRest, sourceSpace,
				target, targetRest, targetSpace,
				axis, invert, mix);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		resetSourcesToRestBeforeAnim = true; // test, this is enforced for this type of constraint
		// TODO this should be automatically set depending on constraint type, not by user
	}

	protected override void OnEditorUpdate()
	{
		base.OnEditorUpdate();

		resetSourcesToRestBeforeAnim = true; // test, this is enforced for this type of constraint
		// TODO this should be automatically set depending on constraint type, not by user
	}
}
