using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public enum InputDeviceType
{
    Mouse,
    Keyboard,
    Gamepad
}

public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance { get; private set; }

    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.Keyboard;

    public event Action<InputDeviceType> OnInputDeviceChanged;

    [SerializeField] private float joystickDeadzone = 0.2f;
    [SerializeField] private float mouseSensitivityThreshold = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
        {
            return;
        }

        if (!HasActualInput(device, eventPtr))
        {
            return;
        }

        if (device is Gamepad)
        {
            SetCurrentDevice(InputDeviceType.Gamepad);
        }
        else if (device is Mouse)
        {
            SetCurrentDevice(InputDeviceType.Mouse);
        }
        else if (device is Keyboard)
        {
            SetCurrentDevice(InputDeviceType.Keyboard);
        }
    }

    private bool HasActualInput(InputDevice device, InputEventPtr eventPtr)
    {
        if (device is Gamepad gamepad)
        {
            foreach (var control in eventPtr.EnumerateChangedControls(device))
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed)
                {
                    return true;
                }
            }
            
            if (gamepad.leftStick.ReadValue().magnitude > joystickDeadzone ||
                gamepad.rightStick.ReadValue().magnitude > joystickDeadzone)
            {
                return true;
            }

            return false;
        }

        if (device is Mouse mouse)
        {
            if (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed)
            {
                return true;
            }

            foreach (var control in eventPtr.EnumerateChangedControls(device))
            {
                if (control == mouse.delta || control == mouse.position)
                {
                    if (mouse.delta.ReadValue().sqrMagnitude > mouseSensitivityThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        if (device is Keyboard keyboard)
        {
            return keyboard.anyKey.isPressed;
        }

        return false;
    }

    private void SetCurrentDevice(InputDeviceType deviceType)
    {
        if (CurrentDevice == deviceType)
        {
            return;
        }

        CurrentDevice = deviceType;
        Debug.Log($"Input device changed to: {CurrentDevice}");
        OnInputDeviceChanged?.Invoke(CurrentDevice);
    }
}