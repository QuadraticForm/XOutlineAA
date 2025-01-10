using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Rotation Distributor")]
public class XRotationDistributorConstraint : XConstraintWithMultipleSourceAndSingleTarget
{
	[Header("Params")]

	public XVector3Bool axis = new XVector3Bool(true, true, true);
	public XVector3Bool invert = new XVector3Bool(false, false, false);

	public XCopyRotationConstraint.Mix mix = XCopyRotationConstraint.Mix.Replace;

	public Space sourceSpace = Space.World;
	public TargetSpace targetSpace = TargetSpace.World;

	public override void Resolve()
	{
		base.Resolve();

		if (targetRest == null) return;

		if (Sources.Count != sourceRests.Count) return;

		for (int i = 0; i < Sources.Count; i++)
		{
			var source = Sources[i].transform;
			var sourceInfluence = Sources[i].influence;

			XCopyRotationConstraint.Resolve(
				Influence * sourceInfluence, axis, invert, mix,
				source, target, sourceSpace, targetSpace,
				sourceRests[i], targetRest);
		}
	}
}
