using UnityEngine;
using UnityEngine.InputSystem;

namespace x
{
	[RequireComponent(typeof(XCamFirstPerson))]
	public class XCamFreeFly : MonoBehaviour
	{
		#region Camera

		[XHeader("Camera")]
		[XComment("If not set, will use self")]
		public Transform cameraTransform = null;

		public Transform CameraTransform
		{
			get
			{
				if (cameraTransform == null) return transform;
				return cameraTransform;
			}
		}

		#endregion

		#region Move Params

		public enum SprintSwitchMethod
		{
			Hold,
			Toggle
		}

		[XHeader("Move Params")]
		public SprintSwitchMethod sprintSwitchMethod = SprintSwitchMethod.Hold;
		public float normalSpeed = 1.0f;
		public float sprintSpeed = 2.5f;
		public float moveEasingTime = 0.1f;

		#endregion

		#region Input Actions

		[XHeader("Input Actions")]

        public InputActionAsset inputActions;
        public string moveActionName = "Move";
		public string sprintActionName = "Sprint";

		private InputAction moveAction;
		private InputAction sprintAction;

		#endregion

		#region Debug Fields

		[XHeader("Debug")]
		public XEmptyField _headerDummy;

		[XReadOnly]
		public bool isSprinting = false;
		[XReadOnly]
		public Vector2 rawMoveInput = Vector2.zero;
		[XReadOnly]
		public XSmoothDampedVector3 velocity = new XSmoothDampedVector3(Vector3.zero, 0.1f);

		#endregion

		#region Components

		private XCamFirstPerson lookCam;

		#endregion

		#region Methods

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			InitMoveActions();
			lookCam = GetComponent<XCamFirstPerson>();
		}

		private void OnEnable()
		{
			inputActions.Enable();
		}

		void InitMoveActions()
		{
            //moveAction = InputSystem.actions.FindAction(moveActionName);
            //sprintAction = InputSystem.actions.FindAction(sprintActionName);
            moveAction = inputActions.FindAction(moveActionName);
            sprintAction = inputActions.FindAction(sprintActionName);

            LogErrorIfActionNotFound(moveAction, moveActionName);
			LogErrorIfActionNotFound(sprintAction, sprintActionName);
		}

		private void LogErrorIfActionNotFound(InputAction action, string actionName)
		{
			if (action == null)
			{
				Debug.LogError($"Input Action '{actionName}' not found. Please check your input configuration.");
			}
		}

		private void UpdateSprintMode()
		{
			if (sprintAction == null)
			{
				isSprinting = false;
				return;
			}

			switch (sprintSwitchMethod)
			{
				case SprintSwitchMethod.Hold:
					isSprinting = sprintAction.IsPressed();
					break;
				case SprintSwitchMethod.Toggle:
					if (sprintAction.triggered)
					{
						isSprinting = !isSprinting;
					}
					break;
			}
		}

		private void UpdateVelocity()
		{
			// raw to 3D movement delta
			rawMoveInput = moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
			var delta = new Vector3(rawMoveInput.x, 0, rawMoveInput.y);

			// scale by speed and time
			float speed = isSprinting ? sprintSpeed : normalSpeed;
			speed = lookCam.isLooking ? speed : 0; // isLooking only affect targetMoveDelta to let movement easing continue even after exit look mode
			var targetVelocity = CameraTransform.TransformDirection(delta) * speed;

			// easing
			moveEasingTime = Mathf.Max(moveEasingTime, 0.01f);
			velocity.smoothTime = moveEasingTime;
			velocity.targetValue = targetVelocity;
			velocity.Update();
		}

		private void Move()
		{
			CameraTransform.position += velocity.value * Time.deltaTime;
		}

		// Update is called once per frame
		void Update()
		{
			UpdateSprintMode();
			UpdateVelocity();
			Move();
		}

		#endregion
	}
}