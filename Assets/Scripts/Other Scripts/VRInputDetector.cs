using UnityEngine;
using UnityEngine.XR;
using System;
using System.Collections.Generic;

/// <summary>
/// VRInputDetector.cs
/// Uses UnityEngine.XR.InputDevices directly instead of the new Input System
/// for more reliable Meta Quest controller detection via Meta Link.
/// </summary>
public class VRInputDetector : MonoBehaviour
{
    [Header("Input Bindings")]
    [Tooltip("Input action reference — kept for compatibility but not used in XR mode.")]
    public UnityEngine.InputSystem.InputActionReference detectionButtonAction;

    public event Action OnDetectionButtonPressed;

    private bool _isListening = false;

    private InputDevice _rightController;
    private InputDevice _leftController;

    private bool _rightTriggerWasPressed  = false;
    private bool _leftTriggerWasPressed   = false;
    private bool _rightGripWasPressed     = false;
    private bool _leftGripWasPressed      = false;
    private bool _rightPrimaryWasPressed  = false;
    private bool _leftPrimaryWasPressed   = false;

    // ------------------------------------------------------------------

    private void OnEnable()
    {
        InputDevices.deviceConnected    += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        FindControllers();
        Debug.Log("[VRInputDetector] Initialised — using XR InputDevices API.");
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected    -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    // ------------------------------------------------------------------

    private void OnDeviceConnected(InputDevice device)
    {
        Debug.Log($"[VRInputDetector] Device connected: {device.name} | " +
                  $"Characteristics: {device.characteristics}");
        FindControllers();
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        Debug.Log($"[VRInputDetector] Device disconnected: {device.name}");
        FindControllers();
    }

    private void FindControllers()
    {
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right |
            InputDeviceCharacteristics.Controller,
            rightHandDevices);

        if (rightHandDevices.Count > 0)
        {
            _rightController = rightHandDevices[0];
            Debug.Log($"[VRInputDetector] Right controller found: {_rightController.name}");
        }
        else
            Debug.LogWarning("[VRInputDetector] No right controller found.");

        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left |
            InputDeviceCharacteristics.Controller,
            leftHandDevices);

        if (leftHandDevices.Count > 0)
        {
            _leftController = leftHandDevices[0];
            Debug.Log($"[VRInputDetector] Left controller found: {_leftController.name}");
        }
        else
            Debug.LogWarning("[VRInputDetector] No left controller found.");

        // List ALL connected XR devices.
        var allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        Debug.Log($"[VRInputDetector] Total XR devices found: {allDevices.Count}");
        foreach (var d in allDevices)
            Debug.Log($"  Device: {d.name} | Characteristics: {d.characteristics}");
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        if (!_isListening)
        {
            ResetButtonStates();
            return;
        }

        // --- XR Controller input ---
        if (CheckButton(_rightController, CommonUsages.triggerButton,
                        ref _rightTriggerWasPressed,  "Right Trigger"))  return;
        if (CheckButton(_leftController,  CommonUsages.triggerButton,
                        ref _leftTriggerWasPressed,   "Left Trigger"))   return;
        if (CheckButton(_rightController, CommonUsages.gripButton,
                        ref _rightGripWasPressed,     "Right Grip"))     return;
        if (CheckButton(_leftController,  CommonUsages.gripButton,
                        ref _leftGripWasPressed,      "Left Grip"))      return;
        if (CheckButton(_rightController, CommonUsages.primaryButton,
                        ref _rightPrimaryWasPressed,  "Right Primary"))  return;
        if (CheckButton(_leftController,  CommonUsages.primaryButton,
                        ref _leftPrimaryWasPressed,   "Left Primary"))   return;

        // --- Keyboard fallback ---
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[VRInputDetector] Keyboard fallback pressed.");
            FireDetection();
        }
    }

    // ------------------------------------------------------------------

    private bool CheckButton(InputDevice device,
                              InputFeatureUsage<bool> usage,
                              ref bool wasPressed,
                              string buttonName)
    {
        if (!device.isValid) return false;

        bool isPressed = false;
        device.TryGetFeatureValue(usage, out isPressed);

        if (isPressed && !wasPressed)
        {
            Debug.Log($"[VRInputDetector] {buttonName} pressed — firing detection.");
            wasPressed = true;
            FireDetection();
            return true;
        }

        if (!isPressed) wasPressed = false;
        return false;
    }

    private void ResetButtonStates()
    {
        _rightTriggerWasPressed  = false;
        _leftTriggerWasPressed   = false;
        _rightGripWasPressed     = false;
        _leftGripWasPressed      = false;
        _rightPrimaryWasPressed  = false;
        _leftPrimaryWasPressed   = false;
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void StartListening()
    {
        _isListening = true;
        FindControllers();
        Debug.Log("[VRInputDetector] Started listening.");
    }

    public void StopListening()
    {
        _isListening = false;
        Debug.Log("[VRInputDetector] Stopped listening.");
    }

    private void FireDetection()
    {
        _isListening = false;
        OnDetectionButtonPressed?.Invoke();
        Debug.Log("[VRInputDetector] Detection event fired.");
    }
}
