using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// TrialControllerScenario2.cs
///
/// Orchestrates Scenario 2 — oncoming vehicles passing in the opposite lane,
/// with one vehicle randomly drifting into the player's lane as the hazard.
///
/// Key differences from TrialController (Scenario 1):
///   - Uses OncomingVehicleSpawner instead of VehicleSpawner
///   - Player continues moving forward throughout (no pause during trial)
///   - Highlight activates when the hazard STARTS drifting, not at trial start
///   - Response timer begins at drift onset
///
/// Setup:
///   Attach to a new empty GameObject called "StudyManagerScenario2",
///   or add alongside the existing TrialController on Study Manager and
///   enable/disable as needed per scenario.
/// </summary>
public class TrialControllerScenario2 : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------

    [Header("References")]
    public PlayerForwardMovement    playerMovement;
    public OncomingVehicleSpawner   oncomingSpawner;
    public VRInputDetector          inputDetector;
    public HazardHighlightManager   highlightManager;
    public Transform                playerCamera;

    [Header("Trial Settings")]
    public int   trialsPerCondition  = 5;
    public float interTrialInterval  = 3f;
    public float responseTimeLimit   = 10f;

    [Header("Participant")]
    public int participantID = 1;

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private List<HazardHighlightManager.HighlightCondition> _trialSequence;
    private int    _currentTrialIndex;
    private float  _trialStartTime;
    private bool   _waitingForResponse;
    private bool   _studyComplete;
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

    private IEnumerator RunStudy()
    {
        Debug.Log($"[Scenario2] Study started. {_trialSequence.Count} trials total.");
        yield return new WaitForSeconds(1f);

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
        var condition            = _trialSequence[trialIndex];
        int trialWithinCondition = (trialIndex % trialsPerCondition) + 1;

        //Debug.Log($"[Scenario2] Trial {trialIndex + 1} | Condition: {condition}");
        LogTrialProgress(trialIndex, condition);

        // --- 1. Set condition. ---
        highlightManager.SetActiveCondition(
            HazardHighlightManager.HighlightCondition.None);

        // --- 2. Spawn oncoming vehicles. ---
        var (hazardBody, hazardController) =
            oncomingSpawner.SpawnVehicles(playerCamera.position, condition);

        // Assign hazard body to highlight manager.
        highlightManager.currentHazard = hazardBody;

        // --- 3. Start player movement. ---
        playerMovement.Resume();

        // --- 4. Wait for hazard to start drifting. ---
        // The OncomingVehicle controller fires OnDriftStarted when drift begins.
        bool driftStarted = false;
        hazardController.OnDriftStarted = () => driftStarted = true;

        // Wait until drift starts or vehicles pass the player.
        float waitTimer = 0f;
        float maxWait   = 15f; // safety timeout
        while (!driftStarted && waitTimer < maxWait)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (!driftStarted)
        {
            // Vehicles passed without drift — skip trial.
            Debug.LogWarning("[Scenario2] Hazard drift did not trigger. Skipping trial.");
            oncomingSpawner.ClearVehicles();
            yield break;
        }

        // --- 5. Activate highlight at drift onset and start timer. ---
        highlightManager.SetActiveCondition(condition);
        _trialStartTime     = Time.time;
        _waitingForResponse = true;
        inputDetector.StartListening();

        Debug.Log("[Scenario2] Drift started — highlight active, timer running.");

        // --- 6. Wait for response or timeout. ---
        float elapsed = 0f;
        while (_waitingForResponse && elapsed < responseTimeLimit)
        {
            elapsed = Time.time - _trialStartTime;
            yield return null;
        }

        // --- 7. Record result. ---
        bool  responded    = !_waitingForResponse;
        float responseTime = responded
            ? (Time.time - _trialStartTime)
            : responseTimeLimit;

        _results.Add(new TrialResult
        {
            ParticipantID        = participantID,
            TrialNumber          = trialIndex + 1,
            Condition            = condition.ToString(),
            TrialWithinCondition = trialWithinCondition,
            ResponseTime         = responseTime,
            Responded            = responded
        });

        Debug.Log($"[Scenario2] Response: " +
                  $"{(responded ? $"{responseTime:F3}s" : "TIMEOUT")}");

        // --- 8. Clean up. ---
       // NEW — add player position reset
        inputDetector.StopListening();
        _waitingForResponse = false;
        playerMovement.Pause();
        highlightManager.SetActiveCondition(
            HazardHighlightManager.HighlightCondition.None);
        oncomingSpawner.ClearVehicles();

        // Reset player Z position so road never runs out.
        Vector3 resetPos = playerCamera.transform.position;
        resetPos.z = 0f;
        playerCamera.transform.position = resetPos;
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════");
        sb.AppendLine($"  STUDY COMPLETE — PARTICIPANT {participantID}");
        sb.AppendLine($"═══════════════════════════════════════════");
        sb.AppendLine($"  Total trials completed: {_results.Count}");
        sb.AppendLine($"  Results saved to CSV.");
        sb.AppendLine($"───────────────────────────────────────────");
        sb.AppendLine($"  Response summary:");

        var groups = new System.Collections.Generic.Dictionary
            <string, System.Collections.Generic.List<float>>();
        foreach (var r in _results)
        {
            if (!groups.ContainsKey(r.Condition))
                groups[r.Condition] = new System.Collections.Generic.List<float>();
            if (r.Responded)
                groups[r.Condition].Add(r.ResponseTime);
        }

        foreach (var kvp in groups)
        {
            float avg = kvp.Value.Count > 0
                ? kvp.Value[0] : 0f;
            float sum = 0f;
            foreach (var v in kvp.Value) sum += v;
            avg = kvp.Value.Count > 0 ? sum / kvp.Value.Count : 0f;

            sb.AppendLine($"  {kvp.Key,-22} " +
                        $"Responded: {kvp.Value.Count}/{trialsPerCondition} " +
                        $"| Avg RT: {avg:F3}s");
        }

        sb.AppendLine($"═══════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    // ------------------------------------------------------------------
    // Trial sequence 
    // ------------------------------------------------------------------

    // private void BuildTrialSequence()
    // {
    //     var conditions = new List<HazardHighlightManager.HighlightCondition>
    //     {
    //         HazardHighlightManager.HighlightCondition.ObjectOutline,
    //         HazardHighlightManager.HighlightCondition.PeripheralHalo,
    //         HazardHighlightManager.HighlightCondition.DepthColour,
    //         HazardHighlightManager.HighlightCondition.DirectionalBeam
    //     };

    //     int offset  = (participantID - 1) % conditions.Count;
    //     var ordered = new List<HazardHighlightManager.HighlightCondition>();
    //     for (int i = 0; i < conditions.Count; i++)
    //         ordered.Add(conditions[(i + offset) % conditions.Count]);

    //     _trialSequence = new List<HazardHighlightManager.HighlightCondition>();
    //     foreach (var cond in ordered)
    //         for (int t = 0; t < trialsPerCondition; t++)
    //             _trialSequence.Add(cond);
    // }

    private void LogTrialProgress(int trialIndex,
    HazardHighlightManager.HighlightCondition condition)
    {
        int block        = (trialIndex / 4) + 1;
        int posInBlock   = (trialIndex % 4) + 1;
        int remaining    = _trialSequence.Count - trialIndex - 1;

        // Count how many of each condition have been completed so far.
        var counts = new System.Collections.Generic.Dictionary
            <HazardHighlightManager.HighlightCondition, int>();
        foreach (var c in System.Enum.GetValues(
            typeof(HazardHighlightManager.HighlightCondition)))
        {
            var cond = (HazardHighlightManager.HighlightCondition)c;
            if (cond == HazardHighlightManager.HighlightCondition.None) continue;
            counts[cond] = 0;
        }
        for (int i = 0; i < trialIndex; i++)
            if (counts.ContainsKey(_trialSequence[i]))
                counts[_trialSequence[i]]++;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"───────────────────────────────────────────");
        sb.AppendLine($"  ▶ TRIAL {trialIndex + 1} / {_trialSequence.Count} " +
                    $"| Block {block} | Position {posInBlock}/4");
        sb.AppendLine($"  Condition: {condition}");
        sb.AppendLine($"  Trials remaining: {remaining}");
        sb.AppendLine($"  Condition counts so far:");
        foreach (var kvp in counts)
            sb.AppendLine($"    {kvp.Key,-20} {kvp.Value,2} / {trialsPerCondition}");
        sb.AppendLine($"───────────────────────────────────────────");
        Debug.Log(sb.ToString());
    }

    private void BuildTrialSequence()
    {
        _trialSequence = GenerateBalancedSequence(participantID);

        // Print full sequence in a readable format.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════");
        sb.AppendLine($"  PARTICIPANT {participantID} — TRIAL SEQUENCE");
        sb.AppendLine($"═══════════════════════════════════════════");
        sb.AppendLine($"  Total trials: {_trialSequence.Count}");
        sb.AppendLine($"  Trials per condition: {trialsPerCondition}");
        sb.AppendLine($"───────────────────────────────────────────");

        for (int i = 0; i < _trialSequence.Count; i++)
        {
            int block = (i / 4) + 1;
            int posInBlock = (i % 4) + 1;
            sb.AppendLine($"  Trial {i + 1,2} | Block {block,2} | " +
                        $"Position {posInBlock} | {_trialSequence[i]}");
        }

        sb.AppendLine($"═══════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    
    private List<HazardHighlightManager.HighlightCondition> GenerateBalancedSequence(int pID)
    {
        var conditions = new List<HazardHighlightManager.HighlightCondition>
        {
            HazardHighlightManager.HighlightCondition.ObjectOutline,
            HazardHighlightManager.HighlightCondition.PeripheralHalo,
            HazardHighlightManager.HighlightCondition.DepthColour,
            HazardHighlightManager.HighlightCondition.DirectionalBeam
        };

        // --- Step 1: Assign a Latin square row to this participant ---
        // Guarantees every condition order appears evenly across participants.
        // 4 conditions = 4 unique starting offsets, repeating every 4 participants.
        int offset = (pID - 1) % conditions.Count;
        var orderedConditions = new List<HazardHighlightManager.HighlightCondition>();
        for (int i = 0; i < conditions.Count; i++)
            orderedConditions.Add(conditions[(i + offset) % conditions.Count]);

        // --- Step 2: Build blocks of 4 (one of each condition per block) ---
        // With 10 trials per condition and 4 conditions = 40 trials total.
        // Divide into 10 blocks of 4, each containing all 4 conditions once.
        var blocks = new List<List<HazardHighlightManager.HighlightCondition>>();
        for (int b = 0; b < trialsPerCondition; b++)
        {
            var block = new List<HazardHighlightManager.HighlightCondition>(orderedConditions);
            // Shuffle within each block using a deterministic seed per participant + block.
            block = SeededShuffle(block, pID * 100 + b);
            blocks.Add(block);
        }

        // --- Step 3: Flatten blocks into final sequence ---
        var sequence = new List<HazardHighlightManager.HighlightCondition>();
        foreach (var block in blocks)
            foreach (var cond in block)
                sequence.Add(cond);

        return sequence;
    }

    private List<HazardHighlightManager.HighlightCondition> SeededShuffle(
        List<HazardHighlightManager.HighlightCondition> input, int seed)
    {
        var result = new List<HazardHighlightManager.HighlightCondition>(input);

        // Use System.Random with a fixed seed for deterministic shuffling.
        var rng = new System.Random(seed);
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }
    // ------------------------------------------------------------------
    // CSV logging
    // ------------------------------------------------------------------

    private void SaveResults()
    {
        string folder = Application.persistentDataPath;
        string path   = Path.Combine(folder,
            $"HazardStudy_S2_P{participantID}_" +
            $"{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("ParticipantID,TrialNumber,Condition,TrialWithinCondition," +
                      "ResponseTime_s,Responded");

        foreach (var r in _results)
            sb.AppendLine($"{r.ParticipantID},{r.TrialNumber},{r.Condition}," +
                          $"{r.TrialWithinCondition},{r.ResponseTime:F4},{r.Responded}");

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[Scenario2] Results saved to: {path}");
    }

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

    private void OnGUI()
    {
        if (_studyComplete)
        {
            GUI.Label(new Rect(10, 10, 400, 30), "Scenario 2 Complete. Thank you!");
            return;
        }

        if (_currentTrialIndex < _trialSequence.Count)
        {
            GUI.Label(new Rect(10, 10, 400, 25),
                $"[S2] Trial {_currentTrialIndex + 1} / {_trialSequence.Count}");
            GUI.Label(new Rect(10, 35, 400, 25),
                $"Condition: {_trialSequence[_currentTrialIndex]}");
            if (_waitingForResponse)
                GUI.Label(new Rect(10, 60, 400, 25),
                    $"Response time: {(Time.time - _trialStartTime):F2}s");
        }
    }
}
