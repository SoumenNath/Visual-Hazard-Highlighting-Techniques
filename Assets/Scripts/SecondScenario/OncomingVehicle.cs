using UnityEngine;

/// <summary>
/// OncomingVehicle.cs
/// Moves a vehicle toward the player (negative Z direction) at a constant speed.
/// If designated as the hazard, waits a random delay then gradually drifts
/// into the player's lane to simulate a head-on collision threat.
///
/// Attach to each spawned oncoming vehicle root GameObject.
/// </summary>
public class OncomingVehicle : MonoBehaviour
{
     
    [Header("Movement")]
    [Tooltip("Speed at which the vehicle moves toward the player.")]
    public float speed = 6f;

    [Header("Lane Drift (Hazard Only)")]
    [Tooltip("Minimum time before the hazard starts drifting into player lane.")]
    public float minDriftDelay = 1.5f;

    [Tooltip("Maximum time before the hazard starts drifting into player lane.")]
    public float maxDriftDelay = 4f;

    [Tooltip("How many seconds it takes to fully drift into the player lane.")]
    public float driftDuration = 2.5f;

    [Tooltip("Target X position in the player's lane (should match player's lane centre).")]
    public float playerLaneX = 0f;

    // ------------------------------------------------------------------

    private bool  _isHazard;
    private bool  _isDrifting;
    private bool  _driftComplete;
    private float _driftTimer;
    private float _driftDelay;
    private float _startX;
    private float _targetX;

    // Callback fired when drift begins — TrialController uses this to
    // activate the highlight and start the response timer.
    public System.Action OnDriftStarted;

    // ------------------------------------------------------------------

    public void Initialise(bool isHazard, float playerLaneXPos)
    {
        _isHazard    = isHazard;
        playerLaneX  = playerLaneXPos;

        if (_isHazard)
        {
            _driftDelay = Random.Range(minDriftDelay, maxDriftDelay);
            _startX     = transform.position.x;
            _targetX    = playerLaneXPos;
        }
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        // Move toward player.
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

        if (!_isHazard || _driftComplete) return;

        // Count down to drift.
        _driftTimer += Time.deltaTime;

        if (!_isDrifting && _driftTimer >= _driftDelay)
        {
            _isDrifting = true;
            _startX     = transform.position.x;
            OnDriftStarted?.Invoke();
        }

        if (_isDrifting)
        {
            float driftProgress = Mathf.Clamp01(
                (_driftTimer - _driftDelay) / driftDuration);

            // Smooth lateral drift using SmoothStep.
            float newX = Mathf.Lerp(_startX, _targetX,
                Mathf.SmoothStep(0f, 1f, driftProgress));

            transform.position = new Vector3(
                newX,
                transform.position.y,
                transform.position.z);

            if (driftProgress >= 1f)
                _driftComplete = true;
        }
    }

    // ------------------------------------------------------------------

    public bool IsHazard     => _isHazard;
    public bool HasDrifted   => _driftComplete;
    public bool IsDrifting   => _isDrifting;
}
