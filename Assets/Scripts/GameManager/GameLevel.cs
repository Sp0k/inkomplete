using UnityEngine;
using System.Collections.Generic;
using Blots;

namespace GameManagement
{
    public class GameLevel : MonoBehaviour
    {
        [SerializeField] private List<Blot> _blotsInLevel;
        public List<Blot> BlotsInLevel => _blotsInLevel;

        [SerializeField] private List<Blot> _startingBlots;
        public List<Blot> StartingBlots => _startingBlots;
    }
}