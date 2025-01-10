using UnityEngine;


public class XMoveObjectToRaycastHit : MonoBehaviour
{
	public Transform objectToMove = null;
	public Camera _rayOrigin = null;
	public Camera RayOrigin { get { return _rayOrigin? _rayOrigin : Camera.main; } }
    public LayerMask layerMask = 1; // Set this to the layer you want the raycast to interact with

	private void Start()
	{
	}

	void Update()
    {
        // Check if the left mouse button is pressed
        if (Input.GetMouseButton(0))
        {
            // Create a ray from the camera through the mouse position
            var ray = RayOrigin.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                // Move the object to the hit point
                objectToMove.position = hit.point;
            }
        }
    }
}