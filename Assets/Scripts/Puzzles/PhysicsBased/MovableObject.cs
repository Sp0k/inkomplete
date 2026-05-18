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

        public Rigidbody _rigidBody;

        private void Start()
        {
            if (_rigidBody == null)
            {
                _rigidBody = GetComponent<Rigidbody>();
            }

            _rigidBody.useGravity = true;
        }

        public void Interact()
        {
            if (!IsInteractive || IsPlaced) return;

            GameManager.Instance.HandleMovableBlockInteraction(transform);
        }

        public void SetCarried(bool isCarried)
        {
            if (IsPlaced) return;

            IsInteractive = !isCarried;
            gameObject.layer = 3;
            GameManager.Instance.CurrentLevel.RebakeNavMesh();
        }

        public void PlaceAtPivot(Transform pivot)
        {
            if (pivot == null) return;

            IsPlaced = true;
            IsInteractive = false;

            transform.SetParent(pivot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (_rigidBody != null)
            {
                _rigidBody.linearVelocity = Vector3.zero;
                _rigidBody.angularVelocity = Vector3.zero;
            }

            gameObject.layer = 0;
        }

        public void CompletePuzzle()
        {
            _rigidBody.isKinematic = false;
            _rigidBody.useGravity = false;
            IsInteractive = false;
            gameObject.layer = 6;
        }
    }
}