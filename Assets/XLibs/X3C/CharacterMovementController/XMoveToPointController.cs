using UnityEngine;

namespace x
{
	public class XMoveToPointController : MonoBehaviour
	{
		[XHeader("Character")]
		[XComment("these if not set, will use self")]
		public Transform character = null;
		public XCharacterMover characterMover = null;

		
		[XHeader("MoveTarget", addLeftMargin:true)]
		[XNullWarning]
		public Transform moveTarget = null;

		[XHeader("Parameters")]
		[Min(0)]
		public float deadZone = 0.05f;
		[Range(0f, 1f)]
		public float speedDamping = 0.9f;

		private void Start()
		{
			if (character == null) character = transform;
			if (characterMover == null) characterMover = GetComponent<XCharacterMover>();
		}

		private void Update()
		{
			if (moveTarget == null || character == null || characterMover == null)
				return;

			// calc distance

			var deltaVector = moveTarget.position - character.position;

			var distance = deltaVector.magnitude;
			if (distance < deadZone)
			{
				characterMover.targetSpeed = 0;
				return;
			}

			// set speed

			var speedMul = (1 - speedDamping);

			characterMover.targetSpeed = speedMul * distance / Time.deltaTime;

			if (characterMover.targetSpeed > characterMover.maxSpeed)
				characterMover.targetSpeed = characterMover.maxSpeed;

			// set direction

			deltaVector.Normalize();

			characterMover.targetMoveDirection = deltaVector;
			characterMover.targetFacingDirection = characterMover.targetMoveDirection;
		}
	}
}