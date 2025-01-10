using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Copy Rotation (Multi)")]
public class XMultiCopyRotationConstraint : XConstraintWithSingleSourceAndMultipleTarget
{
	[Header("Params")]

	public XVector3Bool axis = new XVector3Bool(true, true, true);
	public XVector3Bool invert = new XVector3Bool(false, false, false);

	public XCopyRotationConstraint.Mix mix = XCopyRotationConstraint.Mix.Replace;

	public TargetSpace targetSpace = TargetSpace.World;
	public Space sourceSpace = Space.World;

	public override void Resolve()
	{
		for (int i = 0; i < targets.Count; i++)
		{
			if (!targets[i].HasEffect) continue;

			XCopyRotationConstraint.Resolve(Influence * targets[i].influence, axis, invert, mix,
					Source, targets[i].transform, sourceSpace, targetSpace, sourceRest, targetRests[i]);
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
