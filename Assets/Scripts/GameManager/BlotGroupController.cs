using System.Collections.Generic;
using Blots;
using Puzzle.PhysicsBased;
using UnityEngine;
using UnityEngine.AI;

namespace GameManagement
{
    public class BlotGroupController : MonoBehaviour
    {
        [Header("Carry Pivot")]
        private Transform _attachedMoveable;
        private Blot _moveableOwner;

        [Header("Formation")]
        [SerializeField] private float _baseRadius = 1.25f;
        [SerializeField] private float _radiusGrowthPerBlot = 0.35f;
        [SerializeField] private float _minDistanceBetweenSlots = 1.15f;
        [SerializeField] private float _navMeshSampleDistance = 1.5f;
        [SerializeField] private int _maxAttemptsPerSlot = 60;

        private readonly List<Blot> _blots = new();
        private readonly Dictionary<Blot, Vector3> _localSlots = new();

        public IReadOnlyList<Blot> Blots => _blots;

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

                Vector3 worldTarget = newCenter + localSlot;

                if (!TryGetNavMeshPoint(worldTarget, out Vector3 navMeshTarget))
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

        public void AssignMoveableToPivot(Transform moveableObj)
        {
            if (moveableObj == null || _attachedMoveable != null)
            {
                return;
            }

            Blot closestBlot = GetClosestBlotTo(moveableObj.position);

            if (closestBlot == null)
            {
                Debug.LogWarning("No blot found to carry the movable object.");
                return;
            }

            if (closestBlot.CarryTransform == null)
            {
                Debug.LogWarning($"{closestBlot.name} has no CarryTransform assigned.");
                return;
            }

            _attachedMoveable = moveableObj;
            _moveableOwner = closestBlot;

            RecenterFormationAroundOwner(closestBlot);

            moveableObj.SetParent(closestBlot.CarryTransform, false);
            moveableObj.localPosition = Vector3.zero;
        }

        private Blot GetClosestBlotTo(Vector3 worldPosition)
        {
            Blot closestBlot = null;
            float shortestDistanceSqr = float.MaxValue;

            foreach (Blot blot in _blots)
            {
                if (blot == null)
                {
                    continue;
                }

                float distanceSqr = (blot.transform.position - worldPosition).sqrMagnitude;

                if (distanceSqr < shortestDistanceSqr)
                {
                    shortestDistanceSqr = distanceSqr;
                    closestBlot = blot;
                }
            }

            return closestBlot;
        }

        public void ClearMoveable()
        {
            if (_attachedMoveable == null)
            {
                return;
            }

            MovableObject movableObject = _attachedMoveable.GetComponent<MovableObject>();

            if (movableObject != null && movableObject.PuzzleGroup != null)
            {
                _attachedMoveable.SetParent(movableObject.PuzzleGroup, true);
            }
            else
            {
                _attachedMoveable.SetParent(null, true);
            }

            _attachedMoveable = null;
            _moveableOwner = null;
        }

        public bool HasAttachedMoveable()
        {
            return _attachedMoveable != null;
        }

        private void RecenterFormationAroundOwner(Blot owner)
        {
            if (owner == null || !_localSlots.TryGetValue(owner, out Vector3 ownerSlot))
            {
                return;
            }

            foreach (Blot blot in _blots)
            {
                if (blot == null || !_localSlots.ContainsKey(blot))
                {
                    continue;
                }

                _localSlots[blot] -= ownerSlot;
            }

            _localSlots[owner] = Vector3.zero;

            // The group transform now represents the carrying blot's position.
            transform.position = owner.transform.position;
        }
    }
}