using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// animation and constraint execution order
/// (start) record rest;
/// (game loop) 
/// 1. ResetToRest, in Update
/// 2. Animation, unity internal by animator
/// 3. AnimationRig, or anything using playable API
/// 4. XConstraints, in LateUpdate
/// 
/// inside XConstraints, order is controlled by XConstraintContainer and XConstraintOrderController
/// 
/// XConstraintContainer execute all first level children constraints in child order (the same in scene hierarchy).
/// XConstraintOrderController execute all constraints in its list, order is controlled manually 
/// 
/// TODO forbid 2 XConstraintBase Components on the same Object!
/// 
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("")] // user should not create instances of this base class
public class XConstraintBase : MonoBehaviour
{
	public enum Axis
	{
		X, Y, Z
	}

	public enum Direction
	{
		X, Y, Z,
		NegativeX,
		NegativeY,
		NegativeZ
	}

	public enum Space
	{
		World = 0,

		[InspectorName(null)] // hide from user as it's not fully implemented for user to choose ref coordsys on UI
		Custom = 1, // not fully implemented
		// Pose = 2, // TODO, not implemented, and can be replaced by custom in unity
		// LocalWithParent = 3, // TODO, not implemented

		/// <summary>
		/// relative to initial(rest) local system, 
		/// a coord system as if object's TRS hasn't changed since start.
		/// same as Blender's bone's "local" space.
		/// </summary>
		LocalRest = 4,

		// LocalWithSourceOrientation = Space.LocalRest + 1, // this is for target only

		/// <summary>
		/// relative to current local system
		/// </summary>
		LocalCurrent = 6, // this does not exist in Blender, added cuz artists find it useful

		Parent = 10,
	}

	public enum TargetSpace
	{
		World = Space.World, // 0
		[InspectorName(null)]
		Custom = Space.Custom, // TODO, not fully implemented
		// Pose = Space.Pose, // TODO, not implemented
		// LocalWithParent = Space.LocalWithParent, // TODO, not implemented
		/// <summary>
		/// relative to initial(rest) local system, 
		/// this is a system as if object's TRS hasn't changed since start.
		/// same as in Blender
		/// </summary>
		LocalRest = Space.LocalRest, // 4
		/// <summary>
		/// relative to initlal(rest) local system, while ignoring parents' transform,
		/// followed by a correction for the difference in target and source orientation.
		/// when applied to the source as local, source travels with the target at the same direction.
		/// (if the parents are all in rest pose)
		/// </summary>
		LocalRestWithSourceOrientation = Space.LocalRest + 1, // 5
		
		// LocalCurrent = Space.LocalCurrent, // this is meaningless, cuz it's identity, and depending on a constant value is meaningless

		Parent = Space.Parent, // 10
	}

	[Header("Basics")]

#if UNITY_EDITOR
	// [XQuickButton("Apply", "Apply", 0.2f, true, true )]
	[XQuickButton("Apply", "Apply", 0.2f)]
	public bool _applyButtonDummy = false;

	public bool executeInEditMode = false;

#endif

	#region Executor

	/// <summary>
	/// 
	/// internal, if set, this constraint won't self execute.
	/// 
	/// </summary>
	/// 
	/// <remarks>
	///
	/// _executor is set by XConstraintExecutor in its Update,
	/// and reset by XConstraintBase in its LateUpdate,
	/// 
	/// so when a constraint is given to (or removed from) an executor,
	/// (using executor's customList or by being its direct children)
	/// the constraint's reference to executor is automatically updated
	/// 
	/// </remarks>
	[XReadOnly]
	public XConstraintExecutor _executor;

	[NonSerialized]
	public bool _hasExecutor = false; // test this instead of (_executor == null) is faster

	#endregion

	#region Influence
	private float ExecutorInfluence => _hasExecutor ? _executor.Influence : 1.0f;

	private float _cachedExecutorInfluence = 1.0f;      // cached for better performance (to avoid Recursion when calling Influence)
	private bool _isExecutorInfluenceCached = false;    // reset to false every Update()

	[Range(0, 1)]
	[FormerlySerializedAs("influence")]
	public float _influence = 1.0f;

	public float Influence {
		get { 
			if (!_isExecutorInfluenceCached)
			{
				_cachedExecutorInfluence = ExecutorInfluence;
				_isExecutorInfluenceCached = true;
			}

			return _influence * _cachedExecutorInfluence; }}

	#endregion

#if UNITY_EDITOR
	public virtual bool ShouldExecute => executeInEditMode || Application.isPlaying;
#else
	public virtual bool ShouldExecute =>  Application.isPlaying;
#endif
	

	public virtual bool ShouldSelfResolve => !_hasExecutor && ShouldExecute;

	public virtual bool ShouldResetToRestInUpdate => false;

	public virtual bool CanResolve => isActiveAndEnabled && Influence > 0;

	public virtual void Resolve()
	{

    }

	// Resolve and Record Rest
	public virtual void Apply()
	{
		RecordRest(); // force record now to prepare for Resolve
		Resolve();
		RecordRest();
	}

	/// <summary>
	/// only run when ShouldExecute is true, that is: executeInEditMode || Application.isPlaying;
	/// </summary>
	protected virtual void OnUpdate() {	}

	protected virtual void OnLateUpdate() { }

	/// <summary>
	/// always run, no matter in play mode or editor mode
	/// </summary>
	protected virtual void OnEditorUpdate() { }

	private void Update()
	{
		_isExecutorInfluenceCached = false;

		if (ShouldExecute)
			OnUpdate();

#if UNITY_EDITOR

		OnEditorUpdate();

#endif
	}

	void LateUpdate()
	{
		OnLateUpdate();

		if (ShouldSelfResolve && CanResolve)
			Resolve();

		// reset reference to executor, see remarks of _executor

		_hasExecutor = false;

#if UNITY_EDITOR

		bool highPerformanceEditorPlay = true;
	
		if (Application.isPlaying && highPerformanceEditorPlay)
			// when playing in editor, we go the fast way too
			_executor = null; // "_executor" will be set by the XConstraintExecutor in Update
		else if (_executor != null)
			// when in editor, 
			// we want _executor to be shown on the inspector with proper reference,
			// so we can't simply reset it,
			// instead, we use a more costly method,
			// only reset when executor has turely removed this constraint_executor = null;
			if (!_executor._IsManaged(this))
				_executor = null;
#else

		// in runtime,
		// the inspector doesn't matter,
		// we go the fast way

		_executor = null; // "_executor" will be set by the XConstraintExecutor in Update
#endif
	}

	/// <summary>
	/// record needed rest states at start, rest means the initial state without any animation
	/// </summary>
	public virtual void RecordRest()
	{
	}

	public virtual void ResetToRest()
	{
	}
}

