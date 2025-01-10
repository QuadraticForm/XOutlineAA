using UnityEngine;

namespace x
{
	[RequireComponent(typeof(CharacterController))] // TODO, really needed?
	public class XCharacterMover : MonoBehaviour
	{
		#region Character

		[Header("Character")]
		[XComment("These values, if not set, the current GameObject will be used.", 3)]
		public Transform character = null;
		public Transform Character { get { return character? character : transform; } }

		public XAbstractCharacterMoveAnimator moveAnimator = null;

		#endregion

		#region Target Set By Controller

		[Header("Target Set By Controller")]
		public float targetSpeed = 0;

		public Vector3 targetMoveDirection = new Vector3 (0, 0, 0);
		public Vector3 targetFacingDirection = new Vector3(0, 0, 0); // not implemented

		public bool jump = false;

		#endregion

		#region Speed Settings

		[Header("Speed Settings")]
		[Min(0)]
		public float normalSpeed = 2.0f;
		[Min(0)]
		public float maxSpeed = 5.335f;

		[Tooltip("Acceleration and deceleration")]
		public float speedChangeRate = 10.0f;

		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float rotationSmoothTime = 0.12f;

		#endregion

		#region Jump & Gravity

		[Space(10)]
		[Header("Jump & Gravity")]
		public float jumpHeight = 1.2f;
		public float gravity = -9.81f;
		public float maxFallingSpeed = 50.0f;

		#endregion

		#region Grounded Check

		[Header("Player Grounded Check")]
		[Tooltip("If the character is grounded or not")]
		public bool Grounded = true;

		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;

		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;

		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		#endregion

		#region Downstairs Falling Prevention

		[Space(10)]
		[Header("Downstairs Falling Prevention")]
		[XComment("These are used to prevent the character from falling when walking down stairs.", 3)]

		public float fallTimeout = 0.25f;
		public float groundedVerticalSpeed = -2.0f;

		private float _fallTimer = 0;

		#endregion

		#region Callbacks



		#endregion

		#region Internal

		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;		

		private CharacterController _characterController = null; // TODO, is this really needed?

		#endregion

		private void Start()
		{
			if (moveAnimator == null)
				moveAnimator = GetComponent<XAbstractCharacterMoveAnimator>();

			_characterController = GetComponent<CharacterController>();

			// reset our timeouts on start
			_fallTimer = fallTimeout;
		}

		/// <summary>
		/// Do nothing, other game logic should set target values in Update.
		/// Actuall movement and rotation is done in LateUpdate.
		/// </summary>
		private void Update()
		{
			
		}

		private void LateUpdate()
		{
			GroundedCheck();
			JumpAndGravity();
			Rotate();
			Move();
			// TestMoveObject();
		}

		protected virtual void GroundedCheck()
		{
			// set sphere position, with offset
			var spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		protected virtual void Rotate()
		{
			// Stop rotation if no movement
			if (targetSpeed < 0.0001f) // TODO remove magic number
				return;

			// Ensure the target direction is a normalized direction vector
			targetMoveDirection.Normalize();

			// Calculate the target rotation based on the target direction
			var targetRotation = Quaternion.LookRotation(targetMoveDirection);

			// Get the current and target Yaw (rotation around the Y axis angles) in degrees
			var currentRotation = Character.rotation;
			float currentYaw = currentRotation.eulerAngles.y;
			float targetYaw = targetRotation.eulerAngles.y;

			// Calculate the new Yaw angle using Mathf.SmoothDampAngle
			float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _rotationVelocity, rotationSmoothTime);

			// Apply the new Yaw angle to achieve smooth rotation
			Character.rotation = Quaternion.Euler(0, newYaw, 0);

			moveAnimator.OnRotationChange(currentRotation, Character.rotation);
		}

		protected virtual void Move()
		{
			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// a reference to the players current horizontal velocity
			var currentHorizontalSpeed = (new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.z)).magnitude;

			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * speedChangeRate);

			bool canMoveHorizontal = moveAnimator ? moveAnimator.CanMoveHorizontal() : true;
			bool canMoveVertical = moveAnimator ? moveAnimator.CanMoveVertical() : true;

			var moveVector = Vector3.zero;

			if (canMoveHorizontal)
				moveVector = targetMoveDirection.normalized * _speed * Time.deltaTime;

			moveAnimator.OnMoveStateChange(moveVector.magnitude / Time.deltaTime, moveVector.normalized, Character.forward);

			if (canMoveVertical)
				moveVector += new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;

			_characterController.Move(moveVector);
		}

		/*
		void TestMoveObject()
		{
			_speed = targetSpeed;

			transform.position += targetMoveDirection.normalized * (_speed * Time.deltaTime);

			moveAnimator.OnMoveStateChange(_speed, targetMoveDirection.normalized, Character.forward);
		}
		*/

		// TODO make this simpler
		protected virtual void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimer = fallTimeout;

				moveAnimator.OnLand();

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = groundedVerticalSpeed; // this is to prevent entering free fall state when going down slopes
				}

				// Jump, and don't allow jumping non-stop
				if (jump)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

					// update animator if using character
					moveAnimator.OnJump();
				}
			}
			else
			{
				// fall timeout
				if (_fallTimer >= 0.0f)
				{
					_fallTimer -= Time.deltaTime;
				}
				else
				{
					moveAnimator.OnFall();
				}
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < maxFallingSpeed)
			{
				_verticalVelocity += gravity * Time.deltaTime;
			}
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(
				new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
				GroundedRadius);
		}
	}
}