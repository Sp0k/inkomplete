using Blots;
using GameManagement;
using Interfaces;
using UnityEngine;

namespace Puzzle.Bridge
{
    public class BridgePuzzle : MonoBehaviour, IInteractable
    {
        public bool IsInteractive { get; private set; } = true;

        [SerializeField] private GameObject _bridge;
        [SerializeField] private int _requiredBlots;

        public void Interact()
        {
            Debug.Log("Interacting with Bridge!");

            if (GameManager.Instance.PlayerBlots.Count < _requiredBlots)
            {
                foreach (Blot b in GameManager.Instance.PlayerBlots)
                {
                    b.GetConfused();
                }
                return;
            }

            _bridge.SetActive(true);
            GameManager.Instance.CurrentLevel.RebakeNavMesh();
            AudioManager.Instance.PlaySfx("puzzle_completion");
            IsInteractive = false;
        }
    }
}