using UnityEngine;

public class DirectionalBeamHighlight : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Beam Appearance")]
    public Color beamColour = new Color(1f, 0.85f, 0.1f, 1f);

    [Tooltip("Width and height of the beam.")]
    public float beamWidth = 0.06f;

    [Tooltip("Length of the beam in world units.")]
    public float beamLength = 30f;

    [Header("Animation")]
    [Range(0f, 5f)]
    public float pulseSpeed = 1.5f;

    // ------------------------------------------------------------------

    private GameObject _beamCube;
    private Material _beamMaterial;
    private bool _isActive;

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        BuildBeam();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_beamCube != null) Destroy(_beamCube);
        if (_beamMaterial != null) Destroy(_beamMaterial);
    }

    // ------------------------------------------------------------------

    public void Activate()
    {
        _isActive = true;
        SetVisible(true);
        Debug.Log($"[DirectionalBeam] Activated on {gameObject.name} at {transform.position}");
    }

    public void Deactivate()
    {
        _isActive = false;
        SetVisible(false);
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        if (!_isActive || _beamCube == null) return;

        // Direction from hazard toward player.
        Vector3 dirToPlayer = (playerCamera.transform.position
                                - transform.position).normalized;

        // Centre the single cube halfway between hazard and beam end.
        Vector3 beamStart = transform.position + dirToPlayer * 0.5f;
        Vector3 beamEnd = transform.position + dirToPlayer * beamLength;
        Vector3 beamCentre = (beamStart + beamEnd) * 0.5f;
        float actualLength = Vector3.Distance(beamStart, beamEnd);

        _beamCube.transform.position = beamCentre;
        _beamCube.transform.rotation = Quaternion.LookRotation(dirToPlayer);
        _beamCube.transform.localScale = new Vector3(beamWidth, beamWidth, actualLength);

        // Pulse.
        float alpha = (pulseSpeed > 0f)
            ? Mathf.Lerp(0.3f, 1f,
                (Mathf.Sin(Time.time * pulseSpeed * 2f * Mathf.PI) + 1f) * 0.5f)
            : 1f;

        Color c = beamColour;
        c.a = alpha;
        if (_beamMaterial.HasProperty("_BaseColor"))
            _beamMaterial.SetColor("_BaseColor", c);
        else
            _beamMaterial.color = c;
    }

    // ------------------------------------------------------------------

    private void BuildBeam()
    {
        _beamCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _beamCube.name = $"{name}_Beam";
        _beamCube.transform.SetParent(transform, false);
        _beamCube.transform.localPosition = Vector3.zero;
        Destroy(_beamCube.GetComponent<BoxCollider>());

        _beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (_beamMaterial.HasProperty("_BaseColor"))
            _beamMaterial.SetColor("_BaseColor", beamColour);
        else
            _beamMaterial.color = beamColour;

        var r = _beamCube.GetComponent<Renderer>();
        r.material = _beamMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    private void SetVisible(bool visible)
    {
        if (_beamCube != null)
            _beamCube.SetActive(visible);
    }
}