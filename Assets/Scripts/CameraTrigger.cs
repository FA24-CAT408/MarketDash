using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera targetCamera;
    
    public int cameraIndex = -1;
    
    // Store the previous camera when player enters
    private CinemachineVirtualCamera previousCamera;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            previousCamera = CameraSystem.Instance.currentCamera;
            
            // Activate the target camera
            if (targetCamera != null)
            {
                CameraSystem.Instance.SetNewCamera(targetCamera);
            }
            else if (cameraIndex >= 0)
            {
                CameraSystem.Instance.SetCameraByIndex(cameraIndex);
            }
            else
            {
                Debug.LogWarning("No camera specified for this trigger!");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Update the target camera to be the previous camera
            // This way, if the player re-enters this trigger, it will switch back
            targetCamera = previousCamera;
        }
    }
}
