using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DampedLookAt : MonoBehaviour
{
    public Transform target;
    public Vector3 dampedTargetPos = new Vector3(0,0,0);
    [Range(0,1)]
    public float damping = 0.1f; // TODO damping independent of frame rate
    // public float 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            return;

        // calc damping

        // calc diff

        var targetPos = target.position;
        var diff = targetPos - dampedTargetPos;
        diff = diff * (1 - damping);

        dampedTargetPos = dampedTargetPos + diff;

        // look at

        transform.LookAt(dampedTargetPos);
    }
}
