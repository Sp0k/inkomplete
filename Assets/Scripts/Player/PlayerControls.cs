using UnityEngine;
using UnityEngine.InputSystem;
using GameManagement;

namespace Player
{
    public class PlayerControls : MonoBehaviour
    {
        private Vector3 prevPos = Vector3.zero;

        public bool CanClick = true;

        public void OnLeftClick(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (!CanClick)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
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