using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// SequenceValidator.cs
/// Editor utility that generates and validates trial sequences for
/// multiple participants without running the simulation.
/// Access via Tools → Validate Trial Sequences in the Unity menu.
/// </summary>
public class SequenceValidator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Validate Trial Sequences")]
    public static void ValidateSequences()
    {
        int participantsToCheck = 12;  // check 3 full Latin square rotations
        int trialsPerCondition  = 10;
        int totalTrials         = 40;

        var conditions = new List<HazardHighlightManager.HighlightCondition>
        {
            HazardHighlightManager.HighlightCondition.ObjectOutline,
            HazardHighlightManager.HighlightCondition.PeripheralHalo,
            HazardHighlightManager.HighlightCondition.DepthColour,
            HazardHighlightManager.HighlightCondition.DirectionalBeam
        };

        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("  TRIAL SEQUENCE VALIDATION");
        sb.AppendLine($"  Checking {participantsToCheck} participants | " +
                      $"{totalTrials} trials each | {trialsPerCondition} per condition");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        bool allValid = true;

        for (int pID = 1; pID <= participantsToCheck; pID++)
        {
            var sequence = GenerateBalancedSequence(pID, trialsPerCondition, conditions);

            // --- Validation checks ---
            bool valid = true;
            var errors = new List<string>();

            // Check 1: Correct total length.
            if (sequence.Count != totalTrials)
            {
                errors.Add($"Wrong length: {sequence.Count} (expected {totalTrials})");
                valid = false;
            }

            // Check 2: Each condition appears exactly trialsPerCondition times.
            var counts = new Dictionary<HazardHighlightManager.HighlightCondition, int>();
            foreach (var c in conditions) counts[c] = 0;
            foreach (var t in sequence)
                if (counts.ContainsKey(t)) counts[t]++;
            foreach (var c in conditions)
            {
                if (counts[c] != trialsPerCondition)
                {
                    errors.Add($"{c} appears {counts[c]} times (expected {trialsPerCondition})");
                    valid = false;
                }
            }

            // Check 3: No condition appears more than 2 consecutive times.
            int maxRun = 1, currentRun = 1;
            for (int i = 1; i < sequence.Count; i++)
            {
                if (sequence[i] == sequence[i - 1])
                {
                    currentRun++;
                    if (currentRun > maxRun) maxRun = currentRun;
                    if (currentRun > 2)
                    {
                        errors.Add($"Run of {currentRun} consecutive " +
                                   $"{sequence[i]} starting at trial {i - currentRun + 2}");
                        valid = false;
                    }
                }
                else currentRun = 1;
            }

            // Check 4: Each block of 4 contains all 4 conditions exactly once.
            for (int b = 0; b < trialsPerCondition; b++)
            {
                var blockCounts = new Dictionary
                    <HazardHighlightManager.HighlightCondition, int>();
                foreach (var c in conditions) blockCounts[c] = 0;
                for (int i = b * 4; i < b * 4 + 4; i++)
                    blockCounts[sequence[i]]++;
                foreach (var c in conditions)
                {
                    if (blockCounts[c] != 1)
                    {
                        errors.Add($"Block {b + 1}: {c} appears {blockCounts[c]} times");
                        valid = false;
                    }
                }
            }

            if (!valid) allValid = false;

            // --- Print participant summary ---
            sb.AppendLine($"───────────────────────────────────────────────────────────────");
            sb.AppendLine($"  Participant {pID,2} | " +
                          $"{(valid ? "✓ VALID" : "✗ INVALID")} | " +
                          $"Max consecutive run: {maxRun}");

            // Print condition counts.
            sb.Append("  Counts: ");
            foreach (var c in conditions)
                sb.Append($"{c}={counts[c]}  ");
            sb.AppendLine();

            // Print first condition of each block to show ordering variety.
            sb.Append("  Block starts: ");
            for (int b = 0; b < trialsPerCondition; b++)
                sb.Append($"[{ShortName(sequence[b * 4])}] ");
            sb.AppendLine();

            // Print full sequence.
            sb.Append("  Full sequence: ");
            for (int i = 0; i < sequence.Count; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append("| ");
                sb.Append($"{ShortName(sequence[i])} ");
            }
            sb.AppendLine();

            // Print errors if any.
            if (errors.Count > 0)
            {
                sb.AppendLine("  ERRORS:");
                foreach (var e in errors)
                    sb.AppendLine($"    ✗ {e}");
            }
        }

        // --- Overall summary ---
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  RESULT: {(allValid ? "ALL SEQUENCES VALID ✓" : "ERRORS FOUND ✗")}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        Debug.Log(sb.ToString());

        // Also show a popup in the editor.
        EditorUtility.DisplayDialog(
            "Sequence Validation",
            allValid
                ? $"All {participantsToCheck} participant sequences are valid.\nSee Console for full details."
                : $"Errors found in sequences.\nSee Console for details.",
            "OK");
    }

    // ------------------------------------------------------------------
    // Replicated sequence generation logic (mirrors TrialControllerScenario2)
    // ------------------------------------------------------------------

    private static List<HazardHighlightManager.HighlightCondition> GenerateBalancedSequence(
        int pID, int trialsPerCondition,
        List<HazardHighlightManager.HighlightCondition> conditions)
    {
        int offset = (pID - 1) % conditions.Count;
        var orderedConditions = new List<HazardHighlightManager.HighlightCondition>();
        for (int i = 0; i < conditions.Count; i++)
            orderedConditions.Add(conditions[(i + offset) % conditions.Count]);

        var blocks = new List<List<HazardHighlightManager.HighlightCondition>>();
        for (int b = 0; b < trialsPerCondition; b++)
        {
            var block = new List<HazardHighlightManager.HighlightCondition>(orderedConditions);
            block = SeededShuffle(block, pID * 100 + b);
            blocks.Add(block);
        }

        var sequence = new List<HazardHighlightManager.HighlightCondition>();
        foreach (var block in blocks)
            foreach (var cond in block)
                sequence.Add(cond);

        return sequence;
    }

    private static List<HazardHighlightManager.HighlightCondition> SeededShuffle(
        List<HazardHighlightManager.HighlightCondition> input, int seed)
    {
        var result = new List<HazardHighlightManager.HighlightCondition>(input);
        var rng    = new System.Random(seed);
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    private static string ShortName(HazardHighlightManager.HighlightCondition c)
    {
        switch (c)
        {
            case HazardHighlightManager.HighlightCondition.ObjectOutline:   return "OL";
            case HazardHighlightManager.HighlightCondition.PeripheralHalo:  return "PH";
            case HazardHighlightManager.HighlightCondition.DepthColour:     return "DC";
            case HazardHighlightManager.HighlightCondition.DirectionalBeam: return "DB";
            default: return "??";
        }
    }
#endif
}
