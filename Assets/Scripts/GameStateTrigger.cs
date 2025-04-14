using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateTrigger : MonoBehaviour
{
    [SerializeField] private GameManager.GameState requiredState;
    [SerializeField] private GameManager.GameState targetState;
    [SerializeField] private bool triggerOnce = false;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        
        if (other.CompareTag("Player") && GameManager.Instance.CurrentState == requiredState)
        {
            GameManager.Instance.ChangeState(targetState);
            hasTriggered = true;
        }
    }
}
