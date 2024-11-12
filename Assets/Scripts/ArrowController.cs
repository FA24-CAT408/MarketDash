using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] private float rotationOffset = 0f;
    // [SerializeField] private Camera overlayCamera;
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    PlayerStateMachine _playerStateMachine;
    [SerializeField] Transform _targetTransform;
    Camera _mainCamera;
    Vector3 _fixedPosition;

    // Start is called before the first frame update
    void Start()
    {
        _playerStateMachine = FindObjectOfType<PlayerStateMachine>();
        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        _mainCamera = Camera.main;

        // SetFixedOverlayPosition();
    }

    // void SetFixedOverlayPosition()
    // {
    //     if (overlayCamera != null)
    //     {
    //         // Convert viewport position to world space using the overlay camera
    //         _fixedPosition = overlayCamera.ViewportToWorldPoint(viewportPosition);
    //         transform.position = _fixedPosition;
    //     }
    // }

    void LateUpdate()
    {
        if (GroceryListManager.Instance.targetItem != null)
        {
            _targetTransform = GroceryListManager.Instance.targetItem.transform;
            UpdateArrowRotation();
        }
    }

    void UpdateArrowRotation()
    {
        if (_targetTransform == null || _mainCamera == null || freeLookCamera == null) return;

        // Get the camera's forward direction and direction to target
        Vector3 cameraForward = freeLookCamera.transform.forward;
        Vector3 targetDirection = (_targetTransform.position - freeLookCamera.transform.position).normalized;

        // Project vectors onto XZ plane for horizontal angle calculation
        cameraForward.y = 0;
        targetDirection.y = 0;
        cameraForward.Normalize();
        targetDirection.Normalize();

        // Calculate angle between camera's forward and direction to target
        float angle = Vector3.SignedAngle(cameraForward, targetDirection, Vector3.up);

        // Apply rotation while maintaining fixed position
        transform.rotation = Quaternion.Euler(90, freeLookCamera.transform.eulerAngles.y + angle + rotationOffset, 0);
    }

    // Optional: Method to update the viewport position if needed
    // public void UpdateViewportPosition(Vector3 newViewportPosition)
    // {
    //     viewportPosition = newViewportPosition;
    //     SetFixedOverlayPosition();
    // }

    // Optional: For debugging in editor
    void OnDrawGizmos()
    {
        if (_targetTransform != null && _mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetTransform.position);
        }
    }
}