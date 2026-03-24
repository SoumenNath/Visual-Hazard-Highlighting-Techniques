using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// OncomingVehicleSpawner.cs
/// Spawns 5 oncoming vehicles in the opposite lane travelling toward the player.
/// One vehicle is the hazard and will drift into the player's lane after a
/// random delay. All four highlight scripts are attached to every vehicle body
/// so HazardHighlightManager can activate the correct one.
///
/// Attach to an empty GameObject called "OncomingVehicleSpawner".
/// </summary>
public class OncomingVehicleSpawner : MonoBehaviour
{
    [Header("Vehicle Prefab")]
    [Tooltip("Assign your car prefab here in the Inspector.")]
    public GameObject vehiclePrefab;

    [Tooltip("Scale to apply to the prefab to match road size.")]
    public float vehicleScale = 1f;
    public float vehicleScaleX = 1f;
    public float vehicleScaleY = 1f;
    public float vehicleScaleZ = 1f;

    [Header("Spawn Settings")]
    [Tooltip("How far ahead of the player to spawn oncoming vehicles.")]
    public float spawnDistanceAhead = 50f;

    [Tooltip("X positions for oncoming lanes (should be negative side of road).")]
    public float[] oncomingLaneXPositions = new float[] { -3.5f, -2.5f, -1.5f, -0.5f, 0.5f };

    [Tooltip("Z spread between vehicles so they don't all appear side by side.")]
    public float zSpread = 6f;

    [Header("Vehicle Appearance")]
    public float vehicleLength = 4f;
    public float vehicleWidth  = 2f;
    public float vehicleHeight = 1.8f;

    [Header("Movement")]
    [Tooltip("Speed of oncoming vehicles toward the player.")]
    public float vehicleSpeed = 6f;

    [Header("Player Lane")]
    [Tooltip("X position of the player's lane centre (hazard drifts to this X).")]
    public float playerLaneX = 0f;

    // ------------------------------------------------------------------

    private List<GameObject>      _activeVehicles = new List<GameObject>();
    private List<OncomingVehicle> _vehicleControllers = new List<OncomingVehicle>();

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Spawns 5 oncoming vehicles. Returns the hazard vehicle body GameObject
    /// and the OncomingVehicle controller of the hazard.
    /// </summary>
    public (GameObject hazardBody, OncomingVehicle hazardController)
        SpawnVehicles(Vector3 playerPosition,
                      HazardHighlightManager.HighlightCondition condition)
    {
        ClearVehicles();

        int hazardIndex = Random.Range(0, oncomingLaneXPositions.Length);

        GameObject      hazardBody       = null;
        OncomingVehicle hazardController = null;

        // Shuffle lane indices.
        int[] laneOrder = ShuffledIndices(oncomingLaneXPositions.Length);

        for (int i = 0; i < 5; i++)
        {
            float x = oncomingLaneXPositions[laneOrder[i]];

            // Spread vehicles along Z so they approach at slightly different times.
            float z = playerPosition.z + spawnDistanceAhead
                      + Random.Range(-zSpread * 0.5f, zSpread * 0.5f)
                      + i * (zSpread * 0.3f);

            //float y = vehicleHeight * 0.5f + vehicleHeight * 0.5f;
            float y = 0f;

            bool isHazard = (i == hazardIndex);
            var (root, body, controller) = CreateVehicle(
                $"OncomingVehicle_{i}",
                new Vector3(x, y, z),
                isHazard);

            _activeVehicles.Add(root);
            _vehicleControllers.Add(controller);

            if (isHazard)
            {
                hazardBody       = body;
                hazardController = controller;
            }
        }

        return (hazardBody, hazardController);
    }

    /// <summary>Destroy all currently spawned oncoming vehicles.</summary>
    public void ClearVehicles()
    {
        foreach (var v in _activeVehicles)
            if (v != null) Destroy(v);
        _activeVehicles.Clear();
        _vehicleControllers.Clear();
    }

    // ------------------------------------------------------------------

    private (GameObject root, GameObject body, OncomingVehicle controller)
        CreateVehicle(string vehicleName, Vector3 position, bool isHazard)
    {
        // Root object.
        var root = new GameObject(vehicleName);
        root.transform.position = position;

        // Visible body as child cube — rotated 180° so it faces the player.
        // NEW
        GameObject body;
        if (vehiclePrefab != null)
        {
            body = Instantiate(vehiclePrefab);
            body.name = $"{vehicleName}_Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            //body.transform.localScale    = Vector3.one * vehicleScale;
            body.transform.localScale = new Vector3(vehicleScaleX, vehicleScaleY, vehicleScaleZ);

            // Remove any colliders on prefab or children.
            foreach (var col in body.GetComponentsInChildren<Collider>())
                Destroy(col);
        }
        else
        {
            // Fallback to cube if no prefab assigned.
            body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = $"{vehicleName}_Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(vehicleWidth, vehicleHeight, vehicleLength);
            Destroy(body.GetComponent<BoxCollider>());
        }

        // Add all four highlight scripts to the body.
        // body.AddComponent<ObjectOutlineHighlight>();
        // body.AddComponent<DepthColourHighlight>();
        // body.AddComponent<PeripheralHaloHighlight>();
        // body.AddComponent<DirectionalBeamHighlight>();
        GameObject meshObject = body.GetComponentInChildren<MeshRenderer>().gameObject;
        meshObject.AddComponent<ObjectOutlineHighlight>();
        meshObject.AddComponent<DepthColourHighlight>();
        meshObject.AddComponent<PeripheralHaloHighlight>();
        body.AddComponent<DirectionalBeamHighlight>();

        // Add movement controller to root.
        var controller = root.AddComponent<OncomingVehicle>();
        controller.speed       = vehicleSpeed;
        controller.playerLaneX = playerLaneX;
        controller.Initialise(isHazard, playerLaneX);

        return (root, body, controller);
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
