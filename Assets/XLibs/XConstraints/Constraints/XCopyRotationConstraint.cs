using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Copy Rotation")]
public class XCopyRotationConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	public enum Mix
	{
		Replace = 0,
		// TODO, in blender "Add" is done by adding euler angles,
		// which is of course axis order dependant, which is quite a hassle to implement,
		// so this just is the same as BeforeOriginal for now
		Add = 1, 
		/// <summary>
		/// target * source, same as in blender, as if target is source's parent
		/// </summary>
		BeforeOriginal = 2,
		/// <summary>
		/// source * target, same as in blender, as if target is source's child
		/// however, since source's rotation doesn't affect target, 
		/// this seems unintuitive and doesn't seem useful
		/// </summary>
		AfterOriginal = 3,
	}

	[Header("Params")]

	public XVector3Bool axis = new XVector3Bool(true, true, true);
	public XVector3Bool invert = new XVector3Bool(false, false, false);

	public Mix mix = Mix.Replace;

	public Space sourceSpace = Space.World;
	public TargetSpace targetSpace = TargetSpace.World;

	// TODO is this correct?
	static Quaternion _Mix(Quaternion original, Quaternion _new, Mix mix, XVector3Bool mask)
	{
		Quaternion result;

		if (mix == Mix.Replace)
			result = _new;
		// TODO, in blender "Add" is done by adding euler angles,
		// which is of course axis order dependant, which is quite a hassle to implement,
		// so this just is the same as BeforeOriginal for now
		else if (mix == Mix.Add || mix == Mix.BeforeOriginal)
		{
			result = _new * original; // NOTE: the order is not mistaken
		}
		else if (mix == Mix.AfterOriginal)
		{
			result = original * _new;
		}
		else
			result = _new;

		// TODO, is this correct?
		return XConstraintsUtil.MaskChannels(result, original, mask);
	}

	static public void Resolve(
		float influence, XVector3Bool axis, XVector3Bool invert, Mix mix,
		Transform source, Transform target, 
		Space sourceSpace, TargetSpace targetSpace,
		XRestState sourceRest, XRestState targetRest)
	{
		if (source == null || target == null)
			return;

		var targetRot = Quaternion.identity;

		// copy from target

		if (targetSpace == TargetSpace.World)
		{
			targetRot = target.rotation;
		}
		else if (targetSpace == TargetSpace.LocalRest)
		{
			// target.localRotation is target's rotation in its parent's space,
			// transform it into the local space

			targetRot = targetRest.ParentToLocalRotation * target.localRotation;
		}
		else if (targetSpace == TargetSpace.LocalRestWithSourceOrientation)
		{
			// target.localRotation is target's rotation in its parent's space
			// transform it into the local space

			targetRot = targetRest.ParentToLocalRotation * target.localRotation;

			// transform to source's orientation

			// this is rest target to source, not current target to source
			// var targetToSource = sourceRest.WorldToLocalRotation * targetRest.LocalToWorldRotation;

			// this seems right, but no
			// cuz when we say "local space", we actually mean "REST local space"
			// and targetRot above is now in "REST local space"
			// var targetToSource = source.rotation * target.rotation;

			// this is correct
			// var targetToSource = sourceRest.ParentToLocal * source.parent.worldToLocalMatrix * target.parent.localToWorldMatrix * targetRest.LocalToParent;
			var targetToSource = sourceRest.parentToLocal.rotation * source.parent.worldToLocalMatrix.rotation * target.parent.localToWorldMatrix.rotation * targetRest.localToParent.rotation;

			// this is wrong
			// we are calculating the delta rotation, so when targetRot is identity, result should also be identity
			// targetRot = restTargetToSource.rotation * targetRot; 

			targetRot = targetToSource * targetRot * Quaternion.Inverse(targetToSource);
		}

		// invert

		targetRot = XConstraintsUtil.InvertChannels(targetRot, invert);

		// set to source

		if (sourceSpace == Space.World) // targetRot is treated as in world space
		{
			var mixedRot = _Mix(source.rotation, targetRot, mix, axis);

			// apply to source
			source.rotation = Quaternion.Slerp(source.rotation, mixedRot, influence);
		}
		else if (sourceSpace == Space.LocalRest) // targetRot is treated as in source's "local space"
			// relative to local rest system, 
			// this is a system as if object's TRS hasn't changed since start
		{
			// get current source's rotation in its "local rest space"
			var sourceRot = sourceRest.ParentToLocalRotation * source.localRotation;
			
			// do the mixing in "local rest space",
			// NOTE: if mixing is done using localRotation, which is in parent space, we won't get the same result as in blender
			var mixedRotation = _Mix(sourceRot, targetRot, mix, axis);

			// transform position in "local space" to "parent space"
			mixedRotation = sourceRest.LocalToParentRotation * mixedRotation;

			// apply to source, // source.localRotation is in "parent space", so apply to source.localRotation
			source.localRotation = Quaternion.Slerp(source.localRotation, mixedRotation, influence);
		}
		else if (sourceSpace == Space.LocalCurrent)
		{
			// current source's rotation in its local space is identity
			var sourceRot = Quaternion.identity;

			var mixedRotation = _Mix(sourceRot, targetRot, mix, axis);

			// transform position in "local space" to "parent space"
			mixedRotation = source.localRotation * mixedRotation;

			// apply to source, // source.localRotation is in "parent space", so apply to source.localRotation
			source.localRotation = Quaternion.Slerp(source.localRotation, mixedRotation, influence);
		}
		else
		{
			// TODO
		}

		// TODO
	}

	public override void Resolve()
	{
		Resolve(Influence, axis, invert, mix,
				Source, target, sourceSpace, targetSpace, sourceRest, targetRest);
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
