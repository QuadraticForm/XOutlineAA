using UnityEngine;

//namespace xmath
//{
public static class XMath
{
	public static bool HasNaNOrInfinity(Vector3 vector)
	{
		return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z) ||
				float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z);
	}

	public static Vector3 FallbackIfNotValid(Vector3 vector, Vector3? defaultValue = null)
    {
        if (HasNaNOrInfinity(vector))
            return defaultValue ?? Vector3.zero;
		else
			return vector;
    }

	public static Vector3 Multiply(this Vector3 v, Vector3 other)
    {
        return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
    }

    public static Vector3 Divide(this Vector3 v, Vector3 other)
    {
        return new Vector3(v.x / other.x, v.y / other.y, v.z / other.z);
    }

	public static Matrix4x4 LocalToParentMatrix(this Transform transform)
    {
        // Get local position, rotation, and scale
        Vector3 localPosition = transform.localPosition;
        Quaternion localRotation = transform.localRotation;
        Vector3 localScale = transform.localScale;

        // Construct the local to parent matrix
        Matrix4x4 localToParentMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);

        return localToParentMatrix;
    }

	public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax, bool extrapolate = true)
	{
		if (!extrapolate)
		{
			if (value < fromMin) value = fromMin;
			if (value > fromMax) value = fromMax;
		}

		float fromRange = fromMax - fromMin;
		float toRange = toMax - toMin;
		float remappedValue = ((value - fromMin) / fromRange) * toRange + toMin;

		return remappedValue;
	}
}
//}