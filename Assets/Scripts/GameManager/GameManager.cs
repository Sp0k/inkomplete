using UnityEngine;
using Blots;
using System.Collections.Generic;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private List<Blot> _blotsInLevel;
        [SerializeField]
        private List<Blot> _playerBlots;

        public float TargetPointRadiusMultiplier = 0.1f;

        public List<Blot> PlayerBlots => _playerBlots;
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
                        GameObject singletonObject = new GameObject("GameManager");
                        _instance = singletonObject.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

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
    }
}