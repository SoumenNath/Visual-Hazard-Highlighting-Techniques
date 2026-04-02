using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using System;

/// <summary>
/// VRInputDetector.cs
/// Listens for a VR controller button press and fires an event that the
/// TrialController subscribes to. Uses the new Unity Input System.
/// Supports both VR controller trigger and keyboard fallback for desktop testing.
/// </summary>
public class VRInputDetector : MonoBehaviour
{
    [Header("Input Bindings")]
    [Tooltip("Input action reference for the detection button.")]
    public InputActionReference detectionButtonAction;

    // Fired when the participant presses the detection button.
    public event Action OnDetectionButtonPressed;

    private bool _isListening = false;

    // ------------------------------------------------------------------

    private void OnEnable()
    {
        if (detectionButtonAction != null)
        {
            detectionButtonAction.action.Enable();
            detectionButtonAction.action.performed += HandleButtonPress;
            Debug.Log("[VRInputDetector] Action enabled and listening.");
        }
        else
        {
            Debug.LogWarning("[VRInputDetector] No action assigned in Inspector!");
        }
    }

    private void OnDisable()
    {
        if (detectionButtonAction != null)
        {
            detectionButtonAction.action.performed -= HandleButtonPress;
            detectionButtonAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!_isListening) return;

        // --- Debug XR controller detection ---
        var devices = InputSystem.devices;
        bool foundController = false;
        foreach (var device in devices)
        {
            if (device is UnityEngine.InputSystem.XR.XRController)
            {
                foundController = true;
                Debug.Log($"[VRInputDetector] XR Controller found: {device.name} " +
                          $"| Display: {device.displayName}");
            }
        }
        if (!foundController)
            Debug.LogWarning("[VRInputDetector] No XR controllers detected by Input System.");

        // --- Keyboard fallback for desktop testing ---
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[VRInputDetector] Spacebar pressed — firing detection.");
            FireDetection();
        }

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Debug.Log("[VRInputDetector] Enter pressed — firing detection.");
            FireDetection();
        }
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void StartListening()
    {
        _isListening = true;
        Debug.Log("[VRInputDetector] Started listening for input.");
    }

    public void StopListening()
    {
        _isListening = false;
        Debug.Log("[VRInputDetector] Stopped listening.");
    }

    // ------------------------------------------------------------------

    private void HandleButtonPress(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[VRInputDetector] HandleButtonPress called. " +
                  $"Listening: {_isListening} | " +
                  $"Control: {ctx.control?.name} | " +
                  $"Device: {ctx.control?.device?.name}");

        if (_isListening)
        {
            Debug.Log("[VRInputDetector] VR controller button pressed — firing detection.");
            FireDetection();
        }
        else
        {
            Debug.LogWarning("[VRInputDetector] Button pressed but not currently listening.");
        }
    }

    private void FireDetection()
    {
        _isListening = false;
        OnDetectionButtonPressed?.Invoke();
        Debug.Log("[VRInputDetector] Detection fired.");
    }
}