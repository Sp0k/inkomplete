using UnityEngine;
using UnityEngine.InputSystem;
using GameManagement;
using System.Collections.Generic;
using Blots;

namespace Player
{
    public class PlayerControls : MonoBehaviour
    {
        public void OnLeftClick(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 targetPosition = hit.point;
                    transform.position = targetPosition;

                    List<Blot> playerBlots = GameManager.Instance.PlayerBlots;
                    
                    if (playerBlots.Count == 1)
                    {
                        playerBlots[0].MoveBlot(targetPosition);
                        return;
                    }
                    
                    float radius = GameManager.Instance.TargetPointRadiusMultiplier * playerBlots.Count;
                    foreach (Blot blot in playerBlots)
                    {
                        Vector3 randomOffset = Random.insideUnitSphere * radius;
                        randomOffset.y = 0;
                        blot.MoveBlot(targetPosition + randomOffset);
                    }
                }
            }
        }
    }
}