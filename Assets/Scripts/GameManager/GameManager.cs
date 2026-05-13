using Blots;
using Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Management")]
        public GameLevel CurrentLevel { get; set; }
        [SerializeField] private List<Blot> _blotsInLevel;
        [SerializeField] private List<Blot> _playerBlots;

        [Header("Input")]
        public IInteractable HighlightedObject { get; private set; }

        [Header("Group Movement")]
        [SerializeField] private float _targetPointRadiusMultiplier = 0.3f;
        [SerializeField] private float _minDistanceBetweenBlots = 0.75f;
        [SerializeField] private float _navMeshSampleDistance = 1.5f;
        [SerializeField] private int _maxAttemptsPerBlot = 30;

        public List<Blot> PlayerBlots => _playerBlots;

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();

                    if (_instance == null)
                    {
                        GameObject singletonObj = new GameObject("GameManager");
                        _instance = singletonObj.AddComponent<GameManager>();
                    }
                }

                return _instance;
            }
        }

        #region Unity Functions

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Blot Navigation

        public void MovePlayerBlots(Vector3 targetPosition)
        {
            if (_playerBlots.Count == 0)
            {
                return;
            }

            if (_playerBlots.Count == 1)
            {
                if (TryGetNavMeshPoint(targetPosition, out Vector3 singleDest))
                {
                    _playerBlots[0].MoveBlot(singleDest);
                }

                return;
            }

            float radius = Mathf.Max(
                _targetPointRadiusMultiplier * _playerBlots.Count,
                _minDistanceBetweenBlots
            );

            List<Vector3> destinations = GenerateSpacedDestinations(targetPosition, _playerBlots.Count, radius);

            for (int i = 0; i < _playerBlots.Count; i++)
            {
                if (i >= destinations.Count)
                {
                    break;
                }

                Blot blot = _playerBlots[i];

                blot.NavMeshAgent.stoppingDistance = 0f;
                blot.NavMeshAgent.avoidancePriority = Mathf.Clamp(50 + i, 0, 99);

                blot.MoveBlot(destinations[i]);
            }
        }

        private List<Vector3> GenerateSpacedDestinations(Vector3 center, int amount, float radius)
        {
            List<Vector3> destinations = new();

            for (int i = 0; i < amount; i++)
            {
                bool foundPosition = false;

                for (int attempt = 0; attempt < _maxAttemptsPerBlot; attempt++)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * radius;
                    Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                    if (!TryGetNavMeshPoint(candidate, out Vector3 navMeshPosition))
                    {
                        continue;
                    }

                    if (!IsFarEnoughFromOthers(navMeshPosition, destinations))
                    {
                        continue;
                    }

                    destinations.Add(navMeshPosition);
                    foundPosition = true;
                    break;
                }

                if (!foundPosition)
                {
                    // Fallback if random spacing fails
                    float angle = i * Mathf.PI * 2f / amount;
                    Vector3 fallback = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                    if (TryGetNavMeshPoint(fallback, out Vector3 navMeshFallback))
                    {
                        destinations.Add(navMeshFallback);
                    }
                }
            }

            return destinations;
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

        private bool IsFarEnoughFromOthers(Vector3 position, List<Vector3> others)
        {
            foreach (Vector3 other in others)
            {
                if (Vector3.Distance(position, other) < _minDistanceBetweenBlots)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    
        #region Level Management

        public void StartLevel()
        {
            _blotsInLevel.Clear();
            _playerBlots.Clear();

            _blotsInLevel.AddRange(CurrentLevel.BlotsInLevel);
            foreach (Blot blot in _blotsInLevel)
            {
                blot.InitializeBlot();
            }

            foreach (Blot blot in CurrentLevel.StartingBlots)
            {
                blot.RecruitBlot();
            }
        }

        #endregion

        #region Interaction

        public bool SetHighlightedObject(IInteractable interactable)
        {
            if (HighlightedObject != null) return false;

            HighlightedObject = interactable;
            return true;
        }

        public bool ClearHighlightedObject()
        {
            if (HighlightedObject == null) return false;

            HighlightedObject = null;
            return true;
        }

        #endregion
    }
}