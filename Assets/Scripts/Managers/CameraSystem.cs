using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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
            currentCamera.Priority.Value = 100;
        }
    }

    /// <summary>
    /// Switches active camera by setting priority. Deactivates all other cameras.
    /// </summary>
    public void SetNewCamera(CinemachineVirtualCameraBase newCamera)
    {
        if (newCamera == currentCamera) return;
        
        foreach (var cam in virtualCameras)
        {
            cam.Priority.Value = 0;
        }
        
        newCamera.Priority.Value = 100;
        currentCamera = newCamera;
    }
    
    /// <summary>
    /// Switches to a camera by its index in the virtualCameras list.
    /// </summary>
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
    
    /// <summary>
    /// Sets the X component of the current camera's spline dolly offset.
    /// </summary>
    public void SetCurrentCameraXOffset(float xOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineSplineDolly splineDolly = currentCamera.GetComponent<CinemachineSplineDolly>();
        if (splineDolly == null) return;
        
        Vector3 currentOffset = splineDolly.SplineOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.x = xOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    /// <summary>
    /// Sets the Y component of the current camera's spline dolly offset.
    /// </summary>
    public void SetCurrentCameraYOffset(float yOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineSplineDolly splineDolly = currentCamera.GetComponent<CinemachineSplineDolly>();
        if (splineDolly == null) return;
        
        Vector3 currentOffset = splineDolly.SplineOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.y = yOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    /// <summary>
    /// Sets the Z component of the current camera's spline dolly offset.
    /// </summary>
    public void SetCurrentCameraZOffset(float zOffset)
    {
        if (currentCamera == null) return;
        
        CinemachineSplineDolly splineDolly = currentCamera.GetComponent<CinemachineSplineDolly>();
        if (splineDolly == null) return;
        
        Vector3 currentOffset = splineDolly.SplineOffset;
        Vector3 targetOffset = currentOffset;
        targetOffset.z = zOffset;
        
        SetCameraPathOffset(currentCamera, targetOffset);
    }
    
    /// <summary>
    /// Flips the current camera's Z offset (mirrors the camera position along the spline).
    /// </summary>
    public void FlipCurrentCamera()
    {
        if (currentCamera != null)
        {
            FlipCamera(currentCamera);
        }
    }
    
    private void FlipCamera(CinemachineVirtualCameraBase camera)
    {
        CinemachineSplineDolly splineDolly = camera.GetComponent<CinemachineSplineDolly>();
        
        if (splineDolly != null)
        {
            Vector3 currentOffset = splineDolly.SplineOffset;
            Vector3 targetOffset = currentOffset;
            targetOffset.z = -currentOffset.z;
            
            SetCameraPathOffset(camera, targetOffset);
        }
        else
        {
            Debug.LogWarning($"Camera {camera.name} does not have a CinemachineSplineDolly component!");
        }
    }
    
    private void SetCameraPathOffset(CinemachineVirtualCameraBase camera, Vector3 targetOffset, float duration = -1)
    {
        float tweenDuration = duration > 0 ? duration : flipDuration;
        
        CinemachineSplineDolly splineDolly = camera.GetComponent<CinemachineSplineDolly>();
        
        if (splineDolly != null)
        {
            Vector3 currentOffset = splineDolly.SplineOffset;
            Vector3 offsetValue = currentOffset;
            
            DOTween.To(() => offsetValue, 
                       x => {
                           offsetValue = x;
                           splineDolly.SplineOffset = offsetValue;
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
            Debug.LogWarning($"Camera {camera.name} does not have a CinemachineSplineDolly component!");
        }
    }
}
