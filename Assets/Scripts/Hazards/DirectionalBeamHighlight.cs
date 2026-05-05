using UnityEngine;
using System.Collections.Generic;

public class DirectionalBeamHighlight : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Beam Appearance")]
    public Color beamColour = new Color(1f, 0.15f, 0.15f, 1f);

    public float beamLength   = 30f;
    public int   segmentCount = 20;
    public float beamWidth    = 0.12f;

    // ------------------------------------------------------------------

    private List<GameObject> _segments  = new List<GameObject>();
    private List<Material>   _materials = new List<Material>();
    private bool             _isActive;

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        BuildSegments();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        foreach (var s in _segments)  if (s != null) Destroy(s);
        foreach (var m in _materials) if (m != null) Destroy(m);
    }

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

    private void Update()
    {
        if (!_isActive || _segments.Count == 0) return;

        Vector3 dirToPlayer = (playerCamera.transform.position
                               - transform.position).normalized;

        float segmentLength = beamLength / segmentCount;

        for (int i = 0; i < _segments.Count; i++)
        {
            float   distAlongBeam = (i + 0.5f) * segmentLength;
            Vector3 segPos        = transform.position
                                    + dirToPlayer * distAlongBeam;

            _segments[i].transform.position  = segPos;
            _segments[i].transform.rotation  = Quaternion.LookRotation(dirToPlayer);
            _segments[i].transform.localScale = new Vector3(
                beamWidth, beamWidth, segmentLength * 1.02f);

            // Fade alpha along beam — brightest at car end, transparent at player end.
            // No pulse — constant solid beam.
            float t = 1f - (float)i / segmentCount;
            Color c = beamColour;
            c.a     = t;

            if (_materials[i].HasProperty("_BaseColor"))
                _materials[i].SetColor("_BaseColor", c);
            else
                _materials[i].color = c;
        }
    }

    // ------------------------------------------------------------------

    private void BuildSegments()
    {
        float segmentLength = beamLength / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            var seg  = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"{name}_Segment_{i}";
            seg.transform.SetParent(transform, false);
            Destroy(seg.GetComponent<BoxCollider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", beamColour);
            else
                mat.color = beamColour;

            var r               = seg.GetComponent<Renderer>();
            r.material          = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;

            _segments.Add(seg);
            _materials.Add(mat);
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (var s in _segments)
            if (s != null) s.SetActive(visible);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;
        Gizmos.color = beamColour;
        Gizmos.DrawLine(transform.position,
            transform.position +
            (playerCamera.transform.position - transform.position).normalized
            * beamLength);
    }
#endif
}