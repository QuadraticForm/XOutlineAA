using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("XConstraint/2. Constraints (physical)/Ease Follow")]
public class XEaseFollowConstraint : XConstraintWithSingleSourceAndSingleTarget
{
	[Header("Params")]
	[Min(0)]
    public float easeSpeed = 5.0f; // »º¶¯ËÙ¶È

    override public void Resolve()
	{
            // Lerp Ëõ¶Ì¾àÀë
		Source.position = Vector3.Lerp(Source.position, target.position, easeSpeed * Time.deltaTime * Influence);
    }
}
