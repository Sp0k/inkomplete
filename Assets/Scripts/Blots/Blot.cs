using System.Collections.Generic;
using GameManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Blots
{
    struct SineParams
    {
        public float Rate;
        public float MinScale;
        public float MaxScale;

        public SineParams(float rate, float minScale, float maxScale)
        {
            Rate = rate;
            MinScale = minScale;
            MaxScale = maxScale;
        }
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class Blot : MonoBehaviour
    {
        [Header("Blot Movement")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        private Vector3 _previousPosition;

        [Header("Blot State")]
        public BlotState CurrentState { get; private set; } = BlotState.Idle;
        public bool Recruited { get; private set; } = false;

        [Header("Blot Appearance")]
        [Tooltip("List of sprites corresponding to each BlotState. Ensure the order matches the BlotState enum.")]
        [SerializeField] private Canvas _blotCanvas;
        [SerializeField] private List<Sprite> _blotSprites;
        [SerializeField] private List<BlotState> _blotStates;
        [SerializeField] private RectTransform _blotImageRect;
        [SerializeField] private float _flipThreshold = 0.001f;
        private Image _blotImage;
        private Dictionary<BlotState, SineParams> _sineAnimParams = new();

        #region Unity Functions

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _speed;

            if (_blotCanvas != null)
            {
                _blotImage = _blotCanvas.GetComponentInChildren<Image>();

                if (_blotImage != null && _blotImageRect == null)
                {
                    _blotImageRect = _blotImage.GetComponent<RectTransform>();
                }
            }

            _previousPosition = transform.position;
        }

        private void Start()
        {
            _sineAnimParams = new()
            {
                { BlotState.Idle, new SineParams(2f, 0.9f, 1.0f) },
                { BlotState.Moving, new SineParams(4f, 0.8f, 1.1f) }
            };
        }

        private void Update()
        {
            if (CurrentState == BlotState.Moving && !_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
            {
                CurrentState = BlotState.Idle;
            }

            UpdateBlotAppearance();
            ApplySinCurveAnimation();
        }

        private void LateUpdate()
        {
            HandleCanvasFlip();
            _previousPosition = transform.position;
        }

        #endregion

        #region Blot Functions

        public void InitializeBlot()
        {
            if (!Recruited)
            {
                CurrentState = BlotState.Lost;
            }
        }

        public void MoveBlot(Vector3 targetPosition)
        {
            if (!_navMeshAgent.isOnNavMesh)
            {
                Debug.LogWarning("Blot is not on the NavMesh.");
                return;
            }

            _navMeshAgent.SetDestination(targetPosition);
            CurrentState = BlotState.Moving;
        }

        public void RecruitBlot()
        {
            Recruited = true;
            CurrentState = BlotState.Idle;
            GameManager.Instance.PlayerBlots.Add(this);
        }

        private void UpdateBlotAppearance()
        {
            int stateIndex = _blotStates.IndexOf(CurrentState);
            if (stateIndex >= 0 && stateIndex < _blotSprites.Count)
            {
                Image blotImage = _blotCanvas.GetComponentInChildren<Image>();
                if (blotImage != null)
                {
                    blotImage.sprite = _blotSprites[stateIndex];
                }
            }
        }

        private void HandleCanvasFlip()
        {
            if (_blotImageRect == null)
            {
                return;
            }

            float xMovement = transform.position.x - _previousPosition.x;

            if (Mathf.Abs(xMovement) < _flipThreshold)
            {
                return;
            }

            Vector3 scale = _blotImageRect.localScale;

            if (xMovement > 0f)
            {
                scale.x = Mathf.Abs(scale.x);
            } else
            {
                scale.x = -Mathf.Abs(scale.x);
            }

            _blotImageRect.localScale = scale;
        }

        private void ApplySinCurveAnimation()
        {
            if (_blotImageRect == null)
            {
                return;
            }

            if (!_sineAnimParams.TryGetValue(CurrentState, out SineParams sineParams))
            {
                return;
            }

            float sinValue = Mathf.Sin(Time.time * sineParams.Rate);
            float normalizedSin = (sinValue + 1f) * 0.5f;

            float scaleValue = Mathf.Lerp(
                sineParams.MaxScale,
                sineParams.MinScale,
                normalizedSin
            );

            Vector3 currentScale = _blotImageRect.localScale;

            float xDirection = currentScale.x < 0f ? -1f : 1f;

            _blotImageRect.localScale = new Vector3(
                scaleValue * xDirection,
                currentScale.y,
                currentScale.z
            );
        }

        #endregion
    }
}