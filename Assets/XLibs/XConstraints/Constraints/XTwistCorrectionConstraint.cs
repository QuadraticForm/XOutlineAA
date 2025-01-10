using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Twist Correction Constraint")]
public class XTwistCorrectionConstraint : XConstraintWithSingleSourceAndSingleTarget
{
    [Header("Params")]

    public Axis axis = Axis.Y;

	// not using Space directly to limit options, discarding meaningless ones for Twist Correction 
	public enum TwistCorrectionSpace
	{
		World = Space.World,

		Parent = Space.Parent,

		LocalRest = Space.LocalRest,

		// LocalCurrent = Space.LocalCurrent, // LocalCurrent is meaningless for Twist Correction

		// TODO, add other meaningful spaces like "custom"
	}

	public TwistCorrectionSpace space = TwistCorrectionSpace.LocalRest;

    static Quaternion MaskTwist(Quaternion q, Axis axis)
    {
        if (axis == Axis.X)
            return new Quaternion(q.x, 0, 0, q.w);
        else if (axis == Axis.Y)
            return new Quaternion(0, q.y, 0, q.w);
        else if (axis == Axis.Z)
            return new Quaternion(0, 0, q.z, q.w);
        else
            return Quaternion.identity;
    }

    static public void ApplyLocalSpace(
      float influence, Transform source, Transform target, Axis axis, Quaternion sourceRest, Quaternion targetRest)
    {
		// terminology: in XConstraint, similar to blender, source means constrained object while target is depended on by source, "rest" means init state
		//
		// math:
		// there are 2 ways to understand B * A.
		// 1. extrinsic: A is done first, then B, using the global rotation tool.
		// 2. intrinsic: B is done first, then A, using the local rotation tool.
		//
		// so, when it comes to "twist", we think of it using the 2 method:
		//
		// current = init * twist, 
		// that is: twist is done using the local rotation tool.
		//
		// now, we want to apply "target"'s twist on "source", like rotating it using local rotation tool
		// 
        // 术语：在 XConstraint 库中，类似 Blender，source 指被约束的对象，target 指被依赖的对象，rest 指初始状态
        //
        // 数学解释：
        // 有两种方式来理解四元数的乘 B * A。
		// 
		// 例如 A 是绕着 X 轴转 N°，B 是绕着 Y 轴转 M°
        // 1. 外旋：就像使用 global 旋转工具，先执行 A，再执行 B。
        // 2. 内旋：就像使用 local 旋转工具，先执行 B，再执行 A。
        //
        // 在理解 twist 时，我们使用第二种方法：
        //
        // current = rest * twist
        // 也就是说：twist 就像是使用 local 旋转工具，绕着给定轴，旋转一个节点。
        //
        // 现在，我们希望把对 target 做的 twist，在 source 上重复一遍。
        //
        // 下面是详细的数学推导：
        //
        // targetCurrent = targetRest * targetTwist
        // inverse(targetRest) * targetCurrent = inverse(targetRest) * targetRest * targetTwist
        // inverse(targetRest) * targetCurrent = targetTwist
        //
        // sourceCurrent = sourceRest * targetTwist
        
        var targetTwist = Quaternion.Inverse(targetRest) * target.localRotation;
        targetTwist = MaskTwist(targetTwist, axis);
        source.localRotation = Quaternion.Lerp(sourceRest, sourceRest * targetTwist, influence);
    }

	static public void ApplyParentSpace(
      float influence, Transform source, Transform target, Axis axis)
    {
        // math:
		// 1. match source and target's parent space rotation using a delta rotation in source's local space
		// targetCurrent = sourceCurrent * deltaSourceLocal;
		//
		// 2. calculate the delta rotation
		// Inverse(sourceCurrent) * targetCurrent = Inverse(sourceCurrent) * sourceCurrent * deltaSourceLocal;
		// Inverse(sourceCurrent) * targetCurrent = deltaSourceLocal;
		// 
		// 3. mask out rotation on axis that are not the twist axis
		// twist = MaskTwist(deltaSourceLocal, axis)
		//
		// 4. apply twist to source
		// sourceCurrent = sourceCurrent * twist

		// NOTE: objects rotation in parent space is called "localRotation", 
		// don't get confused with rotation in local space, which is rotation relative to its inital local space

		// Calculate rot: rotation needed to match target.
		var deltaSourceLocal = Quaternion.Inverse(source.localRotation) * target.localRotation;

		// Mask out other axes and isolate desired twist.
		var twist = MaskTwist(deltaSourceLocal, axis);

		// Apply this twist to source.
		var newSourceRotation = source.localRotation * twist;

		// Lerp based on influence
		source.localRotation = Quaternion.Lerp(source.localRotation, newSourceRotation, influence);
    }

    static public void ApplyWorldSpace(float influence, Transform source, Transform target, Axis axis)
    {
		// math:
		// 1. match source and target's world space rotation using a delta rotation in source's local space
		// targetCurrent = sourceCurrent * deltaSourceLocal;
		//
		// 2. calculate the delta rotation
		// Inverse(sourceCurrent) * targetCurrent = Inverse(sourceCurrent) * sourceCurrent * deltaSourceLocal;
		// Inverse(sourceCurrent) * targetCurrent = deltaSourceLocal;
		// 
		// 3. mask out rotation on axis that are not the twist axis
		// twist = MaskTwist(deltaSourceLocal, axis)
		//
		// 4. apply twist to source
		// sourceCurrent = sourceCurrent * twist

		// Calculate rot: rotation needed to match target.
		var deltaSourceLocal = Quaternion.Inverse(source.rotation) * target.rotation;

		// Mask out other axes and isolate desired twist.
		var twist = MaskTwist(deltaSourceLocal, axis);

		// Apply this twist to source.
		var newSourceRotation = source.rotation * twist;

		// Lerp based on influence
		source.rotation = Quaternion.Lerp(source.rotation, newSourceRotation, influence);
    }
    
    public override void Resolve()
	{
	    if (space == TwistCorrectionSpace.LocalRest)
		{
			ApplyLocalSpace(Influence, Source, target, axis, sourceRest.localRotation, targetRest.localRotation);
		}
		else if (space == TwistCorrectionSpace.Parent)
		{
			ApplyParentSpace(Influence, Source, target, axis);
		}
		else if (space == TwistCorrectionSpace.World)
		{
            ApplyWorldSpace(Influence, Source, target, axis);
        }
	}
}