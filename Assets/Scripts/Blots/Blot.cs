using UnityEngine;
using UnityEngine.AI;

namespace Blots
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Blot : MonoBehaviour
    {
        [SerializeField] private float _speed = 2.5f;
        [SerializeField] private float _acceleration = 1f;

        [SerializeField] private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent => _navMeshAgent;

        [Header("Wind")]
        [SerializeField] private float _windPushStrength = 0.15f;
        [SerializeField] private float _minimumSpeedMultiplier = 0.35f;
        [SerializeField] private bool _inFanAreaOfEffect = false;

        private Vector3 _windPushDir = Vector3.zero;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _speed;
            _navMeshAgent.acceleration = _acceleration;
        }

        private void Update()
        {
            HandleMovementSlowdown();
        }

        private void LateUpdate()
        {
            HandleFanPush();
        }

        public void MoveBlot(Vector3 targetPosition)
        {
            if (!_navMeshAgent.isOnNavMesh)
            {
                Debug.LogWarning("Blot is not on the NavMesh.");
                return;
            }

            _navMeshAgent.SetDestination(targetPosition);
        }

        private void HandleMovementSlowdown()
        {
            if (!_inFanAreaOfEffect)
            {
                _navMeshAgent.speed = _speed;
                _navMeshAgent.acceleration = _acceleration;
                return;
            }

            Vector3 pushDir = _windPushDir;
            pushDir.y = 0f;

            if (pushDir.sqrMagnitude < 0.01f)
            {
                _navMeshAgent.speed = _speed;
                _navMeshAgent.acceleration = _acceleration;
                return;
            }

            pushDir.Normalize();

            Vector3 desiredMove = _navMeshAgent.desiredVelocity;
            desiredMove.y = 0f;

            float movingIntoWind = 0f;

            if (desiredMove.sqrMagnitude > 0.01f)
            {
                movingIntoWind = Mathf.Clamp01(Vector3.Dot(desiredMove.normalized, -pushDir));
            }

            float speedMultiplier = Mathf.Lerp(
                1f,
                _minimumSpeedMultiplier,
                movingIntoWind
            );

            _navMeshAgent.speed = _speed * speedMultiplier;
            _navMeshAgent.acceleration = _acceleration * speedMultiplier;
        }

        private void HandleFanPush()
        {
            if (!_inFanAreaOfEffect)
            {
                return;
            }

            Vector3 pushDir = _windPushDir;
            pushDir.y = 0f;

            if (pushDir.sqrMagnitude < 0.01f)
            {
                return;
            }

            pushDir.Normalize();

            if (HasReachedDestination())
            {
                _navMeshAgent.ResetPath();
            }

            Vector3 windOffset = pushDir * _windPushStrength * Time.deltaTime;
            _navMeshAgent.Move(windOffset);
        }

        public void ApplyFanWind(Vector3 windDir)
        {
            _inFanAreaOfEffect = true;

            _windPushDir = windDir;
            _windPushDir.y = 0f;

            if (_windPushDir.sqrMagnitude > 0.01f)
            {
                _windPushDir.Normalize();
            }
        }

        public void ClearFanWind()
        {
            _inFanAreaOfEffect = false;
            _windPushDir = Vector3.zero;

            _navMeshAgent.speed = _speed;
            _navMeshAgent.acceleration = _acceleration;
        }

        private bool HasReachedDestination()
        {
            if (!_navMeshAgent.isOnNavMesh)
            {
                return true;
            }

            if (_navMeshAgent.pathPending)
            {
                return false;
            }

            if (!_navMeshAgent.hasPath)
            {
                return true;
            }

            return _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.1f;
        }
    }
}