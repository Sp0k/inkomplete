using UnityEngine;
using Interfaces;
using GameManagement;

namespace Puzzle.PhysicsBased
{
    public class MovableObject : MonoBehaviour, IInteractable
    {
        public bool IsInteractive { get; private set; } = true;
        public Transform PuzzleGroup;

        #region Interaction

        public void Interact()
        {
            if (!IsInteractive) return;

            GameManager.Instance.HandleMovableBlockInteraction(this.transform);
        }

        #endregion
    }
}