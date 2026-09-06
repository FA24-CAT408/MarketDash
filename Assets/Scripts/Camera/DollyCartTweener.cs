using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using DG.Tweening;
using UnityEngine;

public class DollyCartTweener : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float tweenDuration = 2f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;
    
    private CinemachineSplineDolly _splineDolly;
    
    /// <summary>
    /// Tweens the dolly spline position from 1 to 0, then restores the default camera.
    /// </summary>
    public void TweenDollyPosition()
    {
        Debug.Log("TWEEN DOLLY");

        virtualCamera.Priority.Value = 100;
        
        // Tween between position 0 and 1
        DOTween.To(() => _splineDolly.CameraPosition,
                x => _splineDolly.CameraPosition = x,
                0f,
                tweenDuration)
            .From(1f)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                GameManager.Instance.SetPlayerMovementEnabled(true);
                
                FindObjectOfType<CameraSystem>().SetCameraByIndex(0);
                
                virtualCamera.Priority.Value = -10;
            });
    }
    
    private void Start()
    {
        _splineDolly = virtualCamera.GetComponent<CinemachineSplineDolly>();
    }
}
