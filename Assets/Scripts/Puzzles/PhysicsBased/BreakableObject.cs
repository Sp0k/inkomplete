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

        #region Interaction

        public void Interact()
        {
            int hitStrength = GameManager.Instance.PlayerBlots.Count;
            _hitPoints -= hitStrength;

            if (_hitPoints <= 0)
            {
                AudioManager.Instance.PlaySfx(_sfxRef, false, false);
                Destroy(gameObject);
            }
        }

        #endregion
    }
}