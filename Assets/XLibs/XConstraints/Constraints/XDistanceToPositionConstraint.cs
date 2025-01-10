using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/4. Drivers/Distance To Position")]
public class XDistanceToPositionConstraint :
	XConstraintWithSingleSource
{
	[Header("Targets")]

	public Transform targetA = null;
	public Transform targetB = null;

	public enum DistanceSpace
	{
		World = Space.World,
		Source,
	};

	[Header("Params")]
	public bool extrapolate = false;

	[Space(10)]
	public DistanceSpace distanceSpace = DistanceSpace.Source;
	public bool useDeltaDistance = true;

	[Space(10)]
	public Space sourceSpace = Space.LocalRest;
	public XConstraintsUtil.PositionMixMethod mix = XConstraintsUtil.PositionMixMethod.Add;

	[Header("Axis")]
	public XVector3Bool axis = new XVector3Bool(true, true, true);

	[Header("Remap From")]
	public Vector3 fromMin = new Vector3(0, 0, 0);
	public Vector3 fromMax = new Vector3(1, 1, 1);

	[Header("Remap To")]
	public Vector3 toMin = new Vector3(0, 0, 0);
	public Vector3 toMax = new Vector3(1, 1, 1);

	[Header("Debug")]
	[XReadOnly]
	public float restDistance = 0;
	[XReadOnly]
	public float currentDistance = 0;

	private XRestState _sourceRestState;


	public float CalcCurrentDistance()
	{
		if (source == null || targetA == null || targetB == null || sourceRest == null)
			return 0;

		Vector3 posA = targetA.position;
		Vector3 posB = targetB.position;

		if (distanceSpace == DistanceSpace.Source)
		{
			posA = source.worldToLocalMatrix.MultiplyPoint3x4(posA);
			posB = source.worldToLocalMatrix.MultiplyPoint3x4(posB);
		}

		return (posA - posB).magnitude;
	}

	public override void RecordRest()
	{
		base.RecordRest();

		if (source == null || targetA == null || targetB == null)
		{
			restRecorded = false;
			return;
		}

		restDistance = CalcCurrentDistance();

		restRecorded = true;
	}

	public override void Resolve()
	{
		// calc distance

		currentDistance = CalcCurrentDistance();

		// value remap

		var fromValue = useDeltaDistance ? currentDistance - restDistance : currentDistance;

		var toValue = new Vector3();

		if (axis.x) toValue.x = XMath.Remap(fromValue, fromMin.x, fromMax.x, toMin.x, toMax.x, extrapolate);
		if (axis.y) toValue.y = XMath.Remap(fromValue, fromMin.y, fromMax.y, toMin.y, toMax.y, extrapolate);
		if (axis.z) toValue.z = XMath.Remap(fromValue, fromMin.z, fromMax.z, toMin.z, toMax.z, extrapolate);

		// value mix

		var sourcePos = Source.GetPositionInSpace(sourceSpace, sourceRest);

		sourcePos = XConstraintsUtil.MixChannels(sourcePos, toValue, mix, axis, Influence);

		// set

		Source.SetPositionInSpace(sourcePos, sourceSpace, sourceRest);
	}
}
