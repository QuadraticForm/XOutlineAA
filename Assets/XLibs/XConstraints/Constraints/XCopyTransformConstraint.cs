using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Copy Transform")]
public class XCopyTransformConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	public enum Mix
	{
		Replace = 0,
		// TODO, in blender "Add" is done by adding euler angles,
		// which is of course axis order dependant, which is quite a hassle to implement,
		// so this just is the same as BeforeOriginal for now
	//	Add = 1, 
		/// <summary>
		/// target * source, same as in blender, as if target is source's parent
		/// </summary>
	//	BeforeOriginal = 2,
		/// <summary>
		/// source * target, same as in blender, as if target is source's child
		/// however, since source's rotation doesn't affect target, 
		/// this seems unintuitive and doesn't seem useful
		/// </summary>
	//	AfterOriginal = 3,
	}

	[Header("Params")]

	public bool copyPosition = true;
	public bool copyRotation = true;

	public Mix mix = Mix.Replace;

	public Space sourceSpace = Space.World;
	public TargetSpace targetSpace = TargetSpace.World;

	public override void Resolve()
	{
		if (copyRotation)
			XCopyRotationConstraint.Resolve(Influence,
				new XVector3Bool(true), new XVector3Bool(false), XCopyRotationConstraint.Mix.Replace,
				source, target, sourceSpace, targetSpace, sourceRest, targetRest);

		if (copyPosition)
			XCopyPositionConstraint.Resolve(Influence,
				source, sourceRest, sourceSpace,
				target, targetRest, targetSpace, new XVector3Bool(true), new XVector3Bool(false), XConstraintsUtil.PositionMixMethod.Replace);
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
