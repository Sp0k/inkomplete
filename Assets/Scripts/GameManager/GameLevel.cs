using UnityEngine;
using System.Collections.Generic;
using Blots;
using Unity.AI.Navigation;

namespace GameManagement
{
    public class GameLevel : MonoBehaviour
    {
        [Header("Blots")]
        [SerializeField] private List<Blot> _blotsInLevel;
        public List<Blot> BlotsInLevel => _blotsInLevel;
        [SerializeField] private List<Blot> _startingBlots;
        public List<Blot> StartingBlots => _startingBlots;

        [Header("")]
        [SerializeField] private NavMeshSurface _levelNavMesh;
        public NavMeshSurface LevelNavMesh => _levelNavMesh;

        #region Unity Functions
        
        private void Start()
        {
            GameManager.Instance.CurrentLevel = this;
            GameManager.Instance.StartLevel();
        }

        #endregion

        #region NavMesh

        public void RebakeNavMesh()
        {
            _levelNavMesh.RemoveData();
            _levelNavMesh.BuildNavMesh();
        }

        #endregion
    }
}