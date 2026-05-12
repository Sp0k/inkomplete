using UnityEngine;
using UnityEngine.InputSystem;
using GameManagement;

namespace Player
{
    public class PlayerControls : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private LayerMask _interactableLayerMask;
        private Vector3 prevPos = Vector3.zero;

        public void OnLeftClick(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayerMask | _interactableLayerMask))
            {
                Vector3 targetPosition = hit.point;
                if (targetPosition == prevPos)
                {
                    return;
                }

                transform.position = targetPosition;
                prevPos = targetPosition;

                GameManager.Instance.MovePlayerBlots(targetPosition);
            }
        }
    }
}