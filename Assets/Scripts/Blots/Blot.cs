using UnityEngine;
using UnityEngine.AI;

namespace Blots
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Blot : MonoBehaviour
    {
        [SerializeField]
        private float _speed = 5f;
        [SerializeField]
        private bool _isMoving = false;
        [SerializeField]
        private Canvas _blotCanvas;
        [SerializeField]
        private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent => _navMeshAgent;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _speed;
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
    }
}