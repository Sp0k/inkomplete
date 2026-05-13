using UnityEngine;
using GameManagement;
using Blots;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour
{
    [SerializeField] private InputActionReference _tiltLeftAction;
    [SerializeField] private InputActionReference _tiltRightAction;
    [SerializeField] private float _tiltStrength = 3f;
    private Vector3 offset;
    private Vector3 targetPosition;

    public void SetOffset()
    {
        if (_tiltLeftAction == null || _tiltRightAction == null)
        {
            Debug.LogWarning("CameraControls: Input actions not assigned.");
            return;
        }

        if (_tiltLeftAction.action.IsPressed())
        {
            offset = new Vector3(-_tiltStrength, 0f, 0f);
        }
        else if (_tiltRightAction.action.IsPressed())
        {
            offset = new Vector3(_tiltStrength, 0f, 0f);
        }
        else
        {
            offset = new Vector3(0f, 0f, 0f);
        }
    }

    private void Update()
    {
        List<Blot> blots = GameManager.Instance.PlayerBlots;
        if (blots.Count == 0)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (Blot blot in blots)        
        {
            center += blot.transform.position;
        }
        center /= blots.Count;

        SetOffset();

        targetPosition = new Vector3(center.x, transform.position.y, center.z) + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime);
    }
}
