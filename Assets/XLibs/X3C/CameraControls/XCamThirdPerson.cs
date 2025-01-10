using UnityEngine;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace x
{
    public class XCamThirdPerson : MonoBehaviour
    {
		[XHeader("Camera", addLeftMargin:true)]
		[XNullWarning]
        public Transform CameraRotationRoot;

		[XHeader("Params")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        private float _cameraYaw;
        private float _cameraPitch;


        private const float _threshold = 0.01f;

        private void Start()
        {
            _cameraYaw = CameraRotationRoot.rotation.eulerAngles.y;
        }

        private void Update()
        {
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void CameraRotation()
        {
			var _input = XCommonInputStates.Instance;

			// if there is an input and camera position is not fixed
			if (_input.look.Value.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
				// TODO Don't multiply mouse input by Time.deltaTime;
				// TODO, this shouldn't be handled here, we should separate controller and input
				// float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				float deltaTimeMultiplier = 1;

                _cameraYaw += _input.look.Value.x * deltaTimeMultiplier;
                _cameraPitch += _input.look.Value.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cameraYaw = ClampAngle(_cameraYaw, float.MinValue, float.MaxValue);
            _cameraPitch = ClampAngle(_cameraPitch, BottomClamp, TopClamp);

            CameraRotationRoot.rotation = Quaternion.Euler(_cameraPitch + CameraAngleOverride,
                _cameraYaw, 0.0f);
        }


        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}