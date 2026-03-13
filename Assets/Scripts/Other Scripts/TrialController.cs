using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// TrialController.cs
/// 
/// Orchestrates the full within-subjects study:
///   - 4 conditions x 5 trials = 20 trials total
///   - Counterbalanced condition order per participant
///   - Auto-forward camera movement between trials
///   - Response time logging to a CSV file
///
/// Setup:
///   1. Attach to Study Manager GameObject
///   2. Assign playerMovement, vehicleSpawner, inputDetector, highlightManager
///   3. Assign the Main Camera transform to playerCamera
///   4. Press Play — the study starts automatically
/// </summary>
public class TrialController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector References
    // ------------------------------------------------------------------

    [Header("References")]
    public PlayerForwardMovement playerMovement;
    public VehicleSpawner        vehicleSpawner;
    public VRInputDetector       inputDetector;
    public HazardHighlightManager highlightManager;
    public Transform             playerCamera;

    [Header("Trial Settings")]
    [Tooltip("Number of trials per condition.")]
    public int trialsPerCondition = 5;

    [Tooltip("Time in seconds the player moves before the hazard appears.")]
    public float approachDuration = 3f;

    [Tooltip("Time between trials (inter-trial interval) in seconds.")]
    public float interTrialInterval = 2f;

    [Tooltip("Maximum time allowed for participant to respond (seconds).")]
    public float responseTimeLimit = 10f;

    [Header("Participant")]
    public int participantID = 1;

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private List<HazardHighlightManager.HighlightCondition> _trialSequence;
    private int    _currentTrialIndex = 0;
    private float  _trialStartTime;
    private bool   _studyComplete     = false;
    private bool   _waitingForResponse = false;

    // Results storage.
    private List<TrialResult> _results = new List<TrialResult>();

    // ------------------------------------------------------------------

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main.transform;

        BuildTrialSequence();
        inputDetector.OnDetectionButtonPressed += HandleDetectionResponse;
        StartCoroutine(RunStudy());
    }

    private void OnDestroy()
    {
        if (inputDetector != null)
            inputDetector.OnDetectionButtonPressed -= HandleDetectionResponse;
    }

    // ------------------------------------------------------------------
    // Study flow
    // ------------------------------------------------------------------

    private IEnumerator RunStudy()
    {
        Debug.Log($"[TrialController] Study started. {_trialSequence.Count} trials total.");

        yield return new WaitForSeconds(1f); // brief pause before first trial

        for (_currentTrialIndex = 0;
             _currentTrialIndex < _trialSequence.Count;
             _currentTrialIndex++)
        {
            yield return StartCoroutine(RunTrial(_currentTrialIndex));
            yield return new WaitForSeconds(interTrialInterval);
        }

        EndStudy();
    }

    private IEnumerator RunTrial(int trialIndex)
    {
        var condition = _trialSequence[trialIndex];
        int conditionNumber = (int)condition;
        int trialWithinCondition = (trialIndex % trialsPerCondition) + 1;

        Debug.Log($"[TrialController] Trial {trialIndex + 1} | " +
                  $"Condition: {condition} | " +
                  $"Trial within condition: {trialWithinCondition}");

        // --- 1. Set condition on highlight manager. ---
        highlightManager.SetActiveCondition(condition);

        // --- 2. Spawn 5 vehicles ahead of the player. ---
        GameObject hazardVehicle = vehicleSpawner.SpawnVehicles(playerCamera.position, condition);
        

        // The hazard vehicle's body (child) needs to be the current hazard.
        // VehicleSpawner returns the root; the body with scripts is the first child.
        GameObject hazardBody = hazardVehicle.transform.GetChild(0).gameObject;
        highlightManager.currentHazard = hazardBody;

        // --- 3. Start moving the player forward. ---
        playerMovement.Resume();

        // --- 4. Wait for approach duration before activating highlight. ---
        yield return new WaitForSeconds(approachDuration);

        // --- 5. Activate the highlight and start the response timer. ---
        if (condition == HazardHighlightManager.HighlightCondition.PeripheralHalo)
        yield return new WaitForSeconds(1.5f);

        // Activate the highlight and start the response timer.
        highlightManager.SetActiveCondition(condition);
        _trialStartTime     = Time.time;
        _waitingForResponse = true;
        inputDetector.StartListening();

        Debug.Log($"[TrialController] Hazard highlighted. Waiting for response...");

        // --- 6. Wait for response or timeout. ---
        float elapsed = 0f;
        while (_waitingForResponse && elapsed < responseTimeLimit)
        {
            elapsed = Time.time - _trialStartTime;
            yield return null;
        }

        // --- 7. Record result. ---
        bool responded    = !_waitingForResponse;
        float responseTime = responded ? (Time.time - _trialStartTime) : responseTimeLimit;

        _results.Add(new TrialResult
        {
            ParticipantID          = participantID,
            TrialNumber            = trialIndex + 1,
            Condition              = condition.ToString(),
            TrialWithinCondition   = trialWithinCondition,
            ResponseTime           = responseTime,
            Responded              = responded
        });

        Debug.Log($"[TrialController] Response: {(responded ? $"{responseTime:F3}s" : "TIMEOUT")}");

        // --- 8. Clean up trial. ---
        inputDetector.StopListening();
        _waitingForResponse = false;
        playerMovement.Pause();
        highlightManager.SetActiveCondition(HazardHighlightManager.HighlightCondition.None);
        vehicleSpawner.ClearVehicles();
    }

    private void HandleDetectionResponse()
    {
        if (_waitingForResponse)
            _waitingForResponse = false;
    }

    private void EndStudy()
    {
        _studyComplete = true;
        playerMovement.Pause();
        SaveResults();
        Debug.Log("[TrialController] Study complete. Results saved.");
    }

    // ------------------------------------------------------------------
    // Trial sequence builder (counterbalanced)
    // ------------------------------------------------------------------

    private void BuildTrialSequence()
    {
        // All four conditions.
        var conditions = new List<HazardHighlightManager.HighlightCondition>
        {
            HazardHighlightManager.HighlightCondition.ObjectOutline,
            HazardHighlightManager.HighlightCondition.PeripheralHalo,
            HazardHighlightManager.HighlightCondition.DepthColour,
            HazardHighlightManager.HighlightCondition.DirectionalBeam
        };

        // Counterbalance condition order using participant ID (Latin square rotation).
        int offset = (participantID - 1) % conditions.Count;
        var ordered = new List<HazardHighlightManager.HighlightCondition>();
        for (int i = 0; i < conditions.Count; i++)
            ordered.Add(conditions[(i + offset) % conditions.Count]);

        // Build full sequence: each condition repeated trialsPerCondition times.
        _trialSequence = new List<HazardHighlightManager.HighlightCondition>();
        foreach (var cond in ordered)
            for (int t = 0; t < trialsPerCondition; t++)
                _trialSequence.Add(cond);
    }

    // private void BuildTrialSequence()
    // {
    //     _trialSequence = new List<HazardHighlightManager.HighlightCondition>
    //     {
    //         HazardHighlightManager.HighlightCondition.DirectionalBeam
    //     };
    // }
    // ------------------------------------------------------------------
    // CSV logging
    // ------------------------------------------------------------------

    private void SaveResults()
    {
        string folder = @"C:\Users\soume\OneDrive\Desktop\school\University\2025 HCI Masters\HCIN 5501\Final Project\VHHT\StudyResults";
        string path   = Path.Combine(folder,
            $"HazardStudy_P{participantID}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("ParticipantID,TrialNumber,Condition,TrialWithinCondition," +
                      "ResponseTime_s,Responded");

        foreach (var r in _results)
        {
            sb.AppendLine($"{r.ParticipantID},{r.TrialNumber},{r.Condition}," +
                          $"{r.TrialWithinCondition},{r.ResponseTime:F4},{r.Responded}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[TrialController] Results saved to: {path}");
    }

    // ------------------------------------------------------------------
    // Data class
    // ------------------------------------------------------------------

    private class TrialResult
    {
        public int    ParticipantID;
        public int    TrialNumber;
        public string Condition;
        public int    TrialWithinCondition;
        public float  ResponseTime;
        public bool   Responded;
    }

    // ------------------------------------------------------------------
    // On-screen status (optional debug UI)
    // ------------------------------------------------------------------

    private void OnGUI()
    {
        if (_studyComplete)
        {
            GUI.Label(new Rect(10, 10, 400, 30), "Study Complete. Thank you!");
            return;
        }

        if (_currentTrialIndex < _trialSequence.Count)
        {
            GUI.Label(new Rect(10, 10, 400, 25),
                $"Trial {_currentTrialIndex + 1} / {_trialSequence.Count}");
            GUI.Label(new Rect(10, 35, 400, 25),
                $"Condition: {_trialSequence[_currentTrialIndex]}");
            if (_waitingForResponse)
                GUI.Label(new Rect(10, 60, 400, 25),
                    $"Response time: {(Time.time - _trialStartTime):F2}s");
        }
    }
}
