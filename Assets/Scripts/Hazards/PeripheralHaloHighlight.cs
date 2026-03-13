using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Condition 2: Peripheral Halo Highlight (Full Rewrite)
/// Creates its own Canvas and ring UI entirely in code.
/// No manual Canvas/RawImage setup required in the Inspector.
/// Attach to the hazard vehicle body.
/// </summary>
public class PeripheralHaloHighlight : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Ring Appearance")]
    public Color ringColour = new Color(1f, 0.2f, 0.1f, 1f);

    [Tooltip("Diameter of the ring in pixels.")]
    public float ringDiameter = 100f;

    [Tooltip("Thickness of the ring (0.1 = thin, 0.4 = thick).")]
    [Range(0.05f, 0.5f)]
    public float ringThickness = 0.25f;

    [Tooltip("How close to the screen edge the ring sits (0.7 = near centre, 0.95 = near edge).")]
    [Range(0.5f, 0.99f)]
    public float edgeProximity = 0.88f;

    [Tooltip("Pulse frequency in Hz (0 = no pulse).")]
    [Range(0f, 5f)]
    public float pulseFrequency = 1.5f;

    [Header("Texture")]
    public int textureResolution = 128;

    // ------------------------------------------------------------------

    private GameObject    _canvasGO;
    private Canvas        _canvas;
    private RawImage      _ringImage;
    private RectTransform _ringRT;
    private Texture2D     _ringTexture;
    private bool          _isActive;

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        CreateCanvas();
        CreateRingImage();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_canvasGO != null) Destroy(_canvasGO);
        if (_ringTexture != null) Destroy(_ringTexture);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void Activate()
    {
        _isActive = true;
        SetVisible(true);
    }

    public void Deactivate()
    {
        _isActive = false;
        SetVisible(false);
    }

    // ------------------------------------------------------------------

//    private void Update()
//     {
//         if (!_isActive || _ringImage == null) return;

//         // --- 1. Get viewport position of this hazard. ---
//         Vector3 vp       = playerCamera.WorldToViewportPoint(transform.position);
//         bool    isBehind = vp.z < 0f;

//         if (isBehind)
//         {
//             vp.x = 1f - vp.x;
//             vp.y = 1f - vp.y;
//         }

//         // --- 2. Direction from screen centre to hazard in viewport space. ---
//         Vector2 vpDir          = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
//         float   distFromCentre = vpDir.magnitude / 0.5f; // 0=centre, 1=edge

//         if (vpDir.magnitude > 0.001f)
//             vpDir.Normalize();
//         else
//             vpDir = Vector2.right;

//         // --- 3. Convert to screen pixels and place ring at edge. ---
//         // Clamp direction to screen rectangle edge, then pull in by edgeProximity.
//         float hw = Screen.width  * 0.5f;
//         float hh = Screen.height * 0.5f;

//         // Find the scale factor to reach the screen edge in this direction.
//         float scaleToEdge = Mathf.Min(
//             Mathf.Abs(vpDir.x) > 0.001f ? hw / Mathf.Abs(vpDir.x * hw) : float.MaxValue,
//             Mathf.Abs(vpDir.y) > 0.001f ? hh / Mathf.Abs(vpDir.y * hh) : float.MaxValue
//         );

//         Vector2 ringPos      = new Vector2(vpDir.x * hw, vpDir.y * hh)
//                             * edgeProximity;
//         _ringRT.anchoredPosition = ringPos;

//         // --- 4. Pulse + fade when hazard is near screen centre. ---
//         float pulse = (pulseFrequency > 0f)
//             ? Mathf.Lerp(0.3f, 1f,
//                 (Mathf.Sin(Time.time * pulseFrequency * 2f * Mathf.PI) + 1f) * 0.5f)
//             : 1f;

//         // Only show ring when hazard is peripheral (not when directly visible).
//         float vpX = vp.x;
//         bool isPeripheral = vpX < 0.45f || vpX > 0.55f;
//         float peripheralStrength = isPeripheral
//             ? Mathf.Clamp01((distFromCentre - 0.05f) * 2f)
//             : 0f;
//         float visibility = peripheralStrength;

//         Color c = ringColour;
//         c.a = pulse * visibility;
//         _ringImage.color = c;
//     }
    private void Update()
    {
        if (!_isActive || _ringImage == null) return;

        // Convert hazard world position to screen position.
        Vector3 screenPos = playerCamera.WorldToScreenPoint(transform.position);

        // Don't show if behind camera.
        if (screenPos.z < 0f)
        {
            _ringImage.color = Color.clear;
            return;
        }

        // Convert screen position to canvas anchored position.
        float hw = Screen.width  * 0.5f;
        float hh = Screen.height * 0.5f;
        _ringRT.anchoredPosition = new Vector2(screenPos.x - hw, screenPos.y - hh);

        // Pulse alpha.
        float pulse = (pulseFrequency > 0f)
            ? Mathf.Lerp(0.3f, 1f,
                (Mathf.Sin(Time.time * pulseFrequency * 2f * Mathf.PI) + 1f) * 0.5f)
            : 1f;

        Color c = ringColour;
        c.a = pulse;
        _ringImage.color = c;
    }
    // ------------------------------------------------------------------

    private void CreateCanvas()
    {
        _canvasGO = new GameObject($"{name}_HaloCanvas");
        // Do NOT parent to this transform — keep in world/screen space.

        _canvas                  = _canvasGO.AddComponent<Canvas>();
        _canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder     = 10;

        var scaler               = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ConstantPixelSize;

        _canvasGO.AddComponent<GraphicRaycaster>();
    }

    private void CreateRingImage()
    {
        // Build ring texture.
        _ringTexture = BuildRingTexture();

        // Create RawImage GameObject inside the canvas.
        var imageGO  = new GameObject($"{name}_Ring");
        imageGO.transform.SetParent(_canvasGO.transform, false);

        _ringImage         = imageGO.AddComponent<RawImage>();
        _ringImage.texture = _ringTexture;
        _ringImage.color   = ringColour;

        _ringRT                    = _ringImage.rectTransform;
        _ringRT.anchorMin          = new Vector2(0.5f, 0.5f);
        _ringRT.anchorMax          = new Vector2(0.5f, 0.5f);
        _ringRT.pivot              = new Vector2(0.5f, 0.5f);
        _ringRT.sizeDelta          = new Vector2(ringDiameter, ringDiameter);
        _ringRT.anchoredPosition   = Vector2.zero;
    }

    private Texture2D BuildRingTexture()
    {
        int     size    = textureResolution;
        var     tex     = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode    = TextureWrapMode.Clamp;
        tex.filterMode  = FilterMode.Bilinear;

        Color[] pixels  = new Color[size * size];
        float   half    = size * 0.5f;
        float   outerR  = half;
        float   innerR  = half * (1f - ringThickness);
        float   feather = half * 0.1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist       = Vector2.Distance(
                                       new Vector2(x + 0.5f, y + 0.5f),
                                       new Vector2(half, half));
                float outerAlpha = Mathf.InverseLerp(outerR, outerR - feather, dist);
                float innerAlpha = Mathf.InverseLerp(innerR, innerR + feather, dist);
                float alpha      = Mathf.SmoothStep(0f, 1f,
                                       Mathf.Min(outerAlpha, innerAlpha));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGO != null)
            _canvasGO.SetActive(visible);
    }
}