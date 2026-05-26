using UnityEngine;
using UnityEngine.InputSystem;
using GameManagement;
using UnityEngine.SceneManagement;

namespace Player
{
    public class PlayerControls : MonoBehaviour
    {
        public bool CanClick = true;

        [SerializeField] private ControlsDisplayWidget _widget;

        [SerializeField] private GroundTargetIndicator _groundIndicator;
        private Vector3 prevPos = Vector3.zero;

        public void OnMoveTarget(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (!CanClick) return;

            if (_groundIndicator == null || !_groundIndicator.HasValidTarget) return;

            Vector3 targetPosition = _groundIndicator.CurrentTargetPosition;

            if (targetPosition == prevPos) return;

            prevPos = targetPosition;

            GameManager.Instance.MovePlayerBlots(targetPosition);
        }

        public void OnInteractWithTarget(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (GameManager.Instance.TryCompleteActivePuzzle())
            {
                return;
            }

            GameManager.Instance.TryInteractWithHighlightedObject();
        }

        public void OnQuitLevel(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            SceneManager.LoadScene("MainMenu");
        }

        public void OnToggleDrawer(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (_widget != null)
            {
                _widget.ToggleDrawer();
            }
        }
    }
}