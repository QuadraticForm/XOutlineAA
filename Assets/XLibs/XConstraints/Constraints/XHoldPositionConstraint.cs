using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/3. Constraints (Reactive)/Hold Position")]
public class XHoldPositionConstraint : XSetPositionConstraint
{
	[Header("Hold")]
	[XReadOnly]
	public bool hold = false;
	[XReadOnly]
	public bool lastFrameHold = false;

	public void Hold()
	{
		hold = true;

		// if switching hold state in this frame,
		// use position in current frame from now on
		// if (hold && !lastFrameHold)
		if (!lastFrameHold)
		{
			UseCurrentPosition();
		}
	}

	public override void Resolve()
	{
		if (hold)
			base.Resolve();

		lastFrameHold = hold;
		hold = false; // reset hold state, hold state should be set by calling Hold() before execution of this constraint to keep the holding state
	}
}
