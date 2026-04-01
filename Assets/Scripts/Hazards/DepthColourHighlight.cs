using UnityEngine;

/// <summary>
/// Condition 3: Depth-Based Colour Highlight
///
/// Continuously remaps the hazard object's material colour as a function of
/// the Euclidean distance between the player camera and the hazard.
///
/// Colour ranges (configurable):
///   - Near  → "danger" colour (e.g. red / high saturation)
///   - Far   → "safe" colour  (e.g. cool blue / low saturation)
///   - In between → smooth interpolation
///
/// The script supports both standard lit materials (via _Color) and Unlit/URP
/// Lit shaders (via _BaseColor).  It also optionally modulates emission
/// intensity so the hazard glows more intensely the closer it is.
///
/// Attach to the hazard GameObject.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class DepthColourHighlight : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player / VR camera used to measure distance.")]
    public Camera playerCamera;

    [Header("Distance Range")]
    [Tooltip("Distance at which the hazard shows the 'near' (danger) colour.")]
    public float nearDistance = 8f;

    [Tooltip("Distance at which the hazard shows the 'far' (safe) colour.")]
    public float farDistance  = 40f;

    [Header("Colour Mapping")]
    [Tooltip("Colour displayed when the hazard is at or closer than nearDistance.")]
    public Color nearColour = new Color(0.8f, 0.0f, 0.0f, 1f);   // dark red when close

    [Tooltip("Colour displayed when the hazard is at or further than farDistance.")]
    public Color farColour  = new Color(1.0f, 0.9f, 0.9f, 1f); // bright pale pink-red when far
    
    [Header("Emission")]
    [Tooltip("Enable emission intensity modulation (requires emission on the material).")]
    public bool modulateEmission = true;

    [Tooltip("Maximum emission intensity (HDR multiplier) at nearDistance.")]
    public float maxEmissionIntensity = 2.5f;

    [Tooltip("Minimum emission intensity at farDistance.")]
    public float minEmissionIntensity = 0f;

    [Header("Update Rate")]
    [Tooltip("How many times per second to update the colour (0 = every frame).")]
    [Range(0, 60)]
    public int updatesPerSecond = 30;

    // -----------------------------------------------------------------------

    private Renderer  _renderer;
    private Material  _material;          // instance material (won't affect shared)
    private bool      _isActive;
    private Color     _originalColor;
    private Color     _originalEmission;
    private float     _updateInterval;
    private float     _nextUpdateTime;

    // Shader property IDs cached for performance.
    private static readonly int PropColor     = Shader.PropertyToID("_Color");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmission  = Shader.PropertyToID("_EmissionColor");

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        _renderer = GetComponent<Renderer>();
        _material = _renderer.material;
        
        // Explicitly ensure inactive on start
        _isActive = false;
    }
    private void OnDestroy()
    {
        // Restore original colours before destroying.
        RestoreOriginalColours();
        if (_material != null)
            Destroy(_material);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void Activate()
    {
        _isActive = true;
        if (modulateEmission)
            _material.EnableKeyword("_EMISSION");
    }

    public void Deactivate()
    {
        _isActive = false;
        // Reset to neutral grey when deactivated
        if (_material != null)
        {
            Color grey = new Color(0.85f, 0.85f, 0.85f, 1f);
            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", grey);
            else
                _material.color = grey;
        }
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        // Strict guard — do absolutely nothing unless explicitly activated
            if (!_isActive) return;

            float dist = Vector3.Distance(
                playerCamera.transform.position, 
                transform.position);

            float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
            t = Mathf.SmoothStep(0f, 1f, t);

            Color targetColour = Color.Lerp(nearColour, farColour, t);

            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", targetColour);
            else
                _material.color = targetColour;
    }

    // ------------------------------------------------------------------

    private void UpdateColour()
    {
        float dist = Vector3.Distance(playerCamera.transform.position, transform.position);

        // t = 0 at nearDistance, 1 at farDistance.
        float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
        t = Mathf.SmoothStep(0f, 1f, t);     // ease in/out for a natural feel

        Color targetColour = Color.Lerp(nearColour, farColour, t);
        ApplyColour(targetColour);

        if (modulateEmission)
        {
            float emissionIntensity = Mathf.Lerp(maxEmissionIntensity, minEmissionIntensity, t);
            // Emission colour = base colour * HDR intensity.
            Color emissionColour = targetColour * Mathf.Pow(2f, emissionIntensity);
            _material.SetColor(PropEmission, emissionColour);
        }
    }

    private void ApplyColour(Color colour)
    {
        // Try both standard and URP/HDRP base colour property names.
        if (_material.HasProperty(PropColor))
            _material.SetColor(PropColor, colour);

        if (_material.HasProperty(PropBaseColor))
            _material.SetColor(PropBaseColor, colour);
    }

    private void CacheOriginalColours()
    {
        if (_material.HasProperty(PropColor))
            _originalColor = _material.GetColor(PropColor);
        else if (_material.HasProperty(PropBaseColor))
            _originalColor = _material.GetColor(PropBaseColor);
        else
            _originalColor = Color.white;

        if (_material.HasProperty(PropEmission))
            _originalEmission = _material.GetColor(PropEmission);
        else
            _originalEmission = Color.black;
    }

    private void RestoreOriginalColours()
    {
        if (_material == null) return;
        ApplyColour(_originalColor);
        if (_material.HasProperty(PropEmission))
            _material.SetColor(PropEmission, _originalEmission);
    }

#if UNITY_EDITOR
    // Visualise near/far distances as gizmo spheres in the editor.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, nearDistance);
        Gizmos.color = new Color(0.1f, 0.4f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, farDistance);
    }
#endif
}
