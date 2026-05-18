using GameManagement;
using Interfaces;
using UnityEngine;

namespace Puzzle.PhysicsBased
{
    public class BreakableObject : MonoBehaviour, IInteractable
    {
        public bool IsInteractive { get; private set; } = true;

        [Header("Settings")]
        [SerializeField] private int _hitPoints = 3;
        [SerializeField] private string _sfxRef;

        private bool _isBeingAttacked = false;

        public void Interact()
        {
            if (!IsInteractive || _isBeingAttacked) return;

            GameManager.Instance.HandleBreakableObjectInteraction(this);
        }

        public bool TryBeginAttack()
        {
            if (!IsInteractive || _isBeingAttacked)
            {
                return false;
            }

            _isBeingAttacked = true;
            return true;
        }

        public void CancelAttack()
        {
            _isBeingAttacked = false;
        }

        public void ApplyDamage(int hitStrength)
        {
            if (!IsInteractive) return;

            _isBeingAttacked = false;

            _hitPoints -= hitStrength;

            if (_hitPoints <= 0)
            {
                Break();
            }
        }

        private void Break()
        {
            IsInteractive = false;

            if (!string.IsNullOrEmpty(_sfxRef))
            {
                AudioManager.Instance.PlaySfx(_sfxRef, false, false);
            }

            if (GameManager.Instance.HighlightedObject == this)
            {
                GameManager.Instance.ClearHighlightedObject();
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                Destroy(child.gameObject);
            }

            GameManager.Instance.BreakObject(gameObject);
        }
    }
}