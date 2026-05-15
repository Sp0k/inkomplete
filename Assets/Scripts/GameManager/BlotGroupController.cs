using System.Collections.Generic;
using Blots;
using UnityEngine;
using UnityEngine.AI;

namespace GameManagement
{
    public class BlotGroupController : MonoBehaviour
    {
        [Header("Group Pivots")]
        [SerializeField] private Transform _leftPivot;
        [SerializeField] private Transform _rightPivot;

        [Header("Formation")]
        [SerializeField] private float _baseRadius = 0.75f;
        [SerializeField] private float _radiusGrowthPerBlot = 0.25f;
        [SerializeField] private float _minDistanceBetweenSlots = 0.75f;
        [SerializeField] private float _navMeshSampleDistance = 1.5f;
        [SerializeField] private int _maxAttemptsPerSlot = 60;

        private readonly List<Blot> _blots = new();
        private readonly Dictionary<Blot, Vector3> _localSlots = new();

        public Transform LeftPivot => _leftPivot;
        public Transform RightPivot => _rightPivot;
        public IReadOnlyList<Blot> Blots => _blots;

        private void Awake()
        {
            CreateMissingPivots();
        }

        public void Clear()
        {
            _blots.Clear();
            _localSlots.Clear();
        }

        public void AddBlot(Blot blot)
        {
            if (blot == null || _localSlots.ContainsKey(blot))
            {
                return;
            }

            if (_blots.Count == 0)
            {
                if (TryGetNavMeshPoint(blot.transform.position, out Vector3 centerPosition))
                {
                    transform.position = centerPosition;
                }
                else
                {
                    transform.position = blot.transform.position;
                }

                _blots.Add(blot);
                _localSlots.Add(blot, Vector3.zero);
                return;
            }

            Vector3 localSlot = GenerateFreeLocalSlot(_blots.Count + 1);

            _blots.Add(blot);
            _localSlots.Add(blot, localSlot);
        
            Vector3 worldTarget = transform.position + localSlot;
            if (TryGetNavMeshPoint(worldTarget, out Vector3 navMeshTarget))
            {
                blot.MoveBlot(navMeshTarget);
            }
        }

        public void MoveGroupTo(Vector3 targetCenter)
        {
            if (_blots.Count == 0)
            {
                return;
            }

            if (!TryGetNavMeshPoint(targetCenter, out Vector3 newCenter))
            {
                return;
            }

            Vector3 oldCenter = transform.position;

            Vector3 translation = newCenter - oldCenter;

            transform.position = newCenter;

            for (int i = 0; i < _blots.Count; i++)
            {
                Blot blot = _blots[i];

                if (blot == null || !_localSlots.TryGetValue(blot, out Vector3 localSlot))
                {
                    continue;
                }

                if (blot.NavMeshAgent == null || !blot.NavMeshAgent.isOnNavMesh)
                {
                    continue;
                }

                Vector3 previousSlotWorldPosition = oldCenter + localSlot;
                Vector3 translatedTarget = previousSlotWorldPosition + translation;

                if (!TryGetNavMeshPoint(translatedTarget, out Vector3 navMeshTarget))
                {
                    continue;
                }

                blot.NavMeshAgent.stoppingDistance = 0f;
                blot.NavMeshAgent.avoidancePriority = Mathf.Clamp(50 + i, 0, 99);

                blot.MoveBlot(navMeshTarget);
            }
        }

        private Vector3 GenerateFreeLocalSlot(int expectedBlotCount)
        {
            float radius = GetFormationRadius(expectedBlotCount);

            for (int attempt = 0; attempt < _maxAttemptsPerSlot; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 candidate = new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (!IsFarEnoughFromExistingSlots(candidate))
                {
                    continue;
                }

                Vector3 worldCandidate = transform.position + candidate;

                if (!TryGetNavMeshPoint(worldCandidate, out _))
                {
                    continue;
                }

                return candidate;
            }

            return GenerateFallbackSlot(expectedBlotCount, radius);
        }

        private Vector3 GenerateFallbackSlot(int expectedBlotCount, float radius)
        {
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

            for (int i = 0; i < expectedBlotCount * 8; i++)
            {
                float t = (i + 1f) / (expectedBlotCount * 8f);
                float slotRadius = Mathf.Sqrt(t) * radius;
                float angle = i * goldenAngle;

                Vector3 candidate = new Vector3(
                    Mathf.Cos(angle) * slotRadius,
                    0f,
                    Mathf.Sin(angle) * slotRadius
                );

                if (IsFarEnoughFromExistingSlots(candidate))
                {
                    return candidate;
                }
            }

            return Vector3.zero;
        }

        private float GetFormationRadius(int blotCount)
        {
            return _baseRadius + Mathf.Sqrt(Mathf.Max(0, blotCount - 1)) * _radiusGrowthPerBlot;
        }

        private bool IsFarEnoughFromExistingSlots(Vector3 candidate)
        {
            foreach (Vector3 existingSlot in _localSlots.Values)
            {
                if (Vector3.Distance(candidate, existingSlot) < _minDistanceBetweenSlots)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetNavMeshPoint(Vector3 position, out Vector3 result)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = position;
            return false;
        }

        private void CreateMissingPivots()
        {
            if (_leftPivot == null)
            {
                GameObject pull = new GameObject("Pull Pivot");
                pull.transform.SetParent(transform);
                pull.transform.localPosition = new Vector3(0f, 0f, 1f);
                _leftPivot = pull.transform;
            }

            if (_rightPivot == null)
            {
                GameObject push = new GameObject("Push Pivot");
                push.transform.SetParent(transform);
                push.transform.localPosition = new Vector3(0f, 0f, -1f);
                _rightPivot = push.transform;
            }
        }
    }
}