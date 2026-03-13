using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// VRInputDetector.cs
/// Listens for a VR controller button press and fires an event that the
/// TrialController subscribes to. Uses the new Unity Input System.
///
/// Assign the Input Action for your desired button in the Inspector,
/// or use the default which listens for either trigger button on
/// left or right XR controllers.
///
/// Attach to any persistent GameObject (e.g. Study Manager).
/// </summary>
public class VRInputDetector : MonoBehaviour
{
    [Header("Input Bindings")]
    [Tooltip("Input action reference for the detection button. " +
             "Leave empty to use keyboard Space as fallback for testing.")]
    public InputActionReference detectionButtonAction;

    // Fired when the participant presses the detection button.
    public event Action OnDetectionButtonPressed;

    private bool _isListening = false;

    // ------------------------------------------------------------------

    //Old for VR
    // private void OnEnable()
    // {
    //     if (detectionButtonAction != null)
    //     {
    //         detectionButtonAction.action.Enable();
    //         detectionButtonAction.action.performed += HandleButtonPress;
    //     }
    // }

    //New for keyboard testing
    private void OnEnable()
    {
        if (detectionButtonAction != null)
        {
            detectionButtonAction.action.Enable();
            detectionButtonAction.action.performed += HandleButtonPress;
        }
        // No VR action assigned — keyboard fallback in Update() handles input
    }

    private void OnDisable()
    {
        if (detectionButtonAction != null)
            detectionButtonAction.action.performed -= HandleButtonPress;
    }

    // Old for VR
    // private void Update()
    // {
    //     if (!_isListening) return;

    //     // Keyboard spacebar fallback for desktop testing.
    //     if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
    //         FireDetection();

    //     // Also check XR controller triggers directly if no action assigned.
    //     if (detectionButtonAction == null)
    //         CheckXRFallback();
    // }

    //New for keyboard testing
    private void Update()
    {
        if (!_isListening) return;

        // Spacebar for desktop testing
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            FireDetection();

        // Enter key as additional fallback
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            FireDetection();
    }
    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void StartListening() => _isListening = true;
    public void StopListening()  => _isListening = false;

    // ------------------------------------------------------------------

    private void HandleButtonPress(InputAction.CallbackContext ctx)
    {
        if (_isListening)
            FireDetection();
    }

    private void FireDetection()
    {
        _isListening = false;   // prevent double-firing
        OnDetectionButtonPressed?.Invoke();
    }

    private void CheckXRFallback()
    {
        // Try to read XR controller trigger via Gamepad if available.
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            FireDetection();
    }
}
