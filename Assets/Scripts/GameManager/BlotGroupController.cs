using System.Collections;
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

        [Header("Pickup Animation")]
        [SerializeField] private float _pickupApproachRadius = 1.25f;
        [SerializeField] private float _pickupApproachTimeout = 1.25f;
        [SerializeField] private float _pickupArriveDistance = 0.45f;
        [SerializeField] private float _pickupThrowDuration = 0.45f;
        [SerializeField] private float _pickupThrowHeight = 1.5f;

        private Coroutine _pickupRoutine;
        private bool _isPickupAnimating;

        private Rigidbody _attachedRigidbody;
        private bool _attachedRigidbodyWasKinematic;

        private readonly Dictionary<Blot, Vector3> _pickupTargets = new();

        [Header("Formation")]
        [SerializeField] private float _baseRadius = 1.25f;
        [SerializeField] private float _radiusGrowthPerBlot = 0.35f;
        [SerializeField] private float _minDistanceBetweenSlots = 1.15f;
        [SerializeField] private float _navMeshSampleDistance = 1.5f;
        [SerializeField] private int _maxAttemptsPerSlot = 60;

        private readonly List<Blot> _blots = new();
        private readonly Dictionary<Blot, Vector3> _localSlots = new();

        public IReadOnlyList<Blot> Blots => _blots;

        [Header("Attack Animation")]
        [SerializeField] private float _attackApproachRadius = 1.25f;
        [SerializeField] private float _attackApproachTimeout = 1.25f;
        [SerializeField] private float _attackArriveDistance = 0.5f;
        [SerializeField] private float _attackDuration = 0.5f;
        [SerializeField] private float _attackStagger = 0.06f;

        private Coroutine _attackRoutine;
        private bool _isAttackAnimating;
        private readonly Dictionary<Blot, Vector3> _attackTargets = new();

        [Header("Attack Positioning")]
        [SerializeField] private float _attackDistanceFromBlock = 0.5f;
        [SerializeField] private float _attackMinDistanceBetweenBlots = 0.9f;
        [SerializeField] private float _attackRingSpacing = 0.65f;
        [SerializeField] private int _attackMaxRings = 3;
        [SerializeField] private int _attackSpotAttemptsPerRing = 24;

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
            if (moveableObj == null || _attachedMoveable != null || _isPickupAnimating)
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

            _pickupRoutine = StartCoroutine(PickupMoveableRoutine(moveableObj, closestBlot));
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
            if (_pickupRoutine != null)
            {
                StopCoroutine(_pickupRoutine);
                _pickupRoutine = null;
            }

            _isPickupAnimating = false;
            _pickupTargets.Clear();

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

            if (_attachedRigidbody != null)
            {
                _attachedRigidbody.isKinematic = _attachedRigidbodyWasKinematic;
                _attachedRigidbody = null;
            }

            SetMoveableAlpha(_attachedMoveable, 1f);

            _attachedMoveable = null;
            _moveableOwner = null;

            if (movableObject != null)
            {
                movableObject.SetCarried(false);
            }
        }

        public bool HasAttachedMoveable()
        {
            return _attachedMoveable != null || _isPickupAnimating;
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

        private IEnumerator PickupMoveableRoutine(Transform moveableObj, Blot owner)
        {
            _isPickupAnimating = true;
            _moveableOwner = owner;

            MoveBlotsToPickupPositions(moveableObj.position, owner);

            float timer = 0f;

            while (timer < _pickupApproachTimeout)
            {
                if (moveableObj == null || owner == null)
                {
                    _isPickupAnimating = false;
                    yield break;
                }

                if (HaveBlotsReachedPickupPositions())
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            _attachedMoveable = moveableObj;
            _attachedRigidbody = moveableObj.GetComponent<Rigidbody>();

            if (_attachedRigidbody != null)
            {
                _attachedRigidbodyWasKinematic = _attachedRigidbody.isKinematic;
                _attachedRigidbody.isKinematic = true;
                _attachedRigidbody.linearVelocity = Vector3.zero;
                _attachedRigidbody.angularVelocity = Vector3.zero;
            }

            yield return ThrowMoveableToCarryTransform(moveableObj, owner);

            RecenterFormationAroundOwner(owner);

            _isPickupAnimating = false;
            _pickupRoutine = null;

            MovableObject movableObject = _attachedMoveable.GetComponent<MovableObject>();

            if (movableObject != null)
            {
                movableObject.SetCarried(true);
            }

            SetMoveableAlpha(_attachedMoveable, 0.5f);
        }

        private void MoveBlotsToPickupPositions(Vector3 moveablePosition, Blot owner)
        {
            _pickupTargets.Clear();

            int count = _blots.Count;

            if (count == 0)
            {
                return;
            }

            Vector3 ownerDirection = owner.transform.position - moveablePosition;
            ownerDirection.y = 0f;

            if (ownerDirection.sqrMagnitude < 0.001f)
            {
                ownerDirection = Vector3.back;
            }

            ownerDirection.Normalize();

            for (int i = 0; i < count; i++)
            {
                Blot blot = _blots[i];

                if (blot == null || blot.NavMeshAgent == null || !blot.NavMeshAgent.isOnNavMesh)
                {
                    continue;
                }

                Vector3 targetPosition;

                if (blot == owner)
                {
                    targetPosition = moveablePosition + ownerDirection * _pickupApproachRadius;
                }
                else
                {
                    float angle = (Mathf.PI * 2f * i) / count;

                    Vector3 ringOffset = new Vector3(
                        Mathf.Cos(angle) * _pickupApproachRadius,
                        0f,
                        Mathf.Sin(angle) * _pickupApproachRadius
                    );

                    targetPosition = moveablePosition + ringOffset;
                }

                if (!TryGetNavMeshPoint(targetPosition, out Vector3 navMeshTarget))
                {
                    continue;
                }

                _pickupTargets[blot] = navMeshTarget;

                blot.NavMeshAgent.stoppingDistance = 0.15f;
                blot.MoveBlot(navMeshTarget);
            }
        }

        private bool HaveBlotsReachedPickupPositions()
        {
            if (_pickupTargets.Count == 0)
            {
                return true;
            }

            foreach (KeyValuePair<Blot, Vector3> pair in _pickupTargets)
            {
                Blot blot = pair.Key;
                Vector3 target = pair.Value;

                if (blot == null || blot.NavMeshAgent == null)
                {
                    continue;
                }

                if (blot.NavMeshAgent.pathPending)
                {
                    return false;
                }

                float distance = Vector3.Distance(blot.transform.position, target);

                if (distance > _pickupArriveDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator ThrowMoveableToCarryTransform(Transform moveableObj, Blot owner)
        {
            Vector3 startPosition = moveableObj.position;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _pickupThrowDuration);

            while (elapsed < duration)
            {
                if (moveableObj == null || owner == null || owner.CarryTransform == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                // Smoothstep easing.
                float easedT = t * t * (3f - 2f * t);

                Vector3 endPosition = owner.CarryTransform.position;
                Vector3 basePosition = Vector3.Lerp(startPosition, endPosition, easedT);

                // Arc motion: starts low, rises, then lands.
                float arcHeight = Mathf.Sin(easedT * Mathf.PI) * _pickupThrowHeight;
                basePosition.y += arcHeight;

                if (_attachedRigidbody != null)
                {
                    _attachedRigidbody.MovePosition(basePosition);
                }
                else
                {
                    moveableObj.position = basePosition;
                }

                yield return null;
            }

            if (moveableObj != null && owner != null && owner.CarryTransform != null)
            {
                moveableObj.SetParent(owner.CarryTransform, false);
                moveableObj.localPosition = Vector3.zero;
                moveableObj.localRotation = Quaternion.identity;
            }
        }

        public bool TryPlaceAttachedMoveable(MovableObject movableObject, Transform pivot)
        {
            if (movableObject == null || pivot == null)
            {
                return false;
            }

            if (_isPickupAnimating)
            {
                return false;
            }

            if (_attachedMoveable != movableObject.transform)
            {
                return false;
            }

            if (_attachedRigidbody != null)
            {
                _attachedRigidbody.isKinematic = true;
                _attachedRigidbody.linearVelocity = Vector3.zero;
                _attachedRigidbody.angularVelocity = Vector3.zero;
                _attachedRigidbody = null;
            }

            movableObject.PlaceAtPivot(pivot);

            _attachedMoveable = null;
            _moveableOwner = null;
            _pickupTargets.Clear();

            return true;
        }

        private void SetMoveableAlpha(Transform moveableTransform, float alpha)
        {
            if (moveableTransform == null)
            {
                return;
            }

            MeshRenderer[] renderers = moveableTransform.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                Color col = renderer.material.color;
                col.a = alpha;
                renderer.material.color = col;
            }
        }

        public void AttackBreakableObject(BreakableObject breakableObject)
        {
            if (breakableObject == null)
            {
                return;
            }

            if (_isAttackAnimating || _isPickupAnimating)
            {
                return;
            }

            if (_blots.Count == 0)
            {
                return;
            }

            if (!breakableObject.TryBeginAttack())
            {
                return;
            }

            float dist = Vector3.Distance(breakableObject.transform.position, transform.position);
            if (dist > 4.5f)
            {
                Debug.LogWarning($"Distance {dist}");
                foreach(Blot blot in _blots)
                {
                    blot.GetConfused();
                }
                AudioManager.Instance.PlaySfx("blot_cry_2", false, false);
                breakableObject.CancelAttack();
                return;
            }

            _attackRoutine = StartCoroutine(AttackBreakableRoutine(breakableObject));
        }

        private IEnumerator AttackBreakableRoutine(BreakableObject breakableObject)
        {
            _isAttackAnimating = true;

            MoveBlotsToAttackPositions(breakableObject.transform.position);

            float timer = 0f;

            while (timer < _attackApproachTimeout)
            {
                if (breakableObject == null)
                {
                    _isAttackAnimating = false;
                    _attackRoutine = null;
                    yield break;
                }

                if (HaveBlotsReachedAttackPositions())
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            int attackingBlotCount = 0;

            for (int i = 0; i < _blots.Count; i++)
            {
                Blot blot = _blots[i];

                if (blot == null)
                {
                    continue;
                }

                attackingBlotCount++;

                float delay = i * _attackStagger;
                blot.PlayAttackJump(breakableObject.transform.position, delay);
            }

            float totalAttackTime = _attackDuration + (_attackStagger * Mathf.Max(0, attackingBlotCount - 1));
            yield return new WaitForSeconds(totalAttackTime);

            if (breakableObject != null)
            {
                breakableObject.ApplyDamage(attackingBlotCount);
            }

            _attackTargets.Clear();
            _isAttackAnimating = false;
            _attackRoutine = null;
        }

        private void MoveBlotsToAttackPositions(Vector3 breakablePosition)
        {
            _attackTargets.Clear();

            int count = _blots.Count;

            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Blot blot = _blots[i];

                if (blot == null || blot.NavMeshAgent == null || !blot.NavMeshAgent.isOnNavMesh)
                {
                    continue;
                }

                float angle = (Mathf.PI * 2f * i) / count;

                Vector3 ringOffset = new Vector3(
                    Mathf.Cos(angle) * _attackDistanceFromBlock,
                    0f,
                    Mathf.Sin(angle) * _attackDistanceFromBlock
                );

                Vector3 targetPosition = breakablePosition + ringOffset;

                if (!TryGetNavMeshPoint(targetPosition, out Vector3 navMeshTarget))
                {
                    continue;
                }

                _attackTargets[blot] = navMeshTarget;

                blot.NavMeshAgent.stoppingDistance = 0.15f;
                blot.MoveBlot(navMeshTarget);
            }
        }

        private bool HaveBlotsReachedAttackPositions()
        {
            if (_attackTargets.Count == 0)
            {
                return true;
            }

            foreach (KeyValuePair<Blot, Vector3> pair in _attackTargets)
            {
                Blot blot = pair.Key;
                Vector3 target = pair.Value;

                if (blot == null || blot.NavMeshAgent == null)
                {
                    continue;
                }

                if (blot.NavMeshAgent.pathPending)
                {
                    return false;
                }

                float distance = Vector3.Distance(blot.transform.position, target);

                if (distance > _attackArriveDistance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}