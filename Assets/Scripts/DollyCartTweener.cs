using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

public class DollyCartTweener : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float tweenDuration = 2f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;
    
    private CinemachineTrackedDolly _trackedDolly;
    
    public void TweenDollyPosition()
    {
        Debug.Log("TWEEN DOLLY");

        virtualCamera.m_Priority = 100;
        
        // Tween between position 0 and 1
        DOTween.To(() => _trackedDolly.m_PathPosition,
                x => _trackedDolly.m_PathPosition = x,
                0f,
                tweenDuration)
            .From(1f)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                FindObjectOfType<KCCPlayerController>().TogglePlayerCanMove();
                
                FindObjectOfType<CameraSystem>().SetCameraByIndex(0);
                
                virtualCamera.m_Priority = -10;
            });
    }
    
    private void Start()
    {
        _trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
    }
}
