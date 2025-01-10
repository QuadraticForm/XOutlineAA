using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace x
{
	public class XCommonInputStates : MonoBehaviour
	{
		#region Class Scope Instance
		// add a class-scope instance of XCommonInput here
		public static XCommonInputStates _instance = null;
		public static XCommonInputStates _fallbackInstance = null;
		public static XCommonInputStates Instance
		{
			get
			{
				// instances will register themselves to _instance if they are marked global

				// the fallback instance is only used when there is no global instance registered
				if (_instance == null && _fallbackInstance == null)
				{
					_fallbackInstance = new GameObject("XCommonInputStates_Fallback").AddComponent<XCommonInputStates>();
					_fallbackInstance.global = false; // trick to prevent the fallback instance from registering itself
				}

				return _instance == null ? _fallbackInstance : _instance;
			}
		}

		[XHeader("Global Instance")]
		public bool global = true;

		#endregion

		#region Common Input Actions (Unity Input System)

		private List<InputAction> _allInputActions = new List<InputAction>();

		private InputAction moveAction;
		private InputAction lookAction;

		private InputAction fireAction;
		private InputAction jumpAction;
		private InputAction sprintAction;
		private InputAction aimAction;

		private InputAction zoomInAction;
		private InputAction zoomOutAction;

		private InputAction[] weaponSwitchActions = new InputAction[9]; // Array to hold weapon actions, Weapon 0 is usually the non-weapon action, like fists or melee

		#endregion

		#region XInputStates
		
		private List<XInputStateBase> _allInputStates = new List<XInputStateBase>();

		[XHeader("Input States", addLeftMargin:true)]
		public XInputStateVector2 move;
		public XInputStateVector2 look;

		public XInputStateFloat fire;
		public XInputStateFloat jump;
		public XInputStateFloat sprint;
		public XInputStateFloat aim;

		public XInputStateFloat zoomIn;
		public XInputStateFloat zoomOut;

		#endregion

		#region Weapon Switch 

		[XHeader("Weapon")]

		public XInputStateFloat[] weaponSwitches = new XInputStateFloat[9];

		[XHeader("Weapon Switch Events", addLeftMargin:true)]
		[XDrawEventListenerCount, XFoldout]
		public UnityEvent<int> onWeaponSwitchPressed;
		[XDrawEventListenerCount, XFoldout]
		public UnityEvent<int> onWeaponSwitchHeld;

		#endregion

		[XHeader("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		[XHeader("Debug")]
		public bool logInputValue = true;

		#region Respond to PlayerInput's SendMessage or BroadcastMessage
		public void OnMove(InputValue value)
		{
			move.SetValueFromObject(value);
			LogInput("Move", value);
		}

		public void OnLook(InputValue value)
		{
			look.SetValueFromObject(value);
			LogInput("Look", value);
		}

		public void OnFire(InputValue value)
		{
			fire.SetValueFromObject(value);
			LogInput("Fire", value);
		}

		public void OnJump(InputValue value)
		{
			jump.SetValueFromObject(value);
			LogInput("Jump", value);
		}

		public void OnSprint(InputValue value)
		{
			sprint.SetValueFromObject(value);
			LogInput("Sprint", value);
		}

		public void OnAim(InputValue value) // Added OnAim method
		{
			aim.SetValueFromObject(value);
			LogInput("Aim", value);
		}
		#endregion

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}

		private void LogInput(string inputName, InputValue value)
		{
			if (logInputValue)
			{
				Debug.Log($"{inputName}: {value.Get()}");
			}
		}

		private bool IsPointerOverUI()
		{
			if (EventSystem.current == null)
				return false;

			return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
		}

		private void GlobalInstanceRegister()
		{
			if (!global)
				return;

			_instance = this;
		}

		private void InitInputActions()
		{
			Func<string, InputAction> findAndAddAction = 
				(actionName) =>
				{
					var action = InputSystem.actions.FindAction(actionName);
					_allInputActions.Add(action);
					return action;
				};

			moveAction = findAndAddAction("Move");
			lookAction = findAndAddAction("Look");

			fireAction = findAndAddAction("Fire");
			jumpAction = findAndAddAction("Jump");
			sprintAction = findAndAddAction("Sprint");
			aimAction = findAndAddAction("Aim");

			zoomInAction = findAndAddAction("Zoom In");
			zoomOutAction = findAndAddAction("Zoom Out");

			for (int i = 0; i < weaponSwitchActions.Length; i++)
			{
				weaponSwitchActions[i] = findAndAddAction("Weapon " + i);
			}
		}

		private void InitXInputStates()
		{
			// add all input states to the list
			_allInputStates.Add(move);
			_allInputStates.Add(look);

			_allInputStates.Add(fire);
			_allInputStates.Add(jump);
			_allInputStates.Add(sprint);
			_allInputStates.Add(aim);

			_allInputStates.Add(zoomIn);
			_allInputStates.Add(zoomOut);

			for (int i = 0; i < weaponSwitches.Length; i++)
			{
				_allInputStates.Add(weaponSwitches[i]);
			}
		}

		private void Start()
		{
			GlobalInstanceRegister();

			InitInputActions();
			InitXInputStates();

			SetCursorState(false);
		}

		private void Update()
		{
			GlobalInstanceRegister();

			//
			// read values from input actions
			//

			// if the pointer is over UI, reset all input values
			if (IsPointerOverUI())
			{
				// update all input states using a loop
				for (int i = 0; i < _allInputStates.Count; i++)
				{
					_allInputStates[i].ResetValue();
				}
			}
			// normal input reading
			else
			{
				// update all input states using a loop
				for (int i = 0; i < _allInputStates.Count; i++)
				{
					_allInputStates[i].SetValue(_allInputActions[i]);
				}
			}

			//
			// update input states
			//

			// call OnUpdate for all input states using a loop
			for (int i = 0; i < _allInputStates.Count; i++)
			{
				_allInputStates[i].OnUpdate();
			}

			//
			// special case for weapon switch
			// call the event when any weapon switch is pressed
			//

			for (int i = 0; i < weaponSwitches.Length; i++)
			{
				if (weaponSwitches[i].pressed)
				{
					onWeaponSwitchPressed.Invoke(i);
				}
			}
		}

		private void LateUpdate()
		{
			// call OnLateUpdate for all input states using a loop
			for (int i = 0; i < _allInputStates.Count; i++)
			{
				_allInputStates[i].OnLateUpdate();
			}
		}
	}
}