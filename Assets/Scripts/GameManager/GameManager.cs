using Blots;
using Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puzzle.PhysicsBased;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Management")]
        public GameLevel CurrentLevel { get; set; }
        [SerializeField] private List<Blot> _blotsInLevel;
        [SerializeField] private List<Blot> _playerBlots;

        [Header("Input")]
        public IInteractable HighlightedObject { get; private set; }

        [Header("Group Movement")]
        [SerializeField] private BlotGroupController _playerBlotGroup;

        [Header("UI Elements")]
        [SerializeField] private BlotCounterWidget m_BlotCounter;

        public List<Blot> PlayerBlots => _playerBlots;

        private PuzzleCompletion _activePuzzleCompletion;


        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();

                    if (_instance == null)
                    {
                        GameObject singletonObj = new GameObject("GameManager");
                        _instance = singletonObj.AddComponent<GameManager>();
                    }
                }

                return _instance;
            }
        }

        #region Unity Functions

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Blot Navigation

        public void MovePlayerBlots(Vector3 targetPosition)
        {
            if (_playerBlotGroup == null)
            {
                Debug.LogWarning("No BlotGroupController assigned to the GameManager.");
                return;
            }

            _playerBlotGroup.MoveGroupTo(targetPosition);
        }
        public void RegisterPlayerBlot(Blot blot)
        {
            if (blot == null)
            {
                return;
            }

            if (!_playerBlots.Contains(blot))
            {
                _playerBlots.Add(blot);
            }

            if (_playerBlotGroup != null)
            {
                _playerBlotGroup.AddBlot(blot);
            }
        }

        #endregion

        #region Level Management

        public void StartLevel()
        {
            _blotsInLevel.Clear();
            _playerBlots.Clear();

            if (_playerBlotGroup != null)
            {
                _playerBlotGroup.Clear();
            }

            _blotsInLevel.AddRange(CurrentLevel.BlotsInLevel);

            foreach (Blot blot in _blotsInLevel)
            {
                blot.InitializeBlot();
            }

            foreach (Blot blot in CurrentLevel.StartingBlots)
            {
                blot.RecruitBlot();
            }

            m_BlotCounter.InitCounterText(CurrentLevel.StartingBlots.Count,
                CurrentLevel.BlotsInLevel.Count);
        }

		#endregion

		#region UI Management

        public void UpdateBlotCount()
        {
            m_BlotCounter.UpdateCurrentCount(PlayerBlots.Count);
		}

		#endregion

		#region Interaction

		public bool SetHighlightedObject(IInteractable interactable)
        {
            if (HighlightedObject != null) return false;

            HighlightedObject = interactable;
            return true;
        }

        public bool ClearHighlightedObject()
        {
            if (HighlightedObject == null) return false;

            HighlightedObject = null;
            return true;
        }

        public bool ClearHighlightedObject(IInteractable interactable)
        {
            if (HighlightedObject == null) return false;
            if (HighlightedObject != interactable) return false;

            HighlightedObject = null;
            return true;
        }

        public bool IsPlayerBlot(Blot blot)
        {
            return blot != null && _playerBlots.Contains(blot);
        }

        public bool TryInteractWithHighlightedObject()
        {
            if (HighlightedObject == null)
            {
                return false;
            }

            if (!HighlightedObject.IsInteractive)
            {
                HighlightedObject = null;
                return false;
            }

            HighlightedObject.Interact();
            return true;
        }

        public void HandleMovableBlockInteraction(Transform moveableTransform)
        {
            if (_playerBlotGroup == null)
            {
                Debug.LogWarning("No BlotGroupController assigned to the GameManager.");
                return;
            }

            if (_playerBlotGroup.HasAttachedMoveable())
            {
                return;
            }

            _playerBlotGroup.AssignMoveableToPivot(moveableTransform);
        }

        public bool TryPlaceMovableBlock(MovableObject movableObject, Transform pivot)
        {
            if (_playerBlotGroup == null)
            {
                Debug.LogWarning("No BlotGroupController assigned to the GameManager.");
                return false;
            }

            return _playerBlotGroup.TryPlaceAttachedMoveable(movableObject, pivot);
        }

        public void SetActivePuzzleCompletion(PuzzleCompletion puzzleCompletion)
        {
            _activePuzzleCompletion = puzzleCompletion;
        }

        public void ClearActivePuzzleCompletion(PuzzleCompletion puzzleCompletion)
        {
            if (_activePuzzleCompletion == puzzleCompletion)
            {
                _activePuzzleCompletion = null;
            }
        }

        public bool TryCompleteActivePuzzle()
        {
            if (_activePuzzleCompletion == null)
            {
                return false;
            }

            return _activePuzzleCompletion.TryCompletePuzzle();
        }

        public void HandleBreakableObjectInteraction(BreakableObject breakableObject)
        {
            if (_playerBlotGroup == null)
            {
                Debug.LogWarning("No BlotGroupController assigned to the GameManager.");
                return;
            }

            _playerBlotGroup.AttackBreakableObject(breakableObject);
        }

        public void BreakObject(GameObject go)
        {
            Destroy(go);
            StartCoroutine(RebakeNavMesh());
        }

        private IEnumerator RebakeNavMesh()
        {
            yield return new WaitForEndOfFrame();
            CurrentLevel.RebakeNavMesh();
            yield return null;
        }

        #endregion
    }
}