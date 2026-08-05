using UnityEngine;
using UnityEngine.Events;

public class EntranceController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && GameManager.Instance.CurrentState == GameManager.GameState.PreGame)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.InProgress);
        }
    }
}
