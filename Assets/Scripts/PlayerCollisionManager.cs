using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCollisionManager : MonoBehaviour
{
    public UnityEvent startTimer;
    public UnityEvent stopTimer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Start Timer")
        {
            startTimer.Invoke();
        }
        else if (other.gameObject.name == "Stop Timer")
        {
            stopTimer.Invoke();
        }
    }
}
