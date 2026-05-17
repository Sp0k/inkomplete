using Blots;
using GameManagement;
using Interfaces;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Player
{
    public class GroundTargetIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _camera;

        [Header("Input")]
        [SerializeField] private InputActionReference _moveIndicatorAction;

        [Header("Movement")]
        [SerializeField] private float _controllerMoveSpeed = 5f;
        [SerializeField] private float _mouseMoveSensitivity = 0.015f;

        [Header("Camera Bounds")]
        [SerializeField, Range(0f, 0.45f)] private float _viewportMargin = 0.08f;
        [SerializeField] private float _cameraClampRayDistance = 500f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _groundOffset = 0.02f;
        [SerializeField] private float _groundProbeHeight = 10f;
        [SerializeField] private float _groundProbeDist = 30f;

        [Header("NavMesh")]
        [SerializeField] private bool _snapToNavMesh = true;
        [SerializeField] private float _navMeshSampleRadius = 1f;

        public Vector3 CurrentTargetPosition { get; private set; }
        public bool HasValidTarget { get; private set; }

        private Renderer[] _renderers;

        #region Unity Function

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _renderers = GetComponentsInChildren<Renderer>();
            CurrentTargetPosition = transform.position;
        }

        private void OnEnable()
        {
            _moveIndicatorAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveIndicatorAction.action.Disable();
        }

        private void Start()
        {
            InitializeIndicatorPosition();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            MoveIndicatorFromInput();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance.HighlightedObject != null) return;

            Debug.Log("Entered Trigger");

            if (other.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                if (!interactable.IsInteractive || !GameManager.Instance.SetHighlightedObject(interactable)) return;
                Vector3 currentScale = transform.localScale;
                Vector3 resize = new (currentScale.x * 2, currentScale.y, currentScale.z * 2);
                transform.localScale = resize;

                other.gameObject.layer = 8;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (GameManager.Instance.HighlightedObject == null) return;

            if (other.TryGetComponent<IInteractable>(out IInteractable interactable) && interactable == GameManager.Instance.HighlightedObject)
            {
                if (!GameManager.Instance.ClearHighlightedObject()) return;
                Vector3 currentScale = transform.localScale;
                Vector3 resize = new (currentScale.x / 2, currentScale.y, currentScale.z / 2);
                transform.localScale = resize;

                if (other.TryGetComponent<Blot>(out Blot blot)) return;
                other.gameObject.layer = 0;
            }
        }

        #endregion

        #region Indicator Movement Functions
        
        private void MoveIndicatorFromInput()
        {
            if (_camera == null || _moveIndicatorAction == null)
            {
                SetVisible(false);
                HasValidTarget = false;
                return;
            }

            Vector2 input = _moveIndicatorAction.action.ReadValue<Vector2>();

            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 cameraForward = _camera.transform.forward;
            Vector3 cameraRight = _camera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = cameraRight * input.x + cameraForward * input.y;

            if (moveDirection.sqrMagnitude < 0.01f)
            {
                return;
            }

            float moveAmount = GetMoveAmount();
            Vector3 desiredPosition = CurrentTargetPosition + moveDirection * moveAmount;

            if (!TryGetValidGroundPosition(desiredPosition, out Vector3 validPosition))
            {
                return;
            }

            if (!TryClampPositionToCameraView(validPosition, out Vector3 clampedPosition))
            {
                return;
            }

            SetIndicatorPosition(clampedPosition);
        }


        private float GetMoveAmount()
        {
            InputControl activeControl = _moveIndicatorAction.action.activeControl;

            if (activeControl != null && activeControl.device is Mouse)
            {
                return _mouseMoveSensitivity;
            }

            return _controllerMoveSpeed * Time.deltaTime;
        }

        private void InitializeIndicatorPosition()
        {
            Vector3 startPosition = transform.position;

            if (TryGetValidGroundPosition(startPosition, out Vector3 validPosition))
            {
                if (TryClampPositionToCameraView(validPosition, out Vector3 clampedPosition))
                {
                    SetIndicatorPosition(clampedPosition);
                    return;
                }
            }

            HasValidTarget = false;
            SetVisible(false);
        }

        private bool TryGetValidGroundPosition(Vector3 position, out Vector3 validPosition)
        {
            validPosition = position;

            if (_snapToNavMesh)
            {
                if (!NavMesh.SamplePosition(
                        position,
                        out NavMeshHit navHit,
                        _navMeshSampleRadius,
                        NavMesh.AllAreas))
                {
                    return false;
                }

                validPosition = navHit.position;
            }

            Vector3 rayOrigin = validPosition + Vector3.up * _groundProbeHeight;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    _groundProbeDist,
                    _groundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                validPosition = hit.point;
                return true;
            }

            return _snapToNavMesh;
        }

        private bool TryClampPositionToCameraView(Vector3 worldPosition, out Vector3 clampedGroundPosition)
        {
            clampedGroundPosition = worldPosition;

            Vector3 viewportPosition = _camera.WorldToViewportPoint(worldPosition);

            if (viewportPosition.z <= 0f)
            {
                return false;
            }

            float clampedX = Mathf.Clamp(
                viewportPosition.x,
                _viewportMargin,
                1f - _viewportMargin
            );

            float clampedY = Mathf.Clamp(
                viewportPosition.y,
                _viewportMargin,
                1f - _viewportMargin
            );

            bool wasClamped =
                !Mathf.Approximately(viewportPosition.x, clampedX) ||
                !Mathf.Approximately(viewportPosition.y, clampedY);

            if (!wasClamped)
            {
                return true;
            }

            Ray ray = _camera.ViewportPointToRay(new Vector3(clampedX, clampedY, 0f));

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    _cameraClampRayDistance,
                    _groundLayer,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 groundPosition = hit.point;

            if (_snapToNavMesh)
            {
                if (!NavMesh.SamplePosition(
                        groundPosition,
                        out NavMeshHit navHit,
                        _navMeshSampleRadius,
                        NavMesh.AllAreas))
                {
                    return false;
                }

                groundPosition = navHit.position;
            }

            clampedGroundPosition = groundPosition;
            return true;
        }

        private void SetIndicatorPosition(Vector3 position)
        {
            CurrentTargetPosition = position;
            HasValidTarget = true;

            transform.position = CurrentTargetPosition + Vector3.up * _groundOffset;

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.enabled = visible;
            }
        }

        #endregion
    }
}