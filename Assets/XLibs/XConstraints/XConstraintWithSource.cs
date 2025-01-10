using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;



[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public partial class XConstraintWithSource : XConstraintBase
{

	protected bool restRecorded = false;

	// should not be valid and should not Apply if rest is not recorded
	// cuz RecordRest must happen before modification of object's state
	public override bool CanResolve => base.CanResolve && restRecorded;

	/// <summary>
	/// Reset Source's transform to its "Rest" state in "Update".
	/// </summary>
	/// <remarks>
	/// Constraints rely on the transform of the Source object but they also affect the transform of the Source object,
	/// which can lead to a "feedback loop" problem.
	///
	/// <para>
	/// For example, an Aim constraint with 50% influence will eventually degrade to a near 100% Aim after a few frames
	/// if allowed to "feedback loop".
	/// </para>
	///
	/// <para>
	/// Furthermore, "feedback loop" can often lead to inconsistencies in Aim or IK (inconsistency here means after a few iterations,
	/// even if the target returns to the same place as before, Source's transform won't be the same).
	/// </para>
	///
	/// <para>
	/// To avoid these problems, we need a "clean" input for constraints. ("Clean" here means unaffected by constraints.)
	/// A possible "clean" input is the animation data (it’s provided by the animation system every frame,
	/// so it is clean). But when there is no animation data for the source, we can only rely on its "Rest" state.
	/// </para>
	/// </remarks>
	[Header("Rest State")]
	[Tooltip("In Update, reset Source's transform to its 'Rest State' to avoid 'feedback loop' issues.")]
	// [XQuickButton(new[] { "RecordRest", "ResetToRest" }, new[] { "💾 Record", "🔄 Reset To" }, 0.5f, true, true )]
	[XQuickButton("ResetToRest", "🔄 Reset", 0.2f, true, true )]
	[FormerlySerializedAs("resetToRestBeforeAnim")]
	public bool resetSourcesToRestBeforeAnim = true;

	public override bool ShouldResetToRestInUpdate => resetSourcesToRestBeforeAnim;

	public virtual Transform Source { get; }
	public virtual List<XTransformInfluencePair> Sources { get; }

	void Start()
	{
		if (!ShouldExecute)
			return;

		// RecordRest as early as possible
		// before object's state got modified
		if (!restRecorded)
			RecordRest();
	}

	private void Awake()
	{
		if (!ShouldExecute)
			return;

		// RecordRest as early as possible
		// before object's state got modified
		if (!restRecorded)
			RecordRest();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// in some weird case Start is called after Update
		// so RecordRest as early as possible
		// before object's state got modified
		if (!restRecorded)
			RecordRest();
	}

	public override void RecordRest()
	{
		foreach (var entry in Sources)
			entry.transform.RecordRestState(resetSourcesToRestBeforeAnim);

		restRecorded = true;
	}

	public override void ResetToRest()
	{
		if (!restRecorded) return;

		foreach (var entry in Sources)
			entry.transform.GetRestState()?.ResetToRest();
	}
}

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintWithSingleSource : XConstraintWithSource
{
	public enum FallbackSource
	{
		parent,
		self
	}

	[Header("Source")]
	[FormerlySerializedAs("_source")]
	[XQuickButton(new[] { "SelfAsSource", "ParentAsSource", "UsePrevSource" }, new[] { "Self", "Parent", "Prev" }, 0.25f, true, true)]
	public Transform source = null;

	protected XRestState sourceRest;

	public override Transform Source => source;

	// better avoid calling this for performance reasons
	public override List<XTransformInfluencePair> Sources
	{
		get 
		{
			var ret = new List<XTransformInfluencePair>();
			if (Source != null)
				ret.Add(new XTransformInfluencePair { transform = Source, influence = 1.0f });
			return ret;
		}
	}

	// override for better performance, avoid calling "Sources" like in base class
	public override void RecordRest()
	{
		sourceRest = source.RecordRestState(resetSourcesToRestBeforeAnim);

		restRecorded = true;
	}

#if UNITY_EDITOR
	// hide when _source is specified
	// [XQuickButton(new[] { "SelfAsSource", "ParentAsSource" }, new[] { "Self", "Parent" })]
	// public bool _sourceButtonDummy = false;

	public void SelfAsSource()
	{
		source = transform;
	}

	public void ParentAsSource()
	{
		source = transform.parent;
	}

	public void UsePrevSource()
	{
		var siblingIndex = transform.GetSiblingIndex();
		if (siblingIndex <= 0) return;

		var prev = transform.parent.GetChild(siblingIndex - 1);
		if (prev == null) return;

		var prevConstraint = prev.GetComponent<XConstraintWithSource>();
		if (prevConstraint == null) return;

		source = prevConstraint.Source;
	}

#endif
}

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintWithMultipleSource : XConstraintWithSource
{
	[Header("Sources")]
	public List<XTransformInfluencePair> sources;

	protected List<XRestState> sourceRests;

	public override Transform Source
	{
		get
		{
			if (sources == null || sources.Count == 0)
				return null;

			return sources[0].transform;
		}
	}

	public override List<XTransformInfluencePair> Sources => sources;

	public override void RecordRest()
	{
		sourceRests = new List<XRestState>();

		foreach (var source in sources)
			sourceRests.Add(source.transform.RecordRestState(resetSourcesToRestBeforeAnim));

		restRecorded = true;
	}
}