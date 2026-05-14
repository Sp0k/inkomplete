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

    public NavMeshSurface surface;
    public GameObject bridgeGeometry;
    private List<Transform> _blotPoints;

    public float DistanceForBlotToActivate;

    public float moveDuration = 1f;
    public float delayBetweenStarts = 0.3f;

    private float _distSquared;
    private bool _isBridging;

    void Start()
    {
        _distSquared = DistanceForBlotToActivate * DistanceForBlotToActivate;
        bridgeGeometry.SetActive(false);

        _blotPoints = bridgeGeometry.GetComponentsInChildren<Transform>()
            .Where(t => t != bridgeGeometry.transform)
            .OrderBy(t =>
                Mathf.Pow(transform.position.x - t.position.x, 2) +
                Mathf.Pow(transform.position.z - t.position.z, 2))
            .ToList();

        if (GameManager == null)
            GameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        var distsSqrd = GameManager.PlayerBlots.Select(x => (x, Mathf.Pow(transform.position.x - x.transform.position.x, 2) + Mathf.Pow(transform.position.z - x.transform.position.z, 2)));

        if (distsSqrd.Count(x => x.Item2 < _distSquared) >= _blotPoints.Count)
        {
            // TODO: Trigger Bridge on input

            if (true && !_isBridging) 
            {
                _isBridging = true;

                PlayerControls.CanClick = false;
                bridgeGeometry.SetActive(true);

                var firstPointPos = _blotPoints[0].position;
                var closestBlots = GameManager.PlayerBlots
                    .OrderBy(x => 
                        Mathf.Pow(firstPointPos.x - x.transform.position.x, 2) + 
                        Mathf.Pow(firstPointPos.z - x.transform.position.z, 2))
                    .Take(_blotPoints.Count)
                    .ToList();

                StartCoroutine(MoveBlots(closestBlots));
            }
        }
    }
    IEnumerator MoveBlots(List<Blot> blobs)
    {
        for (int i = 0; i < blobs.Count; i++)
        {
            StartCoroutine(LerpBlob(blobs[i].gameObject.GetComponentInChildren<Image>().transform, _blotPoints[i].position));
            yield return new WaitForSeconds(delayBetweenStarts);
        }
    }

    IEnumerator LerpBlob(Transform blob, Vector3 targetPos)
    {
        float speed = 5f; // units per second

        Vector3 startPos = blob.position;

        // Mid point
        Vector3 midTarget = transform.position;
        midTarget.y = startPos.y;

        // Flat target
        Vector3 flatTarget = targetPos;
        flatTarget.y = startPos.y;

        // Distances
        float dist1 = Vector3.Distance(startPos, midTarget);
        float dist2 = Vector3.Distance(midTarget, flatTarget);
        float dist3 = Vector3.Distance(flatTarget, targetPos);

        yield return Move(blob, startPos, midTarget, dist1 / speed);
        yield return Move(blob, midTarget, flatTarget, dist2 / speed);
        yield return Move(blob, flatTarget, targetPos, dist3 / speed);
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
