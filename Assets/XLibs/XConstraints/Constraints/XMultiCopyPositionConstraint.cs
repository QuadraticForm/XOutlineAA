using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Copy Position (Multi)")]
public class XMultiCopyPositionConstraint : XConstraintWithSingleSourceAndMultipleTarget
{
	[Header("Params")]

	public XVector3Bool axis = new XVector3Bool(true, true, true);
	public XVector3Bool invert = new XVector3Bool(false, false, false);

	public XConstraintsUtil.PositionMixMethod mix = XConstraintsUtil.PositionMixMethod.Replace;

	public TargetSpace targetSpace = TargetSpace.World;
	public Space sourceSpace = Space.World;

	public override void Resolve()
	{
		if (targets.Count != targetRests.Count)
			return;

		for (int i = 0; i < targets.Count; i++)
		{
			if (!targets[i].HasEffect)
				continue;

			XCopyPositionConstraint.Resolve(Influence * targets[i].influence, Source, sourceRest, sourceSpace,
				targets[i].transform, targetRests[i], targetSpace, axis, invert, mix);
		}
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
