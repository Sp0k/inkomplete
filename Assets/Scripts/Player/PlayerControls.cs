using UnityEngine;
using UnityEngine.InputSystem;
using GameManagement;

namespace Player
{
    public class PlayerControls : MonoBehaviour
    {
        public bool CanClick = true;

        [SerializeField] private GroundTargetIndicator _groundIndicator;
        private Vector3 prevPos = Vector3.zero;

        public void OnMoveTarget(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (!CanClick) return;

            if (_groundIndicator == null || !_groundIndicator.HasValidTarget || GameManager.Instance.HighlightedObject != null) return;

            Vector3 targetPosition = _groundIndicator.CurrentTargetPosition;

            if (targetPosition == prevPos) return;

            prevPos = targetPosition;

            GameManager.Instance.MovePlayerBlots(targetPosition);
        }

        public void OnInteractWithTarget(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (GameManager.Instance.HighlightedObject == null) return;

            GameManager.Instance.HighlightedObject.Interact();
        }
    }
}