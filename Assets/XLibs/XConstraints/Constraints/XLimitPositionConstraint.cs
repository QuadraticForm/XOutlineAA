using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Limit Position")]
public class XLimitPositionConstraint : XConstraintWithSingleSource
{
	[Header("Params")]

	public XVector3Bool minAxis = XVector3Bool.Zero;
	public Vector3 min = Vector3.zero;
	public XVector3Bool maxAxis = XVector3Bool.Zero;
	public Vector3 max = Vector3.zero;

	public enum SpaceLimitedOptions
	{
		World = Space.World,
		LocalRest = Space.LocalRest,
		// LocalCurrent = Space.LocalCurrent, // this is meaningless here, cuz it's a constant
		Parent = Space.Parent,
	}

	public SpaceLimitedOptions space = SpaceLimitedOptions.LocalRest;

	static public void Resolve(
		float influence,
		Transform source, XRestState sourceRest,
		SpaceLimitedOptions space, XVector3Bool minAxis,
		Vector3 min, XVector3Bool maxAxis, Vector3 max)
	{
		if (source == null || sourceRest == null)
			return;

		var pos = XConstraintsUtil.GetPositionInSpace(source, (Space)space, sourceRest);
		var originalPos = pos;

		// do limit, TODO for my dear asistant

		if (minAxis.x && pos.x < min.x) pos.x = min.x;
        if (minAxis.y && pos.y < min.y) pos.y = min.y;
        if (minAxis.z && pos.z < min.z) pos.z = min.z;

        if (maxAxis.x && pos.x > max.x) pos.x = max.x;
        if (maxAxis.y && pos.y > max.y) pos.y = max.y;
        if (maxAxis.z && pos.z > max.z) pos.z = max.z;

		// set to source

		pos = Vector3.Lerp(originalPos, pos, influence);

		XConstraintsUtil.SetPositionInSpace(source, pos, (Space)space, sourceRest);
	}

	public override void Resolve()
	{
		Resolve(Influence, source, sourceRest, space, minAxis, min, maxAxis, max);
	}
}
