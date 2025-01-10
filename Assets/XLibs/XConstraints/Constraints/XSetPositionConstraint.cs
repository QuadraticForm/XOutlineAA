using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Set Position")]
public class XSetPositionConstraint : XConstraintWithSingleSource
{
	[Header("Params")]
	public Space space = Space.World;

	public Vector3 position;

#if UNITY_EDITOR
	[XQuickButton("UseCurrentPosition", "¡ü", 0.1f, true, false)]
	public Vector3 _currentPosition;
#endif

	public void UseCurrentPosition()
	{
		position = XConstraintsUtil.GetPositionInSpace(transform, space, sourceRest);
	}

	static void Resolve(float influence, Transform source, XRestState sourceRest, Vector3 position, Space space)
	{
		var currentPosition = XConstraintsUtil.GetPositionInSpace(source, space, sourceRest);

		var finalPosition = Vector3.Lerp(currentPosition, position, influence);

		XConstraintsUtil.SetPositionInSpace(source, finalPosition, space, sourceRest);
	}

	public override void Resolve()
	{
		Resolve(Influence, Source, sourceRest, position, space);
	}

#if UNITY_EDITOR
	protected override void OnEditorUpdate()
	{
		base.OnEditorUpdate();

		_currentPosition = XConstraintsUtil.GetPositionInSpace(transform, space, sourceRest);
	}
#endif
}
