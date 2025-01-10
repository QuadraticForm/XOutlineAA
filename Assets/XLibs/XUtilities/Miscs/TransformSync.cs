using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class TransformSync : MonoBehaviour
{
    public Transform target = null;

    public Vector3 rotationOffset = new Vector3(0, 0, 0);
    public Vector3 rotationMultiplier = new Vector3(1, 1, 1);

    public bool useRotationMin = false;
    public Vector3 rotationMin = new Vector3(0, 0, 0);

    public bool useRotationMax = false;
    public Vector3 rotationMax = new Vector3(0, 0, 0);

    public void Sync()
	{
        if (target == null)
            return;

        // solve dependency
        var targetTransformSync = target.GetComponent<TransformSync>();
        if (targetTransformSync)
            targetTransformSync.Sync();


        transform.localPosition = target.localPosition;
        transform.localEulerAngles = target.localEulerAngles;
        transform.localScale = target.localScale;

        transform.localEulerAngles += rotationOffset;
        transform.localEulerAngles = Vector3.Scale(transform.localEulerAngles, rotationMultiplier);

        if (useRotationMin)
            transform.localEulerAngles = Vector3.Max(transform.localEulerAngles, rotationMin);

        if (useRotationMax)
            transform.localEulerAngles = Vector3.Min(transform.localEulerAngles, rotationMax);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

	private void LateUpdate()
	{
        Sync();
    }
}
