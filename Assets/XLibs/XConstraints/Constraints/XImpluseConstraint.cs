using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/2. Constraints (physical)/Impluse")]
public class XImpluseConstraint : XConstraintWithSingleSource
{
	[Header("Params")]
	public Space space = Space.World;

	public Vector3 velocity;

	public float maxImpluseTime = 0.1f;

	public bool invoke = false;


	public override void Resolve()
	{
		if (!invoke)
			return;

		if (sourceRest == null)
			return;

		var position = XConstraintsUtil.GetPositionInSpace(Source, space, sourceRest);

		position += velocity * Mathf.Min(Time.deltaTime, maxImpluseTime) * Influence;

		XConstraintsUtil.SetPositionInSpace(Source, position, space, sourceRest);

		invoke = false;
	}
}
