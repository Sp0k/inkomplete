using Blots;
using GameManagement;
using Interfaces;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RippleCanvas : MonoBehaviour, IInteractable
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static RippleCanvas Instance { get; private set; }

    public bool IsInteractive => true;

    // ── Inspector ──────────────────────────────────────────────────────────
    public PlayerControls PlayerControls;

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

    // ── Image related ──────────────────────────────────────────────────────
    private CanvasImage _selectedArt;
    private Texture2D _originalImage;
    private Texture2D _displayedImage;
    private float _opacity = 0f;
    private float _opacityStep = 0f;

    private struct RippleSource
    {
        public Vector2 origin;   // XZ position in mesh-local space
        public float birthTime;
    }

    private readonly List<RippleSource> _ripples = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildMesh();

        var col = gameObject.AddComponent<MeshCollider>();
        col.convex = true;
        col.isTrigger = true;
        col.sharedMesh = _mesh;

        _meshRenderer = GetComponent<MeshRenderer>();
        _selectedArt = canvasImages[Random.Range(0, canvasImages.Count)];

        _originalImage = _selectedArt.stages[0];
        _displayedImage = _originalImage;

        SetOpacity();
        _meshRenderer.material.mainTexture = _displayedImage;
    }

    private void Update()
    {
        PruneOldRipples();
        if (_ripples.Count > 0) DisplaceMesh();
    }

    private void StartAnimation()
    {
        PlayerControls.CanClick = false;

        var maxBlots = GameManager.Instance.TotalBlotsInLevel;

        var blotCount = GameManager.Instance.PlayerBlots.Count;
        _opacityStep = 1.0f / blotCount;

        float ratio = (float)blotCount / maxBlots;
        int imageIndex = (int)(ratio * 4);
        _originalImage = _selectedArt.stages[imageIndex > 3 ? 3 : imageIndex];
        _displayedImage = _originalImage;

        StartCoroutine(MoveObjectsToTarget(GameManager.Instance.PlayerBlots.Select(x => x.gameObject), transform, 1.5f, 0.5f, 3f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Blot>(out var blot))
        {
            other.gameObject.SetActive(false);

            Instance.Spawn(other.transform.position);

            _opacity += _opacityStep;
            SetOpacity();

            _meshRenderer.material.mainTexture = _displayedImage;
        }
    }

    private IEnumerator MoveObjectsToTarget(IEnumerable<GameObject> objects, Transform target, float lerpDuration, float minWait, float maxWait)
    {
        List<GameObject> remaining = new List<GameObject>(objects);

        while (remaining.Count > 0)
        {
            // Pick random object
            GameObject obj = remaining[Random.Range(0, remaining.Count)];
            remaining.Remove(obj);

            // Disable navmesh
            var nav = obj.GetComponent<NavMeshAgent>();
            if (nav != null)  
            { 
                nav.isStopped = true;
                nav.enabled = false; 
            }
            
            // Add rigidbody for collision
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true; 
            rb.useGravity = false;

            // Lerp to target
            Vector3 startPos = obj.transform.position;
            float elapsed = 0f;
            while (elapsed < lerpDuration)
            {
                elapsed += Time.deltaTime;
                obj.transform.position = Vector3.Lerp(startPos, target.position, elapsed / lerpDuration);
                yield return null;
            }
            obj.transform.position = target.position;

            // Wait random time before next
            if (remaining.Count > 0)
                yield return new WaitForSeconds(Random.Range(minWait, maxWait));
        }
    }

    public void Spawn(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        _ripples.Add(new RippleSource
        {
            origin = new Vector2(local.x, local.z),
            birthTime = Time.time
        });
    }

    public void Spawn(Vector2 canvasPosition) =>
        Spawn(transform.TransformPoint(new Vector3(canvasPosition.x, 0f, canvasPosition.y)));

    public void SpawnAtCenter() => Spawn(transform.position);

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
    }

    private void PruneOldRipples()
    {
        float now = Time.time;
        _ripples.RemoveAll(r => now - r.birthTime > maxRippleAge);
    }

    private void SetOpacity()
    {
        Texture2D copy = new Texture2D(_originalImage.width, _originalImage.height, _originalImage.format, false);
        Color[] pixels = _originalImage.GetPixels();

        if (_opacity > 1.0f) _opacity = 1.0f;
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.Lerp(Color.white, pixels[i], _opacity);
        copy.SetPixels(pixels);
        copy.Apply();

        _displayedImage = copy;
    }

    public void Interact()
    {
        Debug.Log("Start canvas");
        StartAnimation();
    }
}

[System.Serializable]
public class CanvasImage
{
    public List<Texture2D> stages;
}