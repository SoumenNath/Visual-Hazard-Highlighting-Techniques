using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Condition 2: Peripheral Halo Highlight
/// Creates its own Canvas and ring UI entirely in code.
/// Updated for VR compatibility using Screen Space Camera render mode.
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
        // Re-assign camera in case it changed (e.g. after XR rig conversion).
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Update canvas camera reference.
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
            _canvas.worldCamera = playerCamera;

        SetVisible(true);
    }

    public void Deactivate()
    {
        _isActive = false;
        SetVisible(false);
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        if (!_isActive || _ringImage == null || playerCamera == null) return;

        // Convert hazard world position to screen position using camera pixel dims.
        Vector3 screenPos = playerCamera.WorldToScreenPoint(transform.position);

        // Don't show if behind camera.
        if (screenPos.z < 0f)
        {
            _ringImage.color = Color.clear;
            return;
        }

        // Use camera pixel dimensions instead of Screen for VR compatibility.
        float hw = playerCamera.pixelWidth  * 0.5f;
        float hh = playerCamera.pixelHeight * 0.5f;
        _ringRT.anchoredPosition = new Vector2(screenPos.x - hw, screenPos.y - hh);

        // Scale ring based on distance — bigger when closer.
        float dist    = Vector3.Distance(playerCamera.transform.position, transform.position);
        float minDist = 3f;
        float maxDist = 40f;
        float minSize = ringDiameter;
        float maxSize = ringDiameter * 4f;

        float t        = 1f - Mathf.Clamp01((dist - minDist) / (maxDist - minDist));
        float ringSize = Mathf.Lerp(minSize, maxSize, Mathf.SmoothStep(0f, 1f, t));
        _ringRT.sizeDelta = new Vector2(ringSize, ringSize);

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

        _canvas            = _canvasGO.AddComponent<Canvas>();

        // Use Screen Space Camera for VR compatibility.
        // Screen Space Overlay does not work correctly in VR headsets.
        _canvas.renderMode  = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = playerCamera;
        _canvas.planeDistance = 0.5f;
        _canvas.sortingOrder  = 10;

        var scaler                    = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor            = 1f;
        scaler.referencePixelsPerUnit = 100f;

        _canvasGO.AddComponent<GraphicRaycaster>();
    }

    private void CreateRingImage()
    {
        _ringTexture = BuildRingTexture();

        var imageGO = new GameObject($"{name}_Ring");
        imageGO.transform.SetParent(_canvasGO.transform, false);

        _ringImage         = imageGO.AddComponent<RawImage>();
        _ringImage.texture = _ringTexture;
        _ringImage.color   = ringColour;

        _ringRT                  = _ringImage.rectTransform;
        _ringRT.anchorMin        = new Vector2(0.5f, 0.5f);
        _ringRT.anchorMax        = new Vector2(0.5f, 0.5f);
        _ringRT.pivot            = new Vector2(0.5f, 0.5f);
        _ringRT.sizeDelta        = new Vector2(ringDiameter, ringDiameter);
        _ringRT.anchoredPosition = Vector2.zero;
    }

    private Texture2D BuildRingTexture()
    {
        int   size    = textureResolution;
        var   tex     = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode  = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float   half   = size * 0.5f;
        float   outerR = half;
        float   innerR = half * (1f - ringThickness);
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