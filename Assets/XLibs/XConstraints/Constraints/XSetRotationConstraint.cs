using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Set Rotation")]
public class XSetRotationConstraint : XConstraintWithSingleSource
{
    public Vector3 euler = Vector3.zero;

	[XQuickButton("RecordRestRotation")]
	public bool recordRestRotation = false;

	public void RecordRestRotation()
	{
		euler = Source.transform.localEulerAngles;
	}

    public override void Resolve()
	{
		// TODO implement influence

        Source.transform.localEulerAngles = euler;
    }
}
