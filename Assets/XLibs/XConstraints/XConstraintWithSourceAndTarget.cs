using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintWithSingleSourceAndSingleTarget
	: XConstraintWithSingleSource
{
	[Header("Target")]
    public Transform target;

	protected XRestState targetRest;

	public override void RecordRest()
	{
		base.RecordRest();

		targetRest = target.RecordRestState(false); // resetToRestInUpdate is set to false to avoid affecting target

		restRecorded = true;
	}

	public override bool CanResolve => base.CanResolve && target != null;
}

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintWithSingleSourceAndMultipleTarget
	: XConstraintWithSingleSource
{
	[Header("Targets")]
	public List<XTransformInfluencePair> targets;

	protected List<XRestState> targetRests;

	public override void RecordRest()
	{
		base.RecordRest();

		targetRests = new List<XRestState>();

		foreach (var entry in targets)
			targetRests.Add(entry.transform.RecordRestState(false)); // resetToRestInUpdate is set to false to avoid affecting target


		restRecorded = true;
	}

	public override bool CanResolve => base.CanResolve && targets.Count > 0;
}

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintWithMultipleSourceAndSingleTarget
	: XConstraintWithMultipleSource
{
	[Header("Target")]
	public Transform target;

	protected XRestState targetRest;

	public override void RecordRest()
	{
		base.RecordRest();

		targetRest = target.RecordRestState(false); // resetToRestInUpdate is set to false to avoid affecting target

		restRecorded = true;
	}

	public override bool CanResolve => base.CanResolve && target != null;
}