using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Two-Bone IK")]
public class XTwoBoneIKConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("IK Chain")]

	[XQuickButton("ResolveChainFromSource", "Resolve Chain From Source ↓")]
	public bool _resolveChainFromSource;

	public Transform root;
	public Transform mid;
	public Transform end;

	private XRestState rootRest;
	private XRestState midRest;
	private XRestState endRest;

	[Header("Params")]
	public Direction boneDirection = Direction.Y;


	public Transform pole;
	[Range(0, 1)]
	public float targetRotationWeight = 1;

#if UNITY_EDITOR
	[XQuickButton(new[] { "InitTargetAndPole", "SnapTargetAndPole" }, new[] { "🔄Init Target & Pole", "🧲 Snap Target & Pole" })]
	public bool _targetAndPoleButtonDummy; // button dummy field
#endif


	public void ResolveChainFromSource()
	{
		end = Source;
		mid = Source.parent;
		root = mid.parent;

		RecordRest();
	}


	public void InitTargetAndPole()
	{
		if (mid == null || end == null)
			return;

		UpdateRestLengths();

		var gizmoColor = XConstraintsUtil.GetDefaultColorBySide(end.name);
		var gizmoSize = totalRestLength * 0.15f;

		if (pole == null)
		{
			var obj = new GameObject("pole");
			pole = obj.transform;
			pole.parent = this.transform;
		}

		if (target == null)
		{
			var obj = new GameObject("Target");
			target = obj.transform;
			target.parent = this.transform;
		}

		XGizmo.AddOrSetGizmo(pole.gameObject, XGizmo.GizmoType.cube, gizmoColor, null, gizmoSize);
		XGizmo.AddOrSetGizmo(target.gameObject, XGizmo.GizmoType.cube, gizmoColor, null, gizmoSize);

		XConstraintsUtil.Snap(pole, mid);
		XConstraintsUtil.Snap(target, end);
	}


	public void SnapTargetAndPole()
	{
		if (mid == null || end == null)
			return;

		XConstraintsUtil.Snap(pole, mid);
		XConstraintsUtil.Snap(target, end);
	}

	[Header("Stretching")]
	[Min(1)]
	public float maxScale = 1;

	public float crossSectionScaleFactor = 1;

	[Header("Debug")]
	public bool _debug = false;
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public float[] restLengths = new float[2] { 0, 0};
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public float totalRestLength = 0;
	[XConditionalHide("_debug", true, XConditionalHideAttribute.CompareMethod.NotEqual), XReadOnly]
	public Vector3 restScale = Vector3.one;


	public static void SolveTwoBoneIK(Vector3 rootPos, ref Vector3 midPos, ref Vector3 endPos, Vector3 targetPos, Vector3? optionalPolePos = null)
    {
        // If no explicit pole is provided, calculate a default one using mid position and target direction
        Vector3 defaultPolePos = rootPos + (midPos - rootPos).normalized + Vector3.Cross((targetPos - rootPos).normalized, (midPos - rootPos).normalized) * 0.1f;
        Vector3 polePos = optionalPolePos ?? defaultPolePos;

        // Calculate lengths of each bone segment
        float upperArmLength = Vector3.Distance(rootPos, midPos);
        float forearmLength = Vector3.Distance(midPos, endPos);
        float armLength = upperArmLength + forearmLength;

		// Check if target position is the same as root position
        if (Vector3.Distance(rootPos, targetPos) < 0.000001f)
        {
            // Maintain the current pose or handle gracefully when target is at root position
            midPos = rootPos + (midPos - rootPos).normalized * upperArmLength;
            endPos = midPos + (endPos - midPos).normalized * forearmLength;
            return;
        }

        // Calculate distance from root to target and clamp if necessary
        Vector3 targetVector = targetPos - rootPos;
        float targetDistance = Mathf.Min(targetVector.magnitude, armLength);

        // Normalize direction vector from root to target
        Vector3 directionToTarget = targetVector.normalized;

        // Law of cosines to find joint angles
        float angleA = LawOfCosines(upperArmLength, targetDistance, forearmLength);
        float angleB = LawOfCosines(forearmLength, upperArmLength, targetDistance);

        // Determine the vector from root to mid position
        Vector3 planeNormal = Vector3.Cross(directionToTarget, (polePos - rootPos).normalized).normalized; // Flipped normal

        // Calculate desired position for the elbow (mid point)
        Quaternion rootToElbowRotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angleA, planeNormal);
        Vector3 elbowDir = rootToElbowRotation * directionToTarget;
        midPos = rootPos + elbowDir * upperArmLength;

        // Calculate desired position for the end effector
        endPos = midPos + (targetPos - midPos).normalized * forearmLength;
    }

    public static float LawOfCosines(float a, float b, float c)
    {
        // Safeguard to ensure acos input is within valid range [-1,1]
        float cosAngle = Mathf.Clamp((a * a + b * b - c * c) / (2 * a * b), -1.0f, 1.0f);

        return Mathf.Acos(cosAngle);
    }

	public bool ChainValid => root != null && mid != null && end != null;

	public override bool CanResolve => base.CanResolve && ChainValid;

	public void UpdateRestLengths()
	{
		if (!ChainValid) return;

		restLengths[0] = (mid.position - root.position).magnitude;
		restLengths[1] = (end.position - mid.position).magnitude;

		totalRestLength = restLengths[0] + restLengths[1];

		restScale = root.localScale;
	}

	public override void RecordRest()
	{
		base.RecordRest();

		rootRest = root.RecordRestState(resetSourcesToRestBeforeAnim);
		midRest = mid.RecordRestState(resetSourcesToRestBeforeAnim);
		endRest = end.RecordRestState(resetSourcesToRestBeforeAnim);

		UpdateRestLengths();

		restRecorded = true;
	}

	void ApplyStretching()
	{
		if (!CanResolve) return;

		// apply stretching

		// calculate scale along bone direction

		var currentDist = (target.position - root.position).magnitude;

		var scale = currentDist / Mathf.Max(totalRestLength, 0.00001f);

		scale = Mathf.Clamp(scale, 1.0f, maxScale);

		// calculate scale on cross section plane

		var crossSectionScale = Mathf.Pow(scale, crossSectionScaleFactor);

		// 3d scale vec

		var scaleVec = new Vector3(crossSectionScale, crossSectionScale, crossSectionScale);

		if (boneDirection == Direction.X || boneDirection == Direction.NegativeX)
			scaleVec.x = scale;
		else if (boneDirection == Direction.Y || boneDirection == Direction.NegativeY)
			scaleVec.y = scale;
		else
			scaleVec.z = scale;

		// scale the root bone

		root.localScale = Vector3.Scale(restScale, scaleVec);

		// recalibrate mid and end position due to accumulated precision loss?

		mid.position = root.position + (mid.position - root.position).normalized * restLengths[0] * scale;
		end.position = mid.position + (end.position - mid.position).normalized * restLengths[1] * scale;

		// TODO Consider the scenario where the bones in the IK chain do not strictly follow the parent-child relationship


	}

	public void ApplyIK()
	{
		if (!CanResolve) return;

		var polePos = pole ? pole.position : mid.position;
		var midPos = mid.position;
		var endPos=  end.position;


		SolveTwoBoneIK(root.position, ref midPos, ref endPos, target.position, polePos);


		// order below is important

		// rotate root
		XDampedTrackConstraint.ApplyTo(_influence, root, midPos, boneDirection);
		// move mid
		mid.position = midPos;

		// rotate mid
		XDampedTrackConstraint.ApplyTo(_influence, mid, endPos, boneDirection);
		// move end
		end.position = endPos;
	}

	public void ApplyRotation()
	{
		end.rotation = Quaternion.Slerp(end.rotation, target.rotation, Influence * targetRotationWeight);
	}

	public override void Resolve()
	{
		if (!restRecorded)
		{
			Debug.LogError("XConstraint: Execute While Rest Not Recorded!");
			return;
		}

		ApplyStretching();

		ApplyIK();

		ApplyRotation();
	}
}
