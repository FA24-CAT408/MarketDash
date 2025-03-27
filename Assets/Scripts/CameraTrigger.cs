using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera targetCamera;
    
    public bool updateTargetOnExit = false;
    
    public int cameraIndex = -1;
    
    // Store the previous camera when player enters
    private CinemachineVirtualCamera previousCamera;
    private CameraSystem cameraSystem;

    private void Start()
    {
        cameraSystem = FindObjectOfType<CameraSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            previousCamera = cameraSystem.currentCamera;
            
            // Activate the target camera
            if (targetCamera != null)
            {
                cameraSystem.SetNewCamera(targetCamera);
            }
            else if (cameraIndex >= 0)
            {
                cameraSystem.SetCameraByIndex(cameraIndex);
            }
            else
            {
                Debug.LogWarning("No camera specified for this trigger!");
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && updateTargetOnExit)
        {
            // Update the target camera to be the previous camera
            // This way, if the player re-enters this trigger, it will switch back
            targetCamera = previousCamera;
        }
    }
}
