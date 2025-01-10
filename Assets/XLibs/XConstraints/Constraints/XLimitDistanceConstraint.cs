using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Limit Distance")]
public class XLimitDistanceConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]
	[Min(0)]
	public float distance = 0;

	public enum ClampRegion
	{
		Inside = 0,
		Outside = 1,
		OnSurface = 2,
	}

	public ClampRegion clampRegion = ClampRegion.Inside;

	static public void Resolve(
		float influence,
		Transform source, 
		Vector3 targetPos,
		float distance,
		ClampRegion clampRegion)
	{
		if (source == null)
			return;

		var delta = source.position - targetPos;

		var currentDistance = delta.magnitude;

		if (clampRegion == ClampRegion.Inside)
			currentDistance = Mathf.Min(currentDistance, distance);
		else if (clampRegion == ClampRegion.Outside)
			currentDistance = Mathf.Max(currentDistance, distance);
		else if (clampRegion == ClampRegion.OnSurface)
			currentDistance = distance;

		var limitedPosition = delta.normalized * currentDistance + targetPos;

		source.position = Vector3.Lerp(source.position, limitedPosition, influence);
	}

	public override void Resolve()
	{
		Vector3 targetPos;
		
		if (target == null || target == source)
			// no valid target, distance to rest state
		{
			targetPos = Source.parent.localToWorldMatrix.MultiplyPoint(sourceRest.PositionInParentSpace);
		}
        else
			// distance to target
        {
            targetPos = target.position;
        }

		Resolve(Influence, source, targetPos, distance, clampRegion);
	}
}
