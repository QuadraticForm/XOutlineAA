using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Child Of")]
public class XChildOfConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Channel Masks")]
	public XVector3Bool positionMask = XVector3Bool.True;
	public XVector3Bool rotationMask = XVector3Bool.True;
	public XVector3Bool scaleMask = XVector3Bool.True;

	private Matrix4x4 restParentToTarget;	// might get affected if allowEditorManipulation

	/// <summary>
	/// record relative between source & target
	/// </summary>
	void RecordRestRelativeToTarget()
	{
		if (target != null && Source != null)
        {
			var parent = Source.parent;

			// var worldToParent = parent ? parent.worldToLocalMatrix : Matrix4x4.identity;
			var parentToWorld = parent ? parent.localToWorldMatrix : Matrix4x4.identity;

			// restSourceToParent = worldToParent * Source.localToWorldMatrix;
			restParentToTarget = target.worldToLocalMatrix * parentToWorld;
            // restSourceToTarget = target.worldToLocalMatrix * Source.localToWorldMatrix;
        }
        else
        {
            Debug.LogWarning("Target or Source is not assigned.");
        }
	}

	Matrix4x4 GetParentToTarget()
	{
		if (target != null && Source != null)
        {
			var parent = Source.parent;

			var parentToWorld = parent ? parent.localToWorldMatrix : Matrix4x4.identity;

			return target.worldToLocalMatrix * parentToWorld;
        }
        else
        {
			return Matrix4x4.identity;
        }
	}

    public override void RecordRest()
    {
        base.RecordRest();		// record rest relative to real parent, which is source's local transform

		restParentToTarget = GetParentToTarget();

		restRecorded = true;
    }

	public override void Resolve()
	{
		if (targetRest == null)
		{
			Debug.LogError("XConstraint: Execute While Rest Not Recorded!");
			return;
		}

		if (target == null || Source == null)
			return;

        // Calculate the desired world matrix as if the source is a child of the target
		//

		// this won't allow Source to move by itself, so change to the method below
        // Matrix4x4 sourceToWorld = target.localToWorldMatrix * restSourceToTarget;

		var maskedTargetPos = XConstraintsUtil.MaskChannels(target.position, targetRest.position, positionMask);
		var maskedTargetRot = XConstraintsUtil.MaskChannels(target.rotation, targetRest.rotation, rotationMask);
		var maskedTargetScl = XConstraintsUtil.MaskChannels(target.lossyScale, targetRest.scale, scaleMask);

		var currentTargetToWorld = Matrix4x4.TRS(maskedTargetPos, maskedTargetRot, maskedTargetScl);

		var currentSourceToParent = Source.LocalToParentMatrix(); // will be reset in Update, then over-writen in Animation

		var currentSourceToWorld = currentTargetToWorld * restParentToTarget * currentSourceToParent;

        // Interpolating position, rotation, and scale based on influence
        Source.position = Vector3.Lerp(Source.position, currentSourceToWorld.GetPosition(), Influence);
        Source.rotation = Quaternion.Slerp(Source.rotation, currentSourceToWorld.rotation, Influence);
        Source.localScale = Vector3.Lerp(Source.localScale, currentSourceToWorld.lossyScale.Divide(Source.parent.lossyScale), Influence);
    }
}