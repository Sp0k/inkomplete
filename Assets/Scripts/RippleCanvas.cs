using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Generates a mesh rectangle and simulates spammable ripple effects using wave superposition.
/// Waves add together (constructive/destructive interference) just like real wave physics.
///
/// Usage:
///   RippleCanvas.Instance.Spawn(worldPosition);  // from anywhere
///   or get a reference and call ripple.Spawn(worldPosition);
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RippleCanvas : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static RippleCanvas Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Canvas Mesh")]
    [Tooltip("Width of the canvas in world units.")]
    public float width = 10f;

    [Tooltip("Height of the canvas in world units.")]
    public float height = 6f;

    [Tooltip("Number of vertex columns. Higher = smoother ripples, more expensive.")]
    public int columns = 80;

    [Tooltip("Number of vertex rows.")]
    public int rows = 50;

    public List<CanvasImage> canvasImages;

    public int BlotsTotal = 1;

    [Header("Ripple Physics")]
    [Tooltip("How tall the peak of a single ripple is.")]
    public float amplitude = 0.3f;

    [Tooltip("How fast each ripple ring expands outward (world units per second).")]
    public float speed = 4f;

    [Tooltip("How many full wave cycles fit in the visible ring at once (wavelength = speed/frequency).")]
    public float frequency = 3f;

    [Tooltip("How quickly a ripple's amplitude decays over distance.")]
    public float spatialDamping = 1.5f;

    [Tooltip("How quickly a ripple fades over time.")]
    public float timeDamping = 0.8f;

    [Tooltip("Ripples older than this (seconds) are removed.")]
    public float maxRippleAge = 4f;


    // ── Private state ──────────────────────────────────────────────────────
    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private Vector3[] _baseVertices;   // flat resting positions
    private Vector3[] _vertices;       // live displaced positions

    private struct RippleSource
    {
        public Vector2 origin;   // XZ position in mesh-local space
        public float birthTime;
    }

    private readonly List<RippleSource> _ripples = new();

    // ── Unity lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildMesh();

        var col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = _mesh;

        // var selectedImage = canvasImages[Random.Range(0, canvasImages.Count)];
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.material.mainTexture = canvasImages[0].stages[3]; // = selectedImage[3];
    }

    private void Update()
    {
        PruneOldRipples();
        if (_ripples.Count > 0) DisplaceMesh();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RippleCanvas.Instance.Spawn(hit.point);
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a ripple at the given world-space position.
    /// Safe to call every frame — waves superpose correctly.
    /// </summary>
    public void Spawn(Vector3 worldPosition)
    {
        // Convert to local XZ (the mesh lies in the XZ plane by default)
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        _ripples.Add(new RippleSource
        {
            origin = new Vector2(local.x, local.z),
            birthTime = Time.time
        });
    }

    /// <summary>Convenience overload — pass a 2D canvas coordinate directly.</summary>
    public void Spawn(Vector2 canvasPosition) =>
        Spawn(transform.TransformPoint(new Vector3(canvasPosition.x, 0f, canvasPosition.y)));

    /// <summary>Spawns a ripple at the centre of the canvas.</summary>
    public void SpawnAtCenter() => Spawn(transform.position);

    // ── Mesh construction ──────────────────────────────────────────────────
    private void BuildMesh()
    {
        _mesh = new Mesh { name = "RippleCanvas" };
        GetComponent<MeshFilter>().mesh = _mesh;

        int vertCount = columns * rows;
        _baseVertices = new Vector3[vertCount];
        _vertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        float dx = width / (columns - 1);
        float dz = height / (rows - 1);
        float ox = -width * 0.5f;
        float oz = -height * 0.5f;

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                int i = r * columns + c;
                float x = ox + c * dx;
                float z = oz + r * dz;
                _baseVertices[i] = new Vector3(x, 0f, z);
                _vertices[i] = _baseVertices[i];
                uvs[i] = new Vector2((float)c / (columns - 1), (float)r / (rows - 1));
            }

        // Build quads
        int quadCount = (columns - 1) * (rows - 1);
        var tris = new int[quadCount * 6];
        int t = 0;
        for (int r = 0; r < rows - 1; r++)
            for (int c = 0; c < columns - 1; c++)
            {
                int bl = r * columns + c;
                int br = bl + 1;
                int tl = bl + columns;
                int tr = tl + 1;
                tris[t++] = bl; tris[t++] = tl; tris[t++] = tr;
                tris[t++] = bl; tris[t++] = tr; tris[t++] = br;
            }

        _mesh.vertices = _vertices;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }

    // ── Per-frame displacement ─────────────────────────────────────────────
    private void DisplaceMesh()
    {
        float now = Time.time;

        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector2 vxz = new(_baseVertices[i].x, _baseVertices[i].z);
            float displacement = 0f;

            // Superpose every active ripple (wave addition = interference)
            foreach (var rip in _ripples)
            {
                float age = now - rip.birthTime;
                float dist = Vector2.Distance(vxz, rip.origin);

                // Leading edge of the ring hasn't reached this vertex yet
                float waveFront = speed * age;
                if (dist > waveFront + 1f) continue;   // +1 is soft lead-in tolerance

                // Spatial envelope: ring-shaped bell centred on the wave front
                float ringDist = dist - waveFront;                          // negative = behind front
                float envelope = Mathf.Exp(-spatialDamping * ringDist * ringDist * 0.5f);

                // Time decay
                float timeFade = Mathf.Exp(-timeDamping * age);

                // Oscillation
                float phase = (dist - waveFront) * frequency * Mathf.PI * 2f;
                float wave = Mathf.Sin(phase) * amplitude * envelope * timeFade;

                // Superposition: simply add (this gives constructive/destructive interference)
                displacement += wave;
            }

            _vertices[i] = _baseVertices[i] + new Vector3(0f, displacement, 0f);
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = _mesh;
    }

    // ── Housekeeping ───────────────────────────────────────────────────────
    private void PruneOldRipples()
    {
        float now = Time.time;
        _ripples.RemoveAll(r => now - r.birthTime > maxRippleAge);
    }
}

[System.Serializable]
public class CanvasImage
{
    public List<Texture2D> stages;
}