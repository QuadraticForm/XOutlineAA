
using System.Collections.Generic;
using UnityEngine;

namespace x
{
	public class XAbstractCharacterMoveAnimator : MonoBehaviour
	{
		public virtual void OnRotationChange(Quaternion _old, Quaternion _new)
		{
		}

		public virtual void OnMoveStateChange(float _speed, Vector3 _movingDirection, Vector3 _facingDirection)
		{
		}

		public virtual void OnJump()
		{
		}

		public virtual void OnFall()
		{
		}

		public virtual void OnLand()
		{
		}

		public virtual bool CanMoveHorizontal()
		{
			return true;
		}

		public virtual bool CanMoveVertical()
		{
			return true;
		}

		public virtual bool CanJump()
		{
			return true;
		}
	}

	/// <summary>
	/// The default implementation of XAbstractCharacterMoveAnimator using Unity's Animator.
	/// </summary>
	public class XCharacterMoveAnimator : XAbstractCharacterMoveAnimator
	{
		[Header("Animation")]
		public Animator animator;

	#if UNITY_EDITOR
		[Header("Params")]
		public XEmptyField emptyField; // a workaround for header margin when used with custom drawn property XSmoothDampedFloat
	#endif

		public XSmoothDampedFloat smoothDampedSpeed;

		[Header("Animation State Names")]

		public string stateNameSpeed = "Speed";
		public string stateNameGrounded = "Grounded";
		public string stateNameJump = "Jump";
		public string stateNameFall = "FreeFall";

		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFall;

		[Header("Move Limit")]
		public bool limitMoveHorizontalByAnimState = false;

		/// <summary>
		/// only allow horizontal movement when the animator state is in this list
		/// </summary>
		public List<string> enableMoveHorizontalAnimStates = new List<string>();

		private List<int> _moveHorizontalAnimStateIDs = new List<int>();

		private bool _canMoveHorizontal = true;

		public override void OnRotationChange(Quaternion _old, Quaternion _new)
		{

		}

		public override void OnMoveStateChange(float _speed, Vector3 _movingDirection, Vector3 _facingDirection)
		{
			smoothDampedSpeed.targetValue = _speed;
		}

		public override void OnJump()
		{
			animator.SetBool(_animIDJump, true);
			animator.SetBool(_animIDGrounded, false);
		}

		public override void OnFall()
		{
			animator.SetBool(_animIDFall, true);
			animator.SetBool(_animIDGrounded, false);
		}

		public override void OnLand()
		{
			animator.SetBool(_animIDJump, false);
			animator.SetBool(_animIDFall, false);
			animator.SetBool(_animIDGrounded, true);
		}

		public override bool CanMoveHorizontal() => _canMoveHorizontal;

		private void Start()
		{
			if (animator == null)
				TryGetComponent(out animator);

			AssignAnimationIDs();
		}

		private void Update()
		{
			smoothDampedSpeed.Update();

			animator.SetFloat(_animIDSpeed, smoothDampedSpeed.value);

			// limit move horizontal by anim state

			_canMoveHorizontal = true;

			if (limitMoveHorizontalByAnimState)
			{
				var currentNameHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
				_canMoveHorizontal = _moveHorizontalAnimStateIDs.Contains(currentNameHash);
			}
		}

		private void AssignAnimationIDs()
		{
			_animIDSpeed = Animator.StringToHash(stateNameSpeed);
			_animIDGrounded = Animator.StringToHash(stateNameGrounded);
			_animIDJump = Animator.StringToHash(stateNameJump);
			_animIDFall = Animator.StringToHash(stateNameFall);

			foreach (var stateName in enableMoveHorizontalAnimStates)
			{
				_moveHorizontalAnimStateIDs.Add(Animator.StringToHash(stateName));
			}
		}

		#region Audio
		private void OnFootstep(AnimationEvent animationEvent)
		{
			/*
			if (animationEvent.animatorClipInfo.weight > 0.5f)
			{
				if (FootstepAudioClips.Length > 0)
				{
					var index = Random.Range(0, FootstepAudioClips.Length);
					AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
				}
			}
			*/
		}

		private void OnLand(AnimationEvent animationEvent)
		{
			/*
			if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip)
			{
				AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
			}
			*/
		}

		[Header("Audio")]

		public AudioClip LandingAudioClip;
		public AudioClip[] FootstepAudioClips;
		[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

		#endregion
	}
}