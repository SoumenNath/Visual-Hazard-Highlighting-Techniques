using UnityEngine;

/// <summary>
/// Condition 1: Central Object Outline Highlight
/// Renders a bright, coloured outline directly around the hazard object
/// using a second pass (stencil-based) outline shader applied at runtime.
/// Attach to any GameObject that acts as a hazard.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ObjectOutlineHighlight : MonoBehaviour
{
    [Header("Outline Settings")]
    [Tooltip("Colour of the outline.")]
    public Color outlineColour = Color.red;

    [Tooltip("Thickness of the outline in local scale units.")]
    [Range(0.001f, 0.1f)]
    public float outlineWidth = 0.9f;

    [Tooltip("How quickly the outline pulses (0 = no pulse).")]
    [Range(0f, 5f)]
    public float pulseSpeed = 2f;

    [Tooltip("Minimum alpha multiplier during pulse.")]
    [Range(0f, 1f)]
    public float pulseMinAlpha = 0.4f;

    // -----------------------------------------------------------------------
    // The outline is achieved by duplicating the mesh, scaling it slightly
    // outward, and rendering it with front-face culling so only the 'shell'
    // is visible.  This is the classic two-pass outline technique that works
    // without a custom pipeline.
    // -----------------------------------------------------------------------

    private GameObject _outlineObject;
    private Material   _outlineMaterial;
    private bool       _isActive;

    // ------------------------------------------------------------------
    // Shader source (written inline so no external asset is required).
    // Compiled at runtime via Shader.Find fallback or ShaderUtil if present.
    // ------------------------------------------------------------------
    private const string OutlineShaderName = "Hidden/HazardOutline";

    private static readonly string OutlineShaderSource = @"
Shader ""Hidden/HazardOutline""
{
    Properties
    {
        _OutlineColor (""Outline Color"", Color) = (1,0,0,1)
        _OutlineWidth (""Outline Width"", Float) = 0.02
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent+1"" ""RenderType""=""Transparent"" }

        Pass
        {
            Name ""OUTLINE""
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            float4 _OutlineColor;
            float  _OutlineWidth;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f    { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm   = normalize(v.normal);
                float3 offset = norm * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + offset, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}";

    // ------------------------------------------------------------------

    private void Awake()
    {
        BuildOutlineObject();
    }

    private void OnDestroy()
    {
        if (_outlineObject != null) Destroy(_outlineObject);
        if (_outlineMaterial != null) Destroy(_outlineMaterial);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Show the outline on this hazard.</summary>
    public void Activate()
    {
        _isActive = true;
        if (_outlineObject != null)
            _outlineObject.SetActive(true);
    }

    /// <summary>Hide the outline.</summary>
    public void Deactivate()
    {
        _isActive = false;
        if (_outlineObject != null)
            _outlineObject.SetActive(false);
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        if (!_isActive || _outlineMaterial == null) return;

        if (pulseSpeed > 0f)
        {
            float alpha = Mathf.Lerp(pulseMinAlpha, 1f,
                (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
            Color c = outlineColour;
            c.a = alpha;
            _outlineMaterial.SetColor("_OutlineColor", c);
        }
    }

    // ------------------------------------------------------------------

    private void BuildOutlineObject()
    {
        _outlineObject = new GameObject($"{name}_Outline");
        _outlineObject.transform.SetParent(transform, false);
        _outlineObject.transform.localPosition = Vector3.zero;
        _outlineObject.transform.localRotation = Quaternion.identity;

        // Use a fixed world-space offset instead of a scale multiplier
        // so the outline thickness is consistent regardless of object size.
        Vector3 parentScale = transform.localScale;
        _outlineObject.transform.localScale = new Vector3(
            1f + outlineWidth / parentScale.x,
            1f + outlineWidth / parentScale.y,
            1f + outlineWidth / parentScale.z
        );

        var srcFilter = GetComponentInChildren<MeshFilter>();
        if (srcFilter != null)
        {
            var mf = _outlineObject.AddComponent<MeshFilter>();
            mf.sharedMesh = srcFilter.sharedMesh;
            var mr = _outlineObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.material = GetOrCreateOutlineMaterial();
        }

        _outlineObject.SetActive(false);
    }

    private Material GetOrCreateOutlineMaterial()
    {
        Shader shader = Shader.Find(OutlineShaderName);

        // If the shader isn't compiled into the build yet, create a simple
        // Unlit stand-in so the script doesn't break.
        if (shader == null)
        {
            Debug.LogWarning(
                "[ObjectOutlineHighlight] Outline shader not found. " +
                "Add 'Hidden/HazardOutline' to your Always Included Shaders, " +
                "or place the ShaderSource string into a .shader file in your project.");
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        _outlineMaterial = new Material(shader);
        _outlineMaterial.SetColor("_OutlineColor", outlineColour);
        _outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        return _outlineMaterial;
    }
}
