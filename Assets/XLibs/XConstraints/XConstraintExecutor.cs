using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/0. Constraint Executor")]
public class XConstraintExecutor : XConstraintBase
{
	public enum Mode
	{
		DirectChildren,
		Custom
	}

	[Header("Executor")]
	public Mode mode = Mode.DirectChildren;

#if UNITY_EDITOR
	[XQuickButton("CollectFromDirectChildren")]
	public bool _buttonDummy1 = false;
#endif

	// [XConditionalHide("mode", Mode.Custom, XConditionalHideAttribute.CompareMethod.NotEqual)] // this just doesn't work on List! fuck! cuz the OnGUI in a PropertyDrawer correspond to a element in list, not the list as a whole
	public List<XConstraintBase> customList = new List<XConstraintBase>();

#if UNITY_EDITOR
	[XQuickButton("RefreshDirectChildrenCache")]
	public bool _buttonDummy2 = false;
#endif

	[NonSerialized]
	public List<XConstraintBase> _cachedDirectChildren = new List<XConstraintBase>();

	public void CollectFromDirectChildren()
	{
		customList.Clear();

		_CallOnDirectChildren((XConstraintBase constraint) =>
        {
			customList.Add(constraint);
        });
	}

	public void RefreshDirectChildrenCache()
	{
		_cachedDirectChildren.Clear();

		_CallOnDirectChildren((XConstraintBase constraint) =>
        {
			_cachedDirectChildren.Add(constraint);
        });
	}


	public bool _IsManaged(XConstraintBase _test)
	{
		foreach (var constraint in ManagedConstraints)
		{
			if (constraint == _test)
				return true;
		}

		return false;
	}

	public void Start()
	{
		RefreshDirectChildrenCache();
	}

	public override bool ShouldResetToRestInUpdate => true;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		SetManagedConstraintsExecutor();
	}

	protected override void OnEditorUpdate()
	{
		base.OnEditorUpdate();

		// this runs in OnUpdate() in play mode, so let's skip to save a little performance
		if (!Application.isPlaying)
			SetManagedConstraintsExecutor();
	}

	// must call this in Update
	protected void SetManagedConstraintsExecutor()
	{
		// set constraint's executor to prevent them from self executing

		foreach (var constraint in ManagedConstraints)
		{
			constraint._executor = this;
			constraint._hasExecutor = true;
		}
	}

	override public void Resolve()
	{
		foreach (var constraint in ManagedConstraints)
		{
			if (constraint.CanResolve) 
				constraint.Resolve();
		}
    }

	List<XConstraintBase> ManagedConstraints
	{
		get 
		{
			if (mode == Mode.DirectChildren)
				return _cachedDirectChildren;
			else
				return customList;
		}
	}

	// call on NOT cached direct children, slow!
	protected void _CallOnDirectChildren(Action<XConstraintBase> func)
	{
		foreach (Transform child in transform)
		{
			var constraint = child.GetComponent<XConstraintBase>();
			if (constraint != null)
			{
				func?.Invoke(constraint);
			}
		}
	}
}
