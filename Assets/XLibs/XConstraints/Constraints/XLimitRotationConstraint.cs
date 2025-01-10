using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/1. Constraints/Limit Rotation")]
public class XLimitRotationConstraint : XConstraintWithSingleSource
{
	[Header("Limits")]
    public Vector3 min = new Vector3(0,0,0);
    public Vector3 max = new Vector3(0,0,0);

    public bool limitX = false;
    public bool limitY = false;
    public bool limitZ = false;

    override public void Resolve()
	{
		// TODO implement influence

        var currentEuler = Source.transform.localRotation.eulerAngles;

        if (limitX)
        {
            currentEuler.x = Mathf.DeltaAngle(0, currentEuler.x);
            currentEuler.x = Mathf.Min(currentEuler.x, max.x);
            currentEuler.x = Mathf.Max(currentEuler.x, min.x);
        }

        if (limitY) 
        {
            currentEuler.y = Mathf.DeltaAngle(0, currentEuler.y);
            currentEuler.y = Mathf.Min(currentEuler.y, max.y);
            currentEuler.y = Mathf.Max(currentEuler.y, min.y);
        }

        if (limitZ)
        {
            currentEuler.z = Mathf.DeltaAngle(0, currentEuler.z);
            currentEuler.z = Mathf.Min(currentEuler.z, max.z);
            currentEuler.z = Mathf.Max(currentEuler.z, min.z);
        }

        Source.transform.localEulerAngles = currentEuler;
    }
}
