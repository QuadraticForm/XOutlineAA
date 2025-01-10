using UnityEngine;

using TwistCorrectionSpace = XTwistCorrectionConstraint.TwistCorrectionSpace;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Twist Correction Constraint (Multi)")]
public class XMultiTwistCorrectionConstraint : XConstraintWithMultipleSourceAndSingleTarget
{
    [Header("Params")]

    public Axis axis = Axis.Y;

	public TwistCorrectionSpace space = TwistCorrectionSpace.LocalRest;
    
    public override void Resolve()
	{
	    if (space == TwistCorrectionSpace.LocalRest)
		{
			if (sources.Count != sourceRests.Count)
				return;

			for (int i = 0; i < sources.Count; ++i)
			{
				if (!sources[i].HasEffect)
					continue;

				XTwistCorrectionConstraint.ApplyLocalSpace
					(Influence * sources[i].influence, sources[i].transform, target, axis, sourceRests[i].localRotation, targetRest.localRotation);
			}
		}
		else if (space == TwistCorrectionSpace.Parent)
		{
			foreach (var entry in Sources)
			{
				if (entry.HasEffect)
					XTwistCorrectionConstraint.ApplyParentSpace(Influence * entry.influence, entry.transform, target, axis);
			}
		}
		else if (space == TwistCorrectionSpace.World)
		{
			foreach (var entry in Sources)
			{
				if (entry.HasEffect)
					XTwistCorrectionConstraint.ApplyWorldSpace(Influence * entry.influence, entry.transform, target, axis);
			}
        }
	}
}