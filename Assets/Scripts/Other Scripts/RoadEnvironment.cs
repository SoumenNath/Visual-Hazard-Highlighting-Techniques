using UnityEngine;

/// <summary>
/// RoadEnvironment.cs
/// Procedurally generates a straight road with sidewalks, lane markings,
/// and simple roadside buildings so the participant has a sense of forward motion.
/// Attach to an empty GameObject called "Environment".
/// </summary>
public class RoadEnvironment : MonoBehaviour
{
    [Header("Road Settings")]
    public float roadWidth      = 10f;
    public float roadLength     = 200f;
    public float laneMarkWidth  = 0.3f;
    public float laneMarkLength = 3f;
    public float laneMarkGap    = 3f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material sidewalkMaterial;
    public Material buildingMaterial;
    public Material laneMarkMaterial;

    [Header("Buildings")]
    public int   buildingsPerSide = 10;
    public float buildingMinHeight = 4f;
    public float buildingMaxHeight = 12f;
    public float buildingSetback   = 8f;   // distance from road edge to building

    private void Start()
    {
        BuildRoad();
        BuildSidewalks();
        BuildLaneMarkings();
        BuildBuildings();
    }

    // ------------------------------------------------------------------

    private void BuildRoad()
    {
        var road = CreateQuad("Road",
            new Vector3(0f, 0f, roadLength * 0.5f),
            new Vector3(roadWidth, 1f, roadLength));
        ApplyMaterial(road, roadMaterial, Color.gray);
    }

    private void BuildSidewalks()
    {
        float sidewalkWidth = 2f;
        float xOffset = roadWidth * 0.5f + sidewalkWidth * 0.5f;

        CreateQuad("Sidewalk_L",
            new Vector3(-xOffset, 0.01f, roadLength * 0.5f),
            new Vector3(sidewalkWidth, 1f, roadLength),
            sidewalkMaterial, new Color(0.8f, 0.78f, 0.72f));

        CreateQuad("Sidewalk_R",
            new Vector3(xOffset, 0.01f, roadLength * 0.5f),
            new Vector3(sidewalkWidth, 1f, roadLength),
            sidewalkMaterial, new Color(0.8f, 0.78f, 0.72f));
    }

    private void BuildLaneMarkings()
    {
        float z = laneMarkLength * 0.5f;
        while (z < roadLength)
        {
            CreateQuad($"LaneMark_{z:F0}",
                new Vector3(0f, 0.02f, z),
                new Vector3(laneMarkWidth, 1f, laneMarkLength),
                laneMarkMaterial, Color.white);
            z += laneMarkLength + laneMarkGap;
        }
    }

    private void BuildBuildings()
    {
        float spacing    = roadLength / buildingsPerSide;
        float sideOffset = roadWidth * 0.5f + buildingSetback;

        for (int i = 0; i < buildingsPerSide; i++)
        {
            float z      = spacing * i + spacing * 0.5f;
            float height = Random.Range(buildingMinHeight, buildingMaxHeight);
            float width  = Random.Range(3f, 7f);
            float depth  = Random.Range(3f, 7f);

            // Left side
            CreateBox($"Building_L_{i}",
                new Vector3(-sideOffset, height * 0.5f, z),
                new Vector3(width, height, depth),
                buildingMaterial,
                // new Color(Random.Range(0.4f, 0.75f),
                //            Random.Range(0.4f, 0.75f),
                //            Random.Range(0.4f, 0.75f)));
                new Color(0.6f, 0.6f, 0.6f));  // neutral grey

            // Right side
            CreateBox($"Building_R_{i}",
                new Vector3(sideOffset, height * 0.5f, z),
                new Vector3(width, height, depth),
                buildingMaterial,
                // new Color(Random.Range(0.4f, 0.75f),
                //            Random.Range(0.4f, 0.75f),
                //            Random.Range(0.4f, 0.75f)));
                new Color(0.6f, 0.6f, 0.6f));  // neutral grey
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private GameObject CreateQuad(string goName, Vector3 position, Vector3 scale,
                                   Material mat = null, Color colour = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        go.transform.SetParent(transform, false);
        go.transform.position   = position;
        go.transform.localScale = new Vector3(scale.x, 0.05f, scale.z);
        Destroy(go.GetComponent<BoxCollider>());
        ApplyMaterial(go, mat, colour);
        return go;
    }

    private GameObject CreateBox(string goName, Vector3 position, Vector3 scale,
                                  Material mat = null, Color colour = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        go.transform.SetParent(transform, false);
        go.transform.position   = position;
        go.transform.localScale = scale;
        Destroy(go.GetComponent<BoxCollider>());
        ApplyMaterial(go, mat, colour);
        return go;
    }

    private void ApplyMaterial(GameObject go, Material mat, Color colour)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        if (mat != null)
        {
            r.material = mat;
        }
        else
        {
            // Create a simple URP Unlit material with the given colour.
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", colour);
            else m.color = colour;
            r.material = m;
        }
    }
}
