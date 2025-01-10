using UnityEngine;

//[RequireComponent(typeof(XGizmoRenderer))]
public class XGizmo : MonoBehaviour
{
    public enum GizmoType
    {
        cube,
        sphere,
        wire_cube,
        wire_sphere,
        cross_3d // New 3D Cross type
    }

    public GizmoType type = GizmoType.cube; // Default type
    public Color color = new Color(1, 0, 0, 0.5f); // Default semi-transparent red
    public float size = 1.0f; // Default size
    public Vector3 scale = new Vector3(1, 1, 1); // Default scale
    public Vector3 offset = Vector3.zero; // Default offset
    public bool inFront = false; // Default occlusion setting

	[XQuickButton("OverrideBySource", "From Source", 0.2f, true, true)]
	public Transform transformOverride;

	public void OverrideBySource()
	{
		transformOverride = GetComponent<XConstraintWithSource>()?.Source;
	}

	public Transform ActualTransform
	{
		get { return transformOverride ? transformOverride : transform; }
	}

	[HideInInspector]
	public Matrix4x4 _gizmoMatrix = Matrix4x4.identity;

	public void Draw(bool isSelected)
	{
        // Adjust color brightness if selected
        var effectiveColor = isSelected ? color * 2.0f : color;
		effectiveColor.a = color.a;
        
        Gizmos.color = effectiveColor;
        var finalScale = scale * size;
        
        // Create a matrix for the local position including offset
        var localOffsetMatrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
        
        // Incorporate this matrix into the world transformation
        _gizmoMatrix = Matrix4x4.TRS(ActualTransform.position, ActualTransform.rotation, Vector3.one) * localOffsetMatrix;
        Gizmos.matrix = _gizmoMatrix;
        
        
        switch (type)
        {
            case GizmoType.cube:
                Gizmos.DrawCube(Vector3.zero, finalScale);
                break;
            case GizmoType.sphere:
                Gizmos.DrawSphere(Vector3.zero, size * Mathf.Max(scale.x, scale.y, scale.z));
                break;
            case GizmoType.wire_cube:
                Gizmos.DrawWireCube(Vector3.zero, finalScale);
                break;
            case GizmoType.wire_sphere:
                Gizmos.DrawWireSphere(Vector3.zero, size * Mathf.Max(scale.x, scale.y, scale.z));
                break;
            case GizmoType.cross_3d:
                Draw3DCross(Vector3.zero, finalScale * 0.5f); // Draw 3D Cross
                break;
        }
	}

    private void OnDrawGizmos()
    {
		Draw(false);
    }

	private void OnDrawGizmosSelected()
	{
		Draw(true);
	}

	private void Draw3DCross(Vector3 center, Vector3 halfScale)
    {
        // X axis line
        Gizmos.DrawLine(center - Vector3.right * halfScale.x, center + Vector3.right * halfScale.x);
        // Y axis line
        Gizmos.DrawLine(center - Vector3.up * halfScale.y, center + Vector3.up * halfScale.y);
        // Z axis line
        Gizmos.DrawLine(center - Vector3.forward * halfScale.z, center + Vector3.forward * halfScale.z);
    }

    public static XGizmo AddGizmo(GameObject obj,
                                  GizmoType type = GizmoType.cube,
                                  Color? color = null,
                                  Vector3? scale = null,
                                  float size = 1.0f,
                                  Vector3? offset = null,
                                  bool inFront = false)
    {
        XGizmo gizmo = obj.AddComponent<XGizmo>();
        gizmo.type = type;
        gizmo.color = color ?? new Color(1, 0, 0, 0.5f); // Default semi-transparent red
        gizmo.scale = scale ?? new Vector3(1, 1, 1); // Default scale
        gizmo.size = size; // Default size
        gizmo.offset = offset ?? Vector3.zero; // Default offset
        gizmo.inFront = inFront; // Occlusion option

        return gizmo;
    }

    public static XGizmo AddOrSetGizmo(GameObject obj,
                                       GizmoType type = GizmoType.cube,
                                       Color? color = null,
                                       Vector3? scale = null,
                                       float size = 1.0f,
                                       Vector3? offset = null,
                                       bool inFront = false)
    {
        XGizmo gizmo = obj.GetComponent<XGizmo>();

        if (gizmo == null)
        {
            gizmo = obj.AddComponent<XGizmo>();
        }
        
        gizmo.type = type;
        gizmo.color = color ?? new Color(1, 0, 0, 0.5f); // Default semi-transparent red
        gizmo.scale = scale ?? new Vector3(1, 1, 1); // Default scale
        gizmo.size = size; // Default size
        gizmo.offset = offset ?? Vector3.zero; // Default offset
        gizmo.inFront = inFront; // Occlusion option

        return gizmo;
    }
}