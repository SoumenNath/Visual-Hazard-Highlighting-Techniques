using UnityEngine;

/// <summary>
/// PlayerForwardMovement.cs
/// Moves the player (camera rig) forward at a constant system-controlled
/// speed along the Z axis. No user input controls the speed or direction.
///
/// Attach to your Camera or VR Camera Rig GameObject.
/// The TrialController calls Pause() and Resume() between trials.
/// </summary>
public class PlayerForwardMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Forward movement speed in world units per second.")]
    public float speed = 5f;

    [Tooltip("If true, the player moves automatically on Start.")]
    public bool autoStart = false;

    // ------------------------------------------------------------------

    private bool _isMoving = false;

    // ------------------------------------------------------------------

    private void Start()
    {
        if (autoStart)
            Resume();
    }

    private void Update()
    {
        if (!_isMoving) return;
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);
    }

    // ------------------------------------------------------------------
    // Public API (called by TrialController)
    // ------------------------------------------------------------------

    public void Resume() => _isMoving = true;
    public void Pause()  => _isMoving = false;

    public bool IsMoving => _isMoving;
}
