using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public CinemachineVirtualCameraBase currentCamera;
    
    public List<CinemachineVirtualCameraBase> virtualCameras;
    
    [Header("Camera Flip Settings")]
    public float flipDuration = 1.0f;
    public Ease flipEase = Ease.InOutQuad;
    
    private void Awake()
    {
        if (virtualCameras.Count > 0 && currentCamera == null)
        {
            currentCamera = virtualCameras[0];
            currentCamera.Priority = 100;
        }
    }

    public void SetNewCamera(CinemachineVirtualCameraBase newCamera)
    {
        if (newCamera == currentCamera) return;
        
        foreach (var cam in virtualCameras)
        {
            cam.Priority = 0;
        }
        
        newCamera.Priority = 100;
        currentCamera = newCamera;
    }
    
    public void SetCameraByIndex(int cameraIndex)
    {
        if (cameraIndex >= 0 && cameraIndex < virtualCameras.Count)
        {
            SetNewCamera(virtualCameras[cameraIndex]);
        }
        else
        {
            Debug.LogWarning($"Camera index {cameraIndex} is out of range!");
        }
    }
    
    public void SetCurrentCameraXOffset(float xOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineVirtualCamera virtualCamera = currentCamera as CinemachineVirtualCamera;
        if (virtualCamera == null) return;
        
        CinemachineTrackedDolly trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) return;
        
        Vector3 currentOffset = trackedDolly.m_PathOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.x = xOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    // Unity Event compatible method - sets custom Y offset for current camera
    public void SetCurrentCameraYOffset(float yOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineVirtualCamera virtualCamera = currentCamera as CinemachineVirtualCamera;
        if (virtualCamera == null) return;
        
        CinemachineTrackedDolly trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) return;
        
        Vector3 currentOffset = trackedDolly.m_PathOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.y = yOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    // Unity Event compatible method - sets custom Z offset for current camera
    public void SetCurrentCameraZOffset(float zOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineVirtualCamera virtualCamera = currentCamera as CinemachineVirtualCamera;
        if (virtualCamera == null) return;
        
        CinemachineTrackedDolly trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) return;
        
        Vector3 currentOffset = trackedDolly.m_PathOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.z = zOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    // Convenience method to flip the current camera
    public void FlipCurrentCamera()
    {
        if (currentCamera != null)
        {
            FlipCamera(currentCamera);
        }
    }
    
    private void FlipCamera(CinemachineVirtualCameraBase camera)
    {
        // We need to cast to CinemachineVirtualCamera to access GetCinemachineComponent
        CinemachineVirtualCamera virtualCamera = camera as CinemachineVirtualCamera;
        
        if (virtualCamera == null)
        {
            Debug.LogWarning($"Camera {camera.name} is not a CinemachineVirtualCamera!");
            return;
        }
        
        // Try to get the dolly track component
        CinemachineTrackedDolly trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        
        if (trackedDolly != null)
        {
            // Get the current path offset
            Vector3 currentOffset = trackedDolly.m_PathOffset;
            
            // Calculate the target offset (with inverted Z)
            Vector3 targetOffset = currentOffset;
            targetOffset.z = -currentOffset.z;
            
            // Use the custom method to set the path offset
            SetCameraPathOffset(camera, targetOffset);
        }
        else
        {
            Debug.LogWarning($"Camera {camera.name} does not have a CinemachineTrackedDolly component!");
        }
    }
    
    private void SetCameraPathOffset(CinemachineVirtualCameraBase camera, Vector3 targetOffset, float duration = -1)
    {
        // Use the specified duration or fall back to the default
        float tweenDuration = duration > 0 ? duration : flipDuration;
        
        // We need to cast to CinemachineVirtualCamera to access GetCinemachineComponent
        CinemachineVirtualCamera virtualCamera = camera as CinemachineVirtualCamera;
        
        if (virtualCamera == null)
        {
            Debug.LogWarning($"Camera {camera.name} is not a CinemachineVirtualCamera!");
            return;
        }
        
        // Try to get the dolly track component
        CinemachineTrackedDolly trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        
        if (trackedDolly != null)
        {
            // Get the current path offset
            Vector3 currentOffset = trackedDolly.m_PathOffset;
            
            // Create a temporary Vector3 to tween
            Vector3 offsetValue = currentOffset;
            
            // Tween the offset
            DOTween.To(() => offsetValue, 
                       x => {
                           offsetValue = x;
                           trackedDolly.m_PathOffset = offsetValue;
                       }, 
                       targetOffset, 
                       tweenDuration)
                   .SetEase(flipEase)
                   .OnComplete(() => {
                       Debug.Log($"Camera {camera.name} path offset set to {targetOffset}");
                   });
        }
        else
        {
            Debug.LogWarning($"Camera {camera.name} does not have a CinemachineTrackedDolly component!");
        }
    }
}
