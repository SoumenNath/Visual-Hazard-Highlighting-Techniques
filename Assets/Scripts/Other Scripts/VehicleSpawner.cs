using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VehicleSpawner.cs
/// Spawns 5 vehicles ahead of the player at the start of each trial.
/// One vehicle is randomly designated as the hazard and returned to the
/// TrialController for highlight activation.
///
/// Vehicles are simple coloured box primitives. Replace the CreateVehicle()
/// method with your own prefab instantiation if you have vehicle models.
///
/// Attach to an empty GameObject called "VehicleSpawner".
/// </summary>
public class VehicleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("How far ahead of the player to spawn vehicles.")]
    public float spawnDistanceAhead = 40f;

    [Tooltip("Lateral positions (X) for each lane.")]
    public float[] laneXPositions = new float[] { -3.5f, -1.5f, 0f, 1.5f, 3.5f };

    [Tooltip("How much random Z spread to add between vehicles.")]
    public float zSpread = 5f;

    [Header("Vehicle Appearance")]
    //Old
    // public float vehicleLength = 3f;
    // public float vehicleWidth  = 1.5f;
    // public float vehicleHeight = 1.5f;

    public float vehicleLength = 4f;
    public float vehicleWidth  = 2f;
    public float vehicleHeight = 1.8f;

    // Colours used for non-hazard vehicles.
    // private readonly Color[] _vehicleColours = new Color[]
    // {
    //     new Color(0.2f, 0.25f, 0.8f),   // blue
    //     new Color(0.15f, 0.55f, 0.2f),  // green
    //     new Color(0.8f, 0.75f, 0.1f),   // yellow
    //     new Color(0.5f, 0.5f, 0.5f),    // grey
    //     new Color(0.9f, 0.9f, 0.9f),    // white
    // };

    // Currently spawned vehicles.
    private List<GameObject> _activeVehicles = new List<GameObject>();

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Spawns 5 vehicles relative to the player's current position.
    /// Returns the GameObject designated as the hazard.
    /// </summary>
    public GameObject SpawnVehicles(Vector3 playerPosition,
    HazardHighlightManager.HighlightCondition condition =
    HazardHighlightManager.HighlightCondition.None)
    {
        ClearVehicles();

        GameObject hazardVehicle = null;

        // For PeripheralHalo, hazard must be in leftmost or rightmost lane.
        // For all other conditions, hazard can be in any lane.
        int hazardLaneIndex;
        if (condition == HazardHighlightManager.HighlightCondition.PeripheralHalo)
        {
            // 0 = leftmost lane, 4 = rightmost lane
            hazardLaneIndex = Random.value > 0.5f ? 0 : laneXPositions.Length - 1;
        }
        else
        {
            hazardLaneIndex = Random.Range(0, laneXPositions.Length);
        }

        // Build list of non-hazard lane indices.
        List<int> otherLanes = new List<int>();
        for (int i = 0; i < laneXPositions.Length; i++)
            if (i != hazardLaneIndex)
                otherLanes.Add(i);

        // Shuffle non-hazard lanes.
        for (int i = otherLanes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = otherLanes[i];
            otherLanes[i] = otherLanes[j];
            otherLanes[j] = temp;
        }

        // Pick which vehicle slot (0-4) is the hazard.
        int hazardSlot = Random.Range(0, 5);
        int otherLaneIndex = 0;

        for (int i = 0; i < 5; i++)
        {
            // Assign lane: hazard gets hazardLaneIndex, others get shuffled remaining lanes.
            int laneIndex = (i == hazardSlot) ? hazardLaneIndex : otherLanes[otherLaneIndex++];
            float x = laneXPositions[laneIndex];
            float z = playerPosition.z + spawnDistanceAhead
                    + Random.Range(-zSpread, zSpread);
            float y = vehicleHeight;

            bool isHazard = (i == hazardSlot);
            GameObject vehicle = CreateVehicle($"Vehicle_{i}", new Vector3(x, y, z), isHazard);
            _activeVehicles.Add(vehicle);

            if (isHazard)
                hazardVehicle = vehicle;
        }

        return hazardVehicle;
    }

    /// <summary>Destroy all currently spawned vehicles.</summary>
    public void ClearVehicles()
    {
        foreach (var v in _activeVehicles)
            if (v != null) Destroy(v);
        _activeVehicles.Clear();
    }

    // ------------------------------------------------------------------

    private GameObject CreateVehicle(string vehicleName, Vector3 position, bool isHazard)
    {
        // Root object (acts as the "Hazard" parent).
        var root = new GameObject(vehicleName);
        root.transform.position = position;

        // Visible body as child cube.
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = $"{vehicleName}_Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(vehicleWidth, vehicleHeight, vehicleLength);

        // Remove collider — not needed for this study.
        Destroy(body.GetComponent<BoxCollider>());

        // Assign colour.
        var r = body.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color c = new Color(0.85f, 0.85f, 0.85f); 
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else mat.color = c;
        r.material = mat;

        // Add all four highlight scripts to the body (same pattern as your Cube setup).
        body.AddComponent<ObjectOutlineHighlight>();
        body.AddComponent<DepthColourHighlight>();
        body.AddComponent<PeripheralHaloHighlight>();
        body.AddComponent<DirectionalBeamHighlight>();

        return root;
    }

    private int[] ShuffledIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}
