using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }
    
    public CinemachineVirtualCamera currentCamera;
    
    public List<CinemachineVirtualCamera> virtualCameras;
    
    private void Awake()
    {
        // Check if an instance already exists and destroy this one if it does
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        if (virtualCameras.Count > 0 && currentCamera == null)
        {
            currentCamera = virtualCameras[0];
            currentCamera.Priority = 100;
        }
    }

    public void SetNewCamera(CinemachineVirtualCamera newCamera)
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
}
