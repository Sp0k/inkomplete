using UnityEngine;
using Interfaces;
using GameManagement;

namespace Puzzle.PhysicsBased
{
    public class MovableObject : MonoBehaviour, IInteractable
    {
        public bool IsInteractive { get; private set; } = true;
        public bool IsPlaced { get; private set; } = false;

        public Transform PuzzleGroup;

        public void Interact()
        {
            if (!IsInteractive || IsPlaced) return;

            GameManager.Instance.HandleMovableBlockInteraction(transform);
        }

        public void SetCarried(bool isCarried)
        {
            if (IsPlaced) return;

            IsInteractive = !isCarried;
        }

        public void PlaceAtPivot(Transform pivot)
        {
            if (pivot == null) return;

            IsPlaced = true;
            IsInteractive = false;

            transform.SetParent(pivot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            gameObject.layer = 0;
        }

        public void CompletePuzzle()
        {
            IsInteractive = false;
            gameObject.layer = 6;
        }
    }
}