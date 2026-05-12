using System.Collections.Generic;
using Blots;
using UnityEngine;

namespace Obstacles
{
    public class FanObstacle : MonoBehaviour
    {
        [SerializeField] private Transform _pivotPoint;
        [SerializeField] private List<Blot> _blotsInRange = new();
        [SerializeField] private LayerMask _aoeLayer;
        [SerializeField] private Vector3 _windPushDir = Vector3.left;
        public Vector3 WindPushDir => _windPushDir;


        #region Unity Functions

        private void Update()
        {

            foreach (Blot blot in _blotsInRange)
            {
                if (HasDirectAccessToBlot(blot))
                {
                    Vector3 windDir = blot.transform.position - _pivotPoint.position;
                    windDir.y = 0f;

                    blot.ApplyFanWind(windDir);
                }
                else
                {
                    blot.ClearFanWind();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Blot entered fan range.");
            Blot blot = other.GetComponentInParent<Blot>();
            if (blot != null && !_blotsInRange.Contains(blot))
            {
                _blotsInRange.Add(blot);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log("Blot exited fan range.");
            Blot blot = other.GetComponentInParent<Blot>();
            if (blot != null && _blotsInRange.Contains(blot))
            {
                _blotsInRange.Remove(blot);
                blot.ClearFanWind();
            }
        }

        #endregion

        #region Fan Logic

        private bool HasDirectAccessToBlot(Blot blot)
        {
            Vector3 orig = _pivotPoint.position;

            Vector3 target = blot.transform.position + Vector3.up * 0.5f;

            Vector3 dir = target - orig;
            float dist = dir.magnitude;

            if (Physics.Raycast(
                orig,
                dir.normalized,
                out RaycastHit hit,
                dist,
                ~_aoeLayer,
                QueryTriggerInteraction.Ignore))

            {
                Blot hitBlot = hit.collider.GetComponentInParent<Blot>();
                return hitBlot == blot;
            }
            return false;
        }

        #endregion
    }
}