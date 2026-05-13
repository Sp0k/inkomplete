using UnityEngine;
using GameManagement;
using Blots;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour
{
    [Header("Action References")]
    [SerializeField] private InputActionReference _tiltLeftAction;
    [SerializeField] private InputActionReference _tiltRightAction;
    [SerializeField] private InputActionReference _dezoomAction;
    [SerializeField] private InputActionReference _zoomAction;

    [Header("Tilt Parameters")]
    [SerializeField] private float _tiltStrength = 3f;
    [SerializeField] private float _dezoomStrength = 3f;
    private Vector3 offset;
    private Vector3 targetPosition;

    public void SetOffset()
    {
        if (_tiltLeftAction == null || _tiltRightAction == null || _dezoomAction == null)
        {
            Debug.LogWarning("CameraControls: Input actions not assigned.");
            return;
        }

        float depthOffset = 0f;
        float sideOffset = 0f;

        if (_dezoomAction.action.IsPressed())
        {
            depthOffset -= _dezoomStrength;
        }
        
        if (_zoomAction.action.IsPressed())
        {
            depthOffset += _dezoomStrength;
        }

        if (_tiltLeftAction.action.IsPressed())
        {
            sideOffset -= _tiltStrength;
        }
        
        if (_tiltRightAction.action.IsPressed())
        {
            sideOffset += _tiltStrength;
        }

        offset = new Vector3(sideOffset, 0f, depthOffset);
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
