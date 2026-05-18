using Blots;
using GameManagement;
using UnityEngine;

namespace Puzzle.PhysicsBased
{
    public class PuzzleCompletion : MonoBehaviour
    {
        [Header("SFX")]
        [SerializeField] private string _sfxRef;

        [Header("Puzzle Piece")]
        [SerializeField] private MovableObject _attachedPuzzlePiece;

        [Header("Placement")]
        [SerializeField] private Transform _pivot;

        private Blot _currentCarryingBlot;
        private bool _isCompleted = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_isCompleted) return;
            if (_attachedPuzzlePiece == null) return;

            Blot enteringBlot = other.GetComponentInParent<Blot>();
            if (enteringBlot == null) return;

            if (!IsPieceCarriedBy(enteringBlot)) return;

            _currentCarryingBlot = enteringBlot;
            GameManager.Instance.SetActivePuzzleCompletion(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isCompleted) return;

            Blot exitingBlot = other.GetComponentInParent<Blot>();
            if (exitingBlot == null) return;

            if (exitingBlot != _currentCarryingBlot) return;

            _currentCarryingBlot = null;
            GameManager.Instance.ClearActivePuzzleCompletion(this);
        }

        public bool TryCompletePuzzle()
        {
            if (_isCompleted) return false;
            if (_currentCarryingBlot == null) return false;
            if (_attachedPuzzlePiece == null || _pivot == null) return false;

            if (!IsPieceCarriedBy(_currentCarryingBlot))
            {
                return false;
            }

            bool placed = GameManager.Instance.TryPlaceMovableBlock(
                _attachedPuzzlePiece,
                _pivot
            );

            if (!placed)
            {
                return false;
            }

            _isCompleted = true;
            _currentCarryingBlot = null;
            _attachedPuzzlePiece.CompletePuzzle();
            GameManager.Instance.ClearActivePuzzleCompletion(this);

            if (!string.IsNullOrEmpty(_sfxRef))
            {
                AudioManager.Instance.PlaySfx(_sfxRef, false, false);
            }

            GameManager.Instance.CurrentLevel.RebakeNavMesh();

            return true;
        }

        private bool IsPieceCarriedBy(Blot blot)
        {
            if (blot == null || _attachedPuzzlePiece == null)
            {
                return false;
            }

            Blot carryingBlot = _attachedPuzzlePiece.GetComponentInParent<Blot>();

            return carryingBlot == blot;
        }
    }
}