using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCollisionManager : MonoBehaviour
{
    public UnityEvent startGame;
    public UnityEvent stopGame;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Start Game")
        {
            Debug.Log("START GAME");
            startGame.Invoke();
        }
        else if (other.gameObject.name == "Stop Game")
        {
            stopGame.Invoke();
        }
        else if (other.gameObject.CompareTag("Item"))
        {
            other.GetComponent<Item>().Interact();
        }
    }
}
