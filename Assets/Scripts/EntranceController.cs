using UnityEngine;
using UnityEngine.Events;

public class EntranceController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            switch (GameManager.Instance.CurrentState)
            {
                case GameManager.GameState.PreGame:
                    GameManager.Instance.ChangeState(GameManager.GameState.InProgress);
                    break;
                case GameManager.GameState.InProgress:
                    if (GroceryListManager.Instance.IsOrderComplete())
                    {
                        GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
                    }
                    break;
            }
        }
    }
}
