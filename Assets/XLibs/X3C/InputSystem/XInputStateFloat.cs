using System;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using System.Diagnostics.Contracts;

#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace x
{

	#region Base Classes

	[Serializable]
	public abstract class XInputStateBase
	{
		public abstract bool held { get; }

		/// <summary>
		/// True if the input was pressed this frame.
		/// 
		/// 如果输入在这一帧被按下，则为真。
		/// </summary>
		public bool pressed { get; protected set; } = false;

		/// <summary>
		/// True if the input was released this frame.
		/// 
		/// 如果输入在这一帧被释放，则为真。
		/// </summary>
		public bool released { get; protected set; } = false;

		[XDrawEventListenerCount, XFoldout]
		public UnityEvent onPressed = new UnityEvent();

		[XDrawEventListenerCount, XFoldout]
		public UnityEvent onHeld = new UnityEvent();

		[XDrawEventListenerCount, XFoldout]
		public UnityEvent onRelease = new UnityEvent();

		public void OnUpdate()
		{
			if (held)
			{
				onHeld.Invoke();
			}
		}

		public void OnLateUpdate()
		{
			pressed = false;
			released = false;
		}

		public abstract void ResetValue();

		public abstract void SetValueFromObject(object objValue);

#if ENABLE_INPUT_SYSTEM

		public abstract void SetValue(InputAction action);

#endif
	}

	[Serializable]
	public abstract class XInputStateBaseT<T> : XInputStateBase where T : struct
	{
		public T Value
		{
			get => _value;
			set => SetValue(value);
		}
		[SerializeField]
		protected T _value;

		[XFoldout]
		public UnityEvent<T> onValueChanged = new UnityEvent<T>();

		public override bool held => !IsZero(_value);

		public abstract bool IsZero(T value);

		public override void ResetValue() => SetValue(default);

		public virtual void SetValue(T newValue)
		{
			var oldValue = _value;
			_value = newValue;

			onValueChanged.Invoke(_value);

			if (IsZero(oldValue) && !IsZero(newValue))
			{
				pressed = true;
				released = false;
				onPressed.Invoke();
			}
			else if (!IsZero(oldValue) && IsZero(newValue))
			{
				pressed = false;
				released = true;
				onRelease.Invoke();
			}
			else
			{
				pressed = false;
				released = false;
			}
		}

		public override void SetValueFromObject(object objValue)
		{
			if (objValue is T tvalue)
			{
				SetValue(tvalue);
			}
		}

#if ENABLE_INPUT_SYSTEM

		public override void SetValue(InputAction action) => SetValue(action.ReadValue<T>());

#endif
	}

	#endregion

	#region Concrete Classes

	[Serializable]
	public class XInputStateFloat : XInputStateBaseT<float>
	{
		public override bool IsZero(float value) => value == 0;
	}

	[Serializable]
	public class XInputStateVector2 : XInputStateBaseT<Vector2>
	{
		public override bool IsZero(Vector2 value) => value == Vector2.zero;
	}

	#endregion

	#region Custom Property Drawer

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(XInputStateFloat))]
	public class XInputStateDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);

			var inputState = fieldInfo.GetValue(property.serializedObject.targetObject) as XInputStateFloat;

			if (inputState == null)
				return;

			int valueLabelWidth = 75;
			var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, valueLabelWidth, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(valueRect, inputState.Value.ToString("F2"));

			int listenerCount = inputState.onValueChanged.GetPersistentEventCount() +
								inputState.onPressed.GetPersistentEventCount() +
								inputState.onHeld.GetPersistentEventCount() +
								inputState.onRelease.GetPersistentEventCount();

			int listenerCountWidth = 75;
			var listenerCountRect = new Rect(position.xMax - listenerCountWidth, position.y, listenerCountWidth, EditorGUIUtility.singleLineHeight);
			listenerCountRect.x = Mathf.Max(listenerCountRect.x, valueRect.xMax);
			EditorGUI.LabelField(listenerCountRect, listenerCount + " Listeners");
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}

	[CustomPropertyDrawer(typeof(XInputStateVector2))]
	public class XInputStateVector2Drawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);

			var inputState = fieldInfo.GetValue(property.serializedObject.targetObject) as XInputStateVector2;

			if (inputState == null)
				return;

			int valueLabelWidth = 75;
			var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, valueLabelWidth, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(valueRect, inputState.Value.ToString("F2"));

			int listenerCount = inputState.onValueChanged.GetPersistentEventCount() +
								inputState.onPressed.GetPersistentEventCount() +
								inputState.onHeld.GetPersistentEventCount() +
								inputState.onRelease.GetPersistentEventCount();

			int listenerCountWidth = 75;
			var listenerCountRect = new Rect(position.xMax - listenerCountWidth, position.y, listenerCountWidth, EditorGUIUtility.singleLineHeight);
			listenerCountRect.x = Mathf.Max(listenerCountRect.x, valueRect.xMax);
			EditorGUI.LabelField(listenerCountRect, listenerCount + " Listeners");
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}

#endif

	#endregion
}
