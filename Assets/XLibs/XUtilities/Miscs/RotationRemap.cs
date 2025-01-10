using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RotationRemap : MonoBehaviour
{
    /*
    public bool isInited = false;
    public Vector3 initialRotation = new Vector3(0, 0, 0);
    public Vector3 preRemapRotation = new Vector3(0, 0, 0);
    public Vector3 postRemapRotation = new Vector3(0, 0, 0);

    public Vector3 offset = new Vector3(0, 0, 0);
    public Vector3 multiplier = new Vector3(1, 1, 1);
    
    
    void RecordInitialRotation()
	{
        if (!isInited)
        {
            initialRotation = transform.localEulerAngles;
            isInited = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RecordInitialRotation();
    
	private void LateUpdate()
	{
        RecordInitialRotation();

        preRemapRotation = transform.localEulerAngles;

        var delta = transform.localEulerAngles - initialRotation;

        delta = Vector3.Scale(delta, multiplier);
        delta += offset;

        postRemapRotation = initialRotation + delta;

        transform.localEulerAngles = postRemapRotation;
    }
    */


    public bool isInited = false;
    public float multiplier = 1;

    Quaternion initRot = new Quaternion();

	private void Update()
	{
        if (!isInited)
		{
            initRot = transform.localRotation;
            isInited = true;
        }
    }

	private void LateUpdate()
	{
        var transformSync = GetComponent<TransformSync>();
        if (transformSync == null)
            return;

        transform.localRotation = Quaternion.LerpUnclamped(initRot, transformSync.target.transform.localRotation, multiplier);
	}
}
