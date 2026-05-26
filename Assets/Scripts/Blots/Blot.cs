using System.Collections;
using System.Collections.Generic;
using GameManagement;
using Interfaces;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

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
    public class Blot : MonoBehaviour, IInteractable
    {
        [Header("Blot Movement")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        private Vector3 _previousPosition;

        [Header("Blot State")]
        public BlotState CurrentState { get; private set; } = BlotState.Idle;
        public bool IsInteractive { get; private set; } = true;

        [Header("Blot Appearance")]
        [Tooltip("List of sprites corresponding to each BlotState. Ensure the order matches the BlotState enum.")]
        [SerializeField] private Transform _blotSpritePivot;
        [SerializeField] private SpriteRenderer _blotSpriteRenderer;
        [SerializeField] private List<Sprite> _blotSprites;
        [SerializeField] private List<BlotState> _blotStates;
        [SerializeField] private float _flipThreshold = 0.001f;
        [SerializeField] private quaternion _baseRendererRotation;
        private Dictionary<BlotState, SineParams> _sineAnimParams = new();

        [Header("Blot Sounds")]
        [SerializeField] private List<string> _sfxRefs;

        [Header("Carrying")]
        [SerializeField] private Transform _carryTransform;
        public Transform CarryTransform => _carryTransform;

        [Header("Attack Animation")]
        [SerializeField] private float _attackDuration = 0.45f;
        [SerializeField] private float _attackJumpHeight = 0.65f;
        [SerializeField] private float _attackForwardDistance = 0.45f;
        [SerializeField] private float _attackSquashAmount = 0.2f;
        [SerializeField] private string _attackSfxRef;

        private Coroutine _attackRoutine;
        private bool _isAttackAnimating = false;
        private Vector3 _basePivotLocalPosition;
        private Vector3 _basePivotLocalScale;

        #region Unity Functions

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _speed;

            if (_blotSpriteRenderer != null)
            {
                _baseRendererRotation = _blotSpriteRenderer.transform.localRotation;
            }

            _previousPosition = transform.position;

            if (_blotSpritePivot != null)
            {
                _basePivotLocalPosition = _blotSpritePivot.localPosition;
                _basePivotLocalScale = _blotSpritePivot.localScale;
            }
        }

        private void Start()
        {
            _sineAnimParams = new()
            {
                { BlotState.Idle, new SineParams(2f, 0.9f, 1.0f) },
                { BlotState.Moving, new SineParams(4f, 0.8f, 1.1f) },
                { BlotState.Lost, new SineParams(4f, -6f, 6f)}
            };
        }

        private void Update()
        {
            if (!_navMeshAgent.enabled)
                return;

            if (CurrentState == BlotState.Moving && !_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
            {
                CurrentState = BlotState.Idle;
            }

            UpdateBlotAppearance();

            if (!_isAttackAnimating)
            {
                ApplySinCurveAnimation();
            }

            Quaternion rotation = transform.localRotation;
            rotation = Quaternion.Euler(rotation.x, 0f, rotation.z);
            transform.localRotation = rotation;
        }

        private void LateUpdate()
        {
            HandleCanvasFlip();
            _previousPosition = transform.position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Blot"))
            {
                Physics.IgnoreCollision(collision.collider, gameObject.GetComponent<BoxCollider>());
            }
        }

        #endregion

        #region Blot Functions

        public void InitializeBlot()
        {
            if (IsInteractive)
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
            if (GameManager.Instance.PlayerBlots.Count == 1 || UnityEngine.Random.Range(0.0f, 1.0f) >= 0.45f)
            {
                int sfxIdx = (int)UnityEngine.Random.Range(0, _sfxRefs.Count);
                AudioManager.Instance.PlaySfx(_sfxRefs[sfxIdx], true, true);
            }
        }

        public void RecruitBlot()
        {
            IsInteractive = false;
            CurrentState = BlotState.Idle;
            _blotSpritePivot.transform.localRotation = _baseRendererRotation;
            GameManager.Instance.RegisterPlayerBlot(this);
        }

        private void UpdateBlotAppearance()
        {
            int stateIndex = _blotStates.IndexOf(CurrentState);
            if (stateIndex >= 0 && stateIndex < _blotSprites.Count)
            {
                if (_blotSpriteRenderer != null)
                {
                    _blotSpriteRenderer.sprite = _blotSprites[stateIndex];
                }
            }
        }

        private void HandleCanvasFlip()
        {
            if (_blotSpriteRenderer == null)
            {
                return;
            }

            float xMovement = transform.position.x - _previousPosition.x;

            if (CurrentState.Equals(BlotState.Moving))
            {
                _blotSpriteRenderer.flipX = xMovement < 0;
            }
        }

        private void ApplySinCurveAnimation()
        {
            if (_blotSpritePivot == null)
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

            if (CurrentState.Equals(BlotState.Lost))
            {
                HandleRotation(scaleValue);
            }
            else
            {
                HandleScaling(scaleValue);
            }

        }

        private void HandleRotation(float rotationValue)
        {
            _blotSpritePivot.transform.localRotation = _baseRendererRotation * Quaternion.Euler(0f, 0f, rotationValue);
        }

        private void HandleScaling(float scaleValue)
        {
            Vector3 currentScale = _blotSpritePivot.transform.localScale;

            float xDirection = currentScale.x < 0f ? -1f : 1f;

            _blotSpritePivot.transform.localScale = new Vector3(
                scaleValue * xDirection,
                currentScale.y,
                currentScale.z
            );
        }

        public Coroutine PlayAttackJump(Vector3 targetWorldPosition, float delay = 0f)
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
            }

            _attackRoutine = StartCoroutine(AttackJumpRoutine(targetWorldPosition, delay));
            return _attackRoutine;
        }

        private IEnumerator AttackJumpRoutine(Vector3 targetWorldPosition, float delay)
        {
            _isAttackAnimating = true;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (_blotSpritePivot == null)
            {
                _isAttackAnimating = false;
                _attackRoutine = null;
                yield break;
            }

            bool hasPlayedSfx = false;

            Vector3 worldDirection = targetWorldPosition - transform.position;
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude < 0.001f)
            {
                worldDirection = transform.forward;
            }

            worldDirection.Normalize();

            if (_blotSpriteRenderer != null && Mathf.Abs(worldDirection.x) > _flipThreshold)
            {
                _blotSpriteRenderer.flipX = worldDirection.x < 0f;
            }

            Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
            localDirection.y = 0f;

            if (localDirection.sqrMagnitude < 0.001f)
            {
                localDirection = Vector3.forward;
            }

            localDirection.Normalize();

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _attackDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                if (!hasPlayedSfx && t >= 0.5f)
                {
                    hasPlayedSfx = true;

                    AudioManager.Instance.PlaySfx(_attackSfxRef, false, true);
                }

                // Goes forward, up, then back down.
                float arc = Mathf.Sin(t * Mathf.PI);

                Vector3 forwardOffset = localDirection * (_attackForwardDistance * arc);
                Vector3 jumpOffset = Vector3.up * (_attackJumpHeight * arc);

                _blotSpritePivot.localPosition =
                    _basePivotLocalPosition + forwardOffset + jumpOffset;

                // Small squash/stretch effect.
                float squash = Mathf.Sin(t * Mathf.PI * 2f);
                float xScale = 1f + Mathf.Max(0f, squash) * _attackSquashAmount;
                float yScale = 1f - Mathf.Max(0f, squash) * _attackSquashAmount;

                _blotSpritePivot.localScale = new Vector3(
                    _basePivotLocalScale.x * xScale,
                    _basePivotLocalScale.y * yScale,
                    _basePivotLocalScale.z
                );

                yield return null;
            }

            _blotSpritePivot.localPosition = _basePivotLocalPosition;
            _blotSpritePivot.localScale = _basePivotLocalScale;
            _blotSpritePivot.localRotation = _baseRendererRotation;

            _isAttackAnimating = false;
            _attackRoutine = null;
        }

        #endregion

        #region Interaction

        public void Interact()
        {
            if (CurrentState.Equals(BlotState.Lost) && IsInteractive)
            {
                RecruitBlot();
                GameManager.Instance.UpdateBlotCount();
            }
        }

        public void GetConfused()
        {
            StartCoroutine(GetConfusedRoutine());
        }

        public IEnumerator GetConfusedRoutine()
        {
            CurrentState = BlotState.Confused;
            yield return new WaitForSeconds(1f);
            CurrentState = BlotState.Idle;
        }

        #endregion
    }
}