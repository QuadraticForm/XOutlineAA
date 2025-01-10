using UnityEngine;
using UnityEngine.InputSystem;

public class XCamFirstPerson : MonoBehaviour
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

	#region Look Params

	// Defines how to enter and exit "look mode"
	// When out of look mode, the cursor is free (unaffected by script) and the camera doesn't rotate.
	// When in look mode, the camera rotates and the cursor state is controlled by the script.
	public enum LookSwitchMethod
    {
        AimAndExit,     // In look mode if 'Exit' isn't triggered; out of look mode if 'Exit' is triggered; enters again with 'Aim'
        AimToggle,      // Toggle look mode by using the aim action
        AimHold         // In look mode while the aim action is held down (key continuously pressed)
    }

	[XHeader("Look Params")]

    public LookSwitchMethod lookSwitchMethod = LookSwitchMethod.AimHold;

    public bool lockCursor = true; // Lock the cursor to the screen center when in look mode
    public bool hideCursor = true; // Hide the cursor when in look mode

    public float lookSpeed = 1.0f;
	public float lookEasingTime = 0.1f;

	#endregion

	#region Input Actions

	[XHeader("Input Actions")]
	public string lookActionName = "Look";
	public string aimActionName = "Aim";
	public string exitActionName = "Exit";

	private InputAction lookAction;
    private InputAction aimAction;
    private InputAction exitAction;

	#endregion

	#region Debug Fields

	[XHeader("Debug")]
	public XEmptyField _headerDummy;

	[XReadOnly]
	public bool isLooking = false;
	[XReadOnly]
	public Vector2 rawLookDelta = Vector2.zero;
	[XReadOnly]
	public XSmoothDampedVector2 lookDelta = new XSmoothDampedVector2(Vector2.zero, 0.1f);

	#endregion

	#region Methods

	// Start is called once before the first Update call after the MonoBehaviour is created
	void Start()
    {
		InitLookActions();

        // Lock and hide cursor if specified in settings and when looking mode is engaged
        UpdateCursorState();
    }

	void InitLookActions()
	{
		lookAction = InputSystem.actions.FindAction(lookActionName);
        aimAction = InputSystem.actions.FindAction(aimActionName);
        exitAction = InputSystem.actions.FindAction(exitActionName);

        LogErrorIfActionNotFound(lookAction, lookActionName);
        LogErrorIfActionNotFound(aimAction, aimActionName);
        LogErrorIfActionNotFound(exitAction, exitActionName);
	}

    private void LogErrorIfActionNotFound(InputAction action, string actionName)
    {
        if (action == null)
        {
            Debug.LogError($"Input Action '{actionName}' not found. Please check your input configuration.");
        }
    }

    private void UpdateCursorState()
    {
        if (isLooking)
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (hideCursor)
            {
                Cursor.visible = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateLookMode()
    {
		var exitTriggered = exitAction?.triggered ?? false;
		var aimTriggered = aimAction?.triggered ?? false;
		var aimHeld = aimAction?.IsPressed() ?? false;

		if (lookSwitchMethod == LookSwitchMethod.AimAndExit)
		{
			if (exitTriggered)
            {
                isLooking = false;
            }
            else if (aimTriggered)
            {
                isLooking = true;
            }
		}

		if (lookSwitchMethod == LookSwitchMethod.AimToggle)
		{
			if (aimTriggered)
            {
                isLooking = !isLooking;
            }
		}

		if (lookSwitchMethod == LookSwitchMethod.AimHold)
		{
			isLooking = aimHeld;
		}
    }

	private void UpdateLookDelta()
    {
		// raw to target

        rawLookDelta = lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();

		var _lookSpeed = isLooking ? lookSpeed : 0; // isLooking only affects target speed to let easing continue after exiting look mode

        var targetLookDelta = _lookSpeed * rawLookDelta;

		// easing

		lookEasingTime = Mathf.Max(lookEasingTime, 0.01f);

		lookDelta.smoothTime = lookEasingTime;

		lookDelta.targetValue = targetLookDelta;

		lookDelta.Update();
	}

	private void RotateByLookDelta()
	{
		var euler = CameraTransform.rotation.eulerAngles;

		euler.x += lookDelta.value.y;
		euler.y += lookDelta.value.x;

		CameraTransform.rotation = Quaternion.Euler(euler);
	}

	public void UpdateAndApply()
	{
		UpdateLookMode();
        UpdateCursorState();
		UpdateLookDelta();
		RotateByLookDelta();
	}

    // Update is called once per frame
    void Update()
    {
		UpdateAndApply();
    }

	#endregion
}