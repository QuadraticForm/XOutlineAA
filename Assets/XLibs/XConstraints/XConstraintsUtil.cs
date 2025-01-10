using UnityEngine;

using Space = XConstraintBase.Space;

public static class XTransformExtensions
{
	/// <summary>
	/// Get transform.localPosition, which is in its "Parent Space",
	/// this is a better name than "localPosition" to avoid confusion.
	/// </summary>
    public static Vector3 GetPositionInParentSpace(this Transform transform)
    {
        return transform.localPosition;
    }
	
	/// <summary>
	/// Set transform.localPosition, which is in its "Parent Space",
	/// this is a better name than "localPosotion" to avoid confusion.
	/// </summary>
    public static void SetPositionInParentSpace(this Transform transform, Vector3 position)
    {
        transform.localPosition = position;
    }

	/// <summary>
	/// Get position in specific space, 
	/// "Local" space is transform's initial space.
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="space"></param>
	/// <param name="restState">Only required when space == Local</param>
	/// <param name="worldToCustom">Only required when space == Custom</param>
	/// <returns></returns>
	public static Vector3 GetPositionInSpace(this Transform transform, XConstraintBase.Space space, XRestState restState = null, Matrix4x4? worldToCustom = null)
		=> XConstraintsUtil.GetPositionInSpace(transform, space, restState, worldToCustom);

	public static void SetPositionInSpace(this Transform transform, Vector3 pos, XConstraintBase.Space space, XRestState restState = null, Matrix4x4? customToWorld = null)
		=> XConstraintsUtil.SetPositionInSpace(transform, pos, space, restState, customToWorld);
}

public class XConstraintsUtil
{
	public enum Side
	{ left, right, none }

	public enum PositionMixMethod
	{
		Replace = 0,
		Add = 1,
	}

	static public Side TellSideByName(string name)
	{
		name = name.ToLower();

		if (name.EndsWith(".l") ||
			name.EndsWith("_l") ||
			name.EndsWith("left"))
		{
			return Side.left;
		}
		else if (name.EndsWith(".r") ||
				name.EndsWith("_r") ||
				name.EndsWith("right"))
		{
			return Side.right;
		}
        else
        {
			return Side.none;
        }
    }

	static public Color GetDefaultColorBySide(Side side)
    {
        switch (side)
        {
            case Side.left:
                return new Color(0.2f, 0.4f, 1f, 0.5f); // 半透明蓝色 (低饱和度)
            case Side.right:
                return new Color(1f, 0.4f, 0.4f, 0.5f); // 半透明红色 (低饱和度)
            case Side.none:
                return new Color(0.4f, 1f, 0.4f, 0.5f); // 半透明绿色 (低饱和度)
            default:
                return Color.white; // 默认返回白色
        }
    }

	static public Color GetDefaultColorBySide(string name)
	{
		return GetDefaultColorBySide(TellSideByName(name));
	}

	static public void Snap(Transform source, Transform target)
	{
		source.position = target.position;
		source.rotation = target.rotation;
		source.localScale = target.lossyScale;
	}

	static public Vector3 InvertChannels(Vector3 v, XVector3Bool invert)
	{
		return new Vector3(
			invert.x ? -v.x : v.x,
			invert.y ? -v.y : v.y,
			invert.z ? -v.z : v.z);
	}

	// for each channel if mask is true, return v, else return fallback
	static public Vector3 MaskChannels(Vector3 v, Vector3 fallback, XVector3Bool mask)
	{
		return new Vector3(
			mask.x ? v.x : fallback.x,
			mask.y ? v.y : fallback.y,
			mask.z ? v.z : fallback.z);
	}

	// TODO: is this correct?
	static public Quaternion InvertChannels(Quaternion v, XVector3Bool invert)
	{
		if (invert.IsAllFalse) return v;

		// Convert the quaternions to Euler angles
        var vEuler = v.eulerAngles;

        // Apply the mask to each Euler angle component
        var x = invert.x ? -vEuler.x : vEuler.x;
        var y = invert.y ? -vEuler.y : vEuler.y;
        var z = invert.z ? -vEuler.z : vEuler.z;

        // Create a new quaternion from the masked Euler angles
        return Quaternion.Euler(x, y, z);
	}

	// for each channel if mask is true, return v, else return fallback
	static public Quaternion MaskChannels(Quaternion v, Quaternion fallback, XVector3Bool mask)
	{
		if (mask.IsAllTrue) return v;
		if (mask.IsAllFalse) return fallback;

		// Convert the quaternions to Euler angles
        var vEuler = v.eulerAngles;
        var fallbackEuler = fallback.eulerAngles;

        // Apply the mask to each Euler angle component
        var x = mask.x ? vEuler.x : fallbackEuler.x;
        var y = mask.y ? vEuler.y : fallbackEuler.y;
        var z = mask.z ? vEuler.z : fallbackEuler.z;

        // Create a new quaternion from the masked Euler angles
        return Quaternion.Euler(x, y, z);
	}

	static public Vector3 MixChannels(Vector3 original, Vector3 _new, PositionMixMethod mix, XVector3Bool mask, float weight = 1.0f)
	{
		if (mix == PositionMixMethod.Add)
		{
			_new =  original + _new;
		}

		if (weight != 1.0f)
			_new = Vector3.Lerp(original, _new, weight);

		return MaskChannels(_new, original, mask);
	}

	/// <summary>
	/// Get position in specific space, 
	/// "Local" space is transform's initial space.
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="space"></param>
	/// <param name="restState">Only required when space == Local</param>
	/// <param name="worldToCustom">Only required when space == Custom</param>
	/// <returns></returns>
	static public Vector3 GetPositionInSpace(Transform transform, Space space, XRestState restState = null, Matrix4x4? worldToCustom = null)
	{
		if (space == Space.World)
		{
			return transform.position;
		}
		else if (space == Space.Custom)
		{
			if (worldToCustom == null)
				return Vector3.zero;

			return worldToCustom.Value.MultiplyPoint(transform.position);
		}
		else if (space == Space.LocalRest)
			// relative to initial(rest) local system, 
			// initial means as if object's TRS hasn't changed since start
		{
			if (restState == null)
				return Vector3.zero;

			return restState.parentToLocal.MultiplyPoint(transform.GetPositionInParentSpace());
		}
		else if (space == Space.Parent)
		{
			return transform.GetPositionInParentSpace();
		}
		else
			// Space.LocalCurrent
			return Vector3.zero; 
	}


	/// <summary>
	/// Set position in specific space, 
	/// "Local" space is transform's initial space.
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="space"></param>
	/// <param name="restState">Only required when space == Local</param>
	/// <param name="worldToCustom">Only required when space == Custom</param>
	/// <returns></returns>
	static public void SetPositionInSpace(Transform transform, Vector3 pos, Space space, XRestState restState = null, Matrix4x4? customToWorld = null)
	{
		if (space == Space.World)
		{
			transform.position = pos;
		}
		else if (space == Space.Custom)
		{
			if (customToWorld == null)
				return;

			transform.position = customToWorld.Value.MultiplyPoint3x4(pos);
		}
		else if (space == Space.LocalRest) 
			// pos is treated as in "local rest space"
			// relative to initial(rest) local system, 
			// this is a system as if object's TRS hasn't changed since start
		{
			if (restState == null)
				return;

			// transform position in "local space" to "parent space"
			transform.SetPositionInParentSpace(restState.localToParent.MultiplyPoint3x4(pos));
		}
		else if (space == Space.LocalCurrent)
		{
			transform.SetPositionInParentSpace(transform.LocalToParentMatrix().MultiplyPoint3x4(pos));
		}
		else if (space == Space.Parent)
		{
			transform.SetPositionInParentSpace(pos);
		}
	}
}

