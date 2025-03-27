using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

[System.Serializable]
public class CameraActivatedEvent : UnityEvent<int> { }

public class CameraSystemV2 : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] vCams;
    
    // Tracks the activation order (most recently activated = highest index)
    private List<CinemachineVirtualCamera> activationOrder = new List<CinemachineVirtualCamera>();
    
    // Base priority value
    [SerializeField] private int basePriority = 10;
    
    // Priority increment between cameras
    [SerializeField] private int priorityStep = 1;

    // Events
    [Header("Events")]
    public CameraActivatedEvent onCameraActivated;
    public UnityEvent<CinemachineVirtualCamera> onCameraActivatedReference;
    public UnityEvent<int, CinemachineVirtualCamera> onCameraActivatedFull;

    private void Start()
    {
        // Initialize all cameras with low priority
        foreach (var cam in vCams)
        {
            cam.Priority = 0;
        }
        
        // Initialize events if null
        if (onCameraActivated == null)
            onCameraActivated = new CameraActivatedEvent();
        if (onCameraActivatedReference == null)
            onCameraActivatedReference = new UnityEvent<CinemachineVirtualCamera>();
        if (onCameraActivatedFull == null)
            onCameraActivatedFull = new UnityEvent<int, CinemachineVirtualCamera>();
    }

    public void ActivateCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= vCams.Length)
            return;
            
        var targetCam = vCams[cameraIndex];
        
        // If camera is already in the list, remove it first
        if (activationOrder.Contains(targetCam))
        {
            activationOrder.Remove(targetCam);
        }
        
        // Add the camera to the end of the list (highest priority position)
        activationOrder.Add(targetCam);
        
        // Update priorities for all cameras in the activation order
        UpdateCameraPriorities();
        
        // Invoke events
        onCameraActivated.Invoke(cameraIndex);
        onCameraActivatedReference.Invoke(targetCam);
        onCameraActivatedFull.Invoke(cameraIndex, targetCam);
    }
    
    private void UpdateCameraPriorities()
    {
        // Reset all cameras to zero priority first
        foreach (var cam in vCams)
        {
            cam.Priority = 0;
        }
        
        // Assign priorities based on activation order
        for (int i = 0; i < activationOrder.Count; i++)
        {
            // Each camera gets progressively higher priority
            activationOrder[i].Priority = basePriority + (i * priorityStep);
        }
    }
    
    // Optional: Method to deactivate a specific camera
    public void DeactivateCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= vCams.Length)
            return;
            
        var targetCam = vCams[cameraIndex];
        
        if (activationOrder.Contains(targetCam))
        {
            activationOrder.Remove(targetCam);
            UpdateCameraPriorities();
        }
    }
    
    // Helper method to get camera by index (useful for UI events)
    public void ActivateCameraByButton(int cameraIndex)
    {
        ActivateCamera(cameraIndex);
    }
    
    // Get the currently active camera index
    public int GetActiveCamera()
    {
        if (activationOrder.Count == 0)
            return -1;
            
        var activeCamera = activationOrder[activationOrder.Count - 1];
        return System.Array.IndexOf(vCams, activeCamera);
    }
}
