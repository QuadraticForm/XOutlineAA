using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Damped Track (like in blender)")]
public class XDampedTrackConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]
    public Direction trackDirection = Direction.Y;

	static public void ApplyTo(float influence, Transform source, Vector3 targetPosition, Direction direction)
	{
		if (source == null)
            return;
     
        var targetDirection = targetPosition - source.position;
     
        var targetRotation = Quaternion.identity;
     
        switch (direction)
        {
            case Direction.X:
                targetRotation = Quaternion.FromToRotation(source.right, targetDirection);
                break;
            case Direction.Y:
                targetRotation = Quaternion.FromToRotation(source.up, targetDirection);
                break;
            case Direction.Z:
                targetRotation = Quaternion.FromToRotation(source.forward, targetDirection);
                break;
            case Direction.NegativeX:
                targetRotation = Quaternion.FromToRotation(-source.right, targetDirection);
                break;
            case Direction.NegativeY:
                targetRotation = Quaternion.FromToRotation(-source.up, targetDirection);
                break;
            case Direction.NegativeZ:
                targetRotation = Quaternion.FromToRotation(-source.forward, targetDirection);
                break;
        }
        
        source.rotation = Quaternion.Slerp(source.rotation, targetRotation * source.rotation, influence);
	}

	public override void Resolve()
	{
		ApplyTo(Influence, Source, target.position, trackDirection);
    }
}