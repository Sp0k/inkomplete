using Blots;
using GameManagement;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class BridgeNode : MonoBehaviour
{
    public GameManager GameManager;
    public PlayerControls PlayerControls;

    public GameObject BridgeGeometry;
    public Transform BridgeEnd;
    private List<Transform> _blotPoints;

    public float DistanceForBlotToActivate = 1.5f;
    private float _activationDistSquared;
    public float DistanceForBlotToDeactivate = 0.5f;
    private float _deactivationDistSquared;

    public float CrossingSpeed = 10f;              // The time it takes for them to get to position
    public float DelayBetweenBridgeBlots = 0.3f; // The time delay between each blot in the bridge
    public float DelayBeforeCrossing = 1f;       // The time delay before the other blots start crossing
    public float DelayBetweenBlots = 0.3f;       // The time delay before each blot not in the bridge 

    private bool _isBridging;
    private bool _canStartCrossing;
    private bool _doneBridging;

    private List<Blot> _bridgeBlots = new();
    private List<Blot> _crossingBlots = new();
    private List<Blot> _crossedBlots = new();
    private float _originalY;

    void Start()
    {
        _activationDistSquared = DistanceForBlotToActivate * DistanceForBlotToActivate;
        _deactivationDistSquared = DistanceForBlotToDeactivate * DistanceForBlotToDeactivate;
        BridgeGeometry.SetActive(false);

        _blotPoints = BridgeGeometry.GetComponentsInChildren<Transform>()
            .Where(t => t != BridgeGeometry.transform)
            .OrderBy(t =>
                Mathf.Pow(transform.position.x - t.position.x, 2) +
                Mathf.Pow(transform.position.z - t.position.z, 2))
            .ToList();

        if (GameManager == null)
            GameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        // Get distance data
        var blotData = GameManager.PlayerBlots.Select(x => 
            new BlotBridgeData() { 
                Blot = x, 
                StartDistanceSquared = Mathf.Pow(transform.position.x - x.transform.position.x, 2) + Mathf.Pow(transform.position.z - x.transform.position.z, 2),
                EndDistanceSquared = Mathf.Pow(BridgeEnd.position.x - x.transform.position.x, 2) + Mathf.Pow(BridgeEnd.position.z - x.transform.position.z, 2),
            });

        // Bridge creation
        if (blotData.Count(x => x.StartDistanceSquared < _activationDistSquared) >= _blotPoints.Count)
        {
            // TODO: Trigger Bridge on input

            if (true && !_isBridging) 
            {
                _isBridging = true;

                PlayerControls.CanClick = false;
                BridgeGeometry.SetActive(true);

                var firstPointPos = _blotPoints[0].position;
                var closestBlots = GameManager.PlayerBlots
                    .OrderBy(x => 
                        Mathf.Pow(firstPointPos.x - x.transform.position.x, 2) + 
                        Mathf.Pow(firstPointPos.z - x.transform.position.z, 2))
                    .Take(_blotPoints.Count)
                    .ToList();
                _bridgeBlots = closestBlots;

                StartCoroutine(CreateBridge(closestBlots));
            }
        }

        // Bridge crossing
        if (_canStartCrossing)
        {
            var leftoverBlotData = blotData.Where(x => !_bridgeBlots.Contains(x.Blot) && !_crossingBlots.Contains(x.Blot)).ToArray();
            var blotsToCross = new List<Blot>();

            foreach (BlotBridgeData blot in leftoverBlotData)
            {
                if (blot.StartDistanceSquared >= _activationDistSquared)
                    blot.Blot.MoveBlot(transform.position);
                else 
                {
                    blotsToCross.Add(blot.Blot);
                    _crossingBlots.Add(blot.Blot);
                }
            }

            if (blotsToCross.Count > 0)
                StartCoroutine(CrossBridge(blotsToCross));
        }

        // Bridge creation
        if (_isBridging && !_doneBridging && _crossedBlots.Count + _bridgeBlots.Count == GameManager.PlayerBlots.Count)
        {
            StartCoroutine(DestroyBridge(_bridgeBlots));
            _doneBridging = true;
            PlayerControls.CanClick = true;
        }

        // Reset nav mesh and colliders
        foreach (BlotBridgeData blot in blotData.Where(x => !_crossedBlots.Contains(x.Blot)))
        {
            if (blot.EndDistanceSquared > _deactivationDistSquared)
                continue;

            var navAgent = blot.Blot.gameObject.GetComponent<NavMeshAgent>();
            navAgent.enabled = true;
            blot.Blot.gameObject.GetComponent<BoxCollider>().enabled = true;
            blot.Blot.MoveBlot(BridgeEnd.position);

            _crossedBlots.Add(blot.Blot);
        }
    }

    IEnumerator CrossBridge(List<Blot> blots)
    {
        foreach (Blot blot in blots)
        {
            var navAgent = blot.gameObject.GetComponent<NavMeshAgent>();
            navAgent.isStopped = true;
            navAgent.enabled = false;
            blot.gameObject.GetComponent<BoxCollider>().enabled = false;

            var blotTransform = blot.transform;
            var blotPath = new List<Vector3>() {
                    transform.position,
                    new Vector3(_blotPoints[0].position.x, blotTransform.position.y, _blotPoints[0].position.z),
                    new Vector3(BridgeEnd.position.x, blotTransform.position.y, BridgeEnd.position.z),
                };

            StartCoroutine(LerpBlotPath(blotTransform, blotPath));
            yield return new WaitForSeconds(DelayBetweenBlots);
        }
    }

    IEnumerator CreateBridge(List<Blot> bridgeBlots)
    {
        _originalY = bridgeBlots.First().transform.position.y;

        for (int i = 0; i < bridgeBlots.Count; i++)
        {
            var navAgent = bridgeBlots[i].gameObject.GetComponent<NavMeshAgent>();
            navAgent.isStopped = true;
            navAgent.enabled = false;
            bridgeBlots[i].gameObject.GetComponent<BoxCollider>().enabled = false;

            var blotTransform = bridgeBlots[i].transform;
            var blotPath = new List<Vector3>() { 
                transform.position, 
                new Vector3(_blotPoints[i].position.x, blotTransform.position.y, _blotPoints[i].position.z), 
                _blotPoints[i].position 
            };

            StartCoroutine(LerpBlotPath(blotTransform, blotPath));
            yield return new WaitForSeconds(DelayBetweenBridgeBlots);
        }

        yield return new WaitForSeconds(DelayBeforeCrossing);
        _canStartCrossing = true;
    }

    IEnumerator DestroyBridge(List<Blot> bridgeBlots)
    {
        var crossing = _crossingBlots.FirstOrDefault();
        var targetY = crossing ? crossing.transform.position.y : _originalY;

        for (int i = 0; i < bridgeBlots.Count; i++)
        {
            var blotTransform = bridgeBlots[i].transform;
            var blotPath = new List<Vector3>() {
                new Vector3(_blotPoints[i].position.x, targetY, _blotPoints[i].position.z),
                BridgeEnd.position
            };

            StartCoroutine(LerpBlotPath(blotTransform, blotPath));
            yield return new WaitForSeconds(DelayBetweenBridgeBlots);
        }

        yield return new WaitForSeconds(DelayBeforeCrossing);
    }

    IEnumerator LerpBlotPath(Transform blot, List<Vector3> positions)
    {
        positions.Insert(0, blot.position);
        float speed = 5f;

        for (int i = 0; i < positions.Count - 1; i++)
        {
            Vector3 start = positions[i];
            Vector3 end = positions[i + 1];
            float dist = Vector3.Distance(start, end);
            yield return Move(blot, start, end, dist / speed);
        }
    }

    IEnumerator Move(Transform blob, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            blob.position = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);

            blob.position = Vector3.Lerp(from, to, t);

            yield return null;
        }

        blob.position = to;
    }
}

struct BlotBridgeData
{
    public Blot Blot;
    public float StartDistanceSquared;
    public float EndDistanceSquared;
}