using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace XUtil
{
    // TODO, see this:
    // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Quaternion_to_Euler_angles_conversion

    public class EulerRotation
    {
        public enum RotSeq
        {
            // we use extrinsic euler rotation to be consistant with other softwares
            xyz, xzy, yxz, yzx, zxy, zyx
        };

        public static Quaternion FromXYZ(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(0, 0, euler.z) * Quaternion.Euler(0, euler.y, 0) * Quaternion.Euler(euler.x, 0, 0);
        }

        public static Quaternion FromXZY(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(0, euler.y, 0) * Quaternion.Euler(0, 0, euler.z) * Quaternion.Euler(euler.x, 0, 0);
        }

        public static Quaternion FromYXZ(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(0, 0, euler.z) * Quaternion.Euler(euler.x, 0, 0) * Quaternion.Euler(0, euler.y, 0);
        }

        public static Quaternion FromYZX(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(euler.x, 0, 0) * Quaternion.Euler(0, 0, euler.z) * Quaternion.Euler(0, euler.y, 0);
        }

        public static Quaternion FromZXY(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(0, euler.y, 0) * Quaternion.Euler(euler.x, 0, 0) * Quaternion.Euler(0, 0, euler.z);
        }

        public static Quaternion FromZYX(Vector3 euler, bool degree = true)
        {
            if (!degree)
                euler = euler / Mathf.PI * 180.0f; // radian to degree
            return Quaternion.Euler(euler.x, 0, 0) * Quaternion.Euler(0, euler.y, 0) * Quaternion.Euler(0, 0, euler.z);
        }

        public static Vector3 ToXYZ(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = 2 * (q.x * q.y + q.w * q.z);
            var r12 = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;
            var r21 = -2 * (q.x * q.z - q.w * q.y);
            var r31 = 2 * (q.y * q.z + q.w * q.x);
            var r32 = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;

            ret.x = Mathf.Atan2(r31, r32);
            ret.y = Mathf.Asin(r21);
            ret.z = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 ToXZY(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = -2 * (q.x * q.z - q.w * q.y);
            var r12 = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;
            var r21 = 2 * (q.x * q.y + q.w * q.z);
            var r31 = -2 * (q.y * q.z - q.w * q.x);
            var r32 = q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z;

            ret.x = Mathf.Atan2(r31, r32);
            ret.z = Mathf.Asin(r21);
            ret.y = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 ToYXZ(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = -2 * (q.x * q.y - q.w * q.z);
            var r12 = q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z;
            var r21 = 2 * (q.y * q.z + q.w * q.x);
            var r31 = -2 * (q.x * q.z - q.w * q.y);
            var r32 = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;

            ret.y = Mathf.Atan2(r31, r32);
            ret.x = Mathf.Asin(r21);
            ret.z = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 ToYZX(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = 2 * (q.y * q.z + q.w * q.x);
            var r12 = q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z;
            var r21 = -2 * (q.x * q.y - q.w * q.z);
            var r31 = 2 * (q.x * q.z + q.w * q.y);
            var r32 = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;

            ret.y = Mathf.Atan2(r31, r32);
            ret.z = Mathf.Asin(r21);
            ret.x = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 ToZXY(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = 2 * (q.x * q.z + q.w * q.y);
            var r12 = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;
            var r21 = -2 * (q.y * q.z - q.w * q.x);
            var r31 = 2 * (q.x * q.y + q.w * q.z);
            var r32 = q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z;

            ret.z = Mathf.Atan2(r31, r32);
            ret.x = Mathf.Asin(r21);
            ret.y = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 ToZYX(Quaternion q, bool degree = true)
        {
            Vector3 ret = new Vector3();

            var r11 = -2 * (q.y * q.z - q.w * q.x);
            var r12 = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;
            var r21 = 2 * (q.x * q.z + q.w * q.y);
            var r31 = -2 * (q.x * q.y - q.w * q.z);
            var r32 = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;

            ret.z = Mathf.Atan2(r31, r32);
            ret.y = Mathf.Asin(r21);
            ret.x = Mathf.Atan2(r11, r12);

            if (degree)
                ret = ret / Mathf.PI * 180.0f;

            return ret;
        }

        public static Vector3 quatToEuler(Quaternion q, RotSeq rotSeq)
        {
            switch (rotSeq)
            {
                case RotSeq.xyz:
                    return ToXYZ(q);

                case RotSeq.yxz:
                    return ToYXZ(q);

                case RotSeq.zxy:
                    return ToZXY(q);

                case RotSeq.xzy:
                    return ToXZY(q);

                case RotSeq.zyx:
                    return ToZYX(q);

                case RotSeq.yzx:
                    return ToYZX(q);

                default:
                    Debug.LogError("No good sequence");
                    return Vector3.zero;

            }

        }
    }
}
