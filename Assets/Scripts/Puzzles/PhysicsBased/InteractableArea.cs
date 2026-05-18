using System.Collections.Generic;
using Blots;
using GameManagement;
using Interfaces;
using UnityEngine;

namespace Puzzle.PhysicsBased
{
    [RequireComponent(typeof(Collider))]
    public class InteractableArea : MonoBehaviour
    {
        [Header("Interactable")]
        [SerializeField] private MonoBehaviour _interactableBehaviour;

        private IInteractable _interactable;
        private readonly HashSet<Blot> _blotsInRange = new();

        private void Awake()
        {
            Collider areaCollider = GetComponent<Collider>();
            areaCollider.isTrigger = true;

            if (_interactableBehaviour != null)
            {
                _interactable = _interactableBehaviour as IInteractable;
            }

            if (_interactable == null)
            {
                _interactable = GetComponentInParent<IInteractable>();
            }

            if (_interactable == null)
            {
                Debug.LogWarning($"{name} has no IInteractable assigned or found in parent.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_interactable == null || !_interactable.IsInteractive)
            {
                return;
            }

            Blot blot = other.GetComponentInParent<Blot>();

            if (blot == null)
            {
                return;
            }

            if (!GameManager.Instance.IsPlayerBlot(blot))
            {
                return;
            }

            _blotsInRange.Add(blot);
            GameManager.Instance.SetHighlightedObject(_interactable);
        }

        private void OnTriggerExit(Collider other)
        {
            Blot blot = other.GetComponentInParent<Blot>();

            if (blot == null)
            {
                return;
            }

            _blotsInRange.Remove(blot);

            if (_blotsInRange.Count == 0)
            {
                GameManager.Instance.ClearHighlightedObject(_interactable);
            }
        }

        private void OnDisable()
        {
            _blotsInRange.Clear();

            if (_interactable != null)
            {
                GameManager.Instance.ClearHighlightedObject(_interactable);
            }
        }
    }
}