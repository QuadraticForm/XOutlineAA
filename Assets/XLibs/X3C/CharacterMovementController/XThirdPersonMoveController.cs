using UnityEngine;

namespace x
{
	public class XThirdPersonMoveController : MonoBehaviour
	{
		[XNullWarning]
		public XCharacterMover characterMover = null;
		[XNullWarning]
		public Transform cameraRotationRoot = null;
		

		private void Start()
		{
			if (characterMover == null) characterMover = GetComponent<XCharacterMover>();
		}

		private void Update()
		{
			var _input = XCommonInputStates.Instance;

			characterMover.jump = _input.jump.pressed;

			// if move not held, set the target speed to zero
			if (!_input.move.held)
			{
				characterMover.targetSpeed = 0.0f;
				return;
			}

			// Set speed based on sprint key and CharacterMover's MaxSpeed and NormalSpeed
			characterMover.targetSpeed = _input.sprint.held ? characterMover.maxSpeed : characterMover.normalSpeed;

			// Direction based on input
			var inputDirection = new Vector3(_input.move.Value.x, 0.0f, _input.move.Value.y).normalized;
			var inputRotationY = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;

			// Direction based on camera and input
			var targetRotation = cameraRotationRoot.transform.eulerAngles.y + inputRotationY;

			characterMover.targetMoveDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
		}
	}
}