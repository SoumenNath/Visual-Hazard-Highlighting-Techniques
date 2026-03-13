using UnityEngine;

/// <summary>
/// HazardHighlightManager
///
/// Central controller for the within-subjects VR hazard study.
/// Attach all four highlight scripts to each hazard, then call
/// SetActiveCondition() to switch between them.
///
/// Usage:
///   manager.SetActiveCondition(HighlightCondition.ObjectOutline);
///   manager.ShowHazard(myHazardGO);
///   manager.HideHazard();
/// </summary>
public class HazardHighlightManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Conditions
    // ------------------------------------------------------------------

    public enum HighlightCondition
    {
        None            = 0,
        ObjectOutline   = 1,   // Condition 1
        PeripheralHalo  = 2,   // Condition 2
        DepthColour     = 3,   // Condition 3
        DirectionalBeam = 4    // Condition 4
    }

    // ------------------------------------------------------------------
    // Inspector
    // ------------------------------------------------------------------

    [Header("Current Condition")]
    public HighlightCondition activeCondition = HighlightCondition.None;

    [Header("Hazard Reference")]
    [Tooltip("The current hazard GameObject (with all four scripts attached).")]
    public GameObject currentHazard;

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Switch to a highlighting condition.
    /// Deactivates all others on the current hazard first.
    /// </summary>
    public void SetActiveCondition(HighlightCondition condition)
    {
        activeCondition = condition;
        if (currentHazard != null)
            ApplyCondition(currentHazard, condition);
    }

    /// <summary>
    /// Present a new hazard under the current condition.
    /// Call this at the start of each trial.
    /// </summary>
    public void ShowHazard(GameObject hazard)
    {
        if (currentHazard != null) HideHazard();
        currentHazard = hazard;
        hazard.SetActive(true);
        ApplyCondition(hazard, activeCondition);
    }

    /// <summary>Hide and deactivate all highlighting on the current hazard.</summary>
    public void HideHazard()
    {
        if (currentHazard == null) return;
        ApplyCondition(currentHazard, HighlightCondition.None);
        currentHazard.SetActive(false);
        currentHazard = null;
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    private void ApplyCondition(GameObject hazard, HighlightCondition condition)
    {
        // Deactivate everything first.
        var outline = hazard.GetComponent<ObjectOutlineHighlight>();
        var halo    = hazard.GetComponent<PeripheralHaloHighlight>();
        var depth   = hazard.GetComponent<DepthColourHighlight>();
        var beam    = hazard.GetComponent<DirectionalBeamHighlight>();

        outline?.Deactivate();
        halo?.Deactivate();
        depth?.Deactivate();
        beam?.Deactivate();

        // Activate the requested one.
        switch (condition)
        {
            case HighlightCondition.ObjectOutline:   outline?.Activate(); break;
            case HighlightCondition.PeripheralHalo:  halo?.Activate();    break;
            case HighlightCondition.DepthColour:     depth?.Activate();   break;
            case HighlightCondition.DirectionalBeam: beam?.Activate();    break;
            case HighlightCondition.None:
            default: break;
        }
    }

    // Allow condition to be set from the inspector at runtime.
    private void OnValidate()
    {
        if (Application.isPlaying && currentHazard != null)
            ApplyCondition(currentHazard, activeCondition);
    }
}
