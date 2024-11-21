using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera mainMenuCamera;
    public CinemachineFreeLook thirdPersonCamera;
    
    [Header("UI Elements")]
    public GameObject mainMenuUI;
    public GameObject inGameUI;
    
    private PlayerStateMachine _player;
    
    public void OnStartGameButton()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PreGame);
    }
    // Start is called before the first frame update
    void Start()
    {
        _player = FindObjectOfType<PlayerStateMachine>();
        
        // SwapUI();

        // player.gameObject.SetActive(false);

        // thirdPersonCamera.Priority = 0;
        // mainMenuCamera.Priority = 10;

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        // GroceryListManager.Instance.GetNewOrder(3);

        // StartGame();

        // isGameActive = false;
    }

    // public void StartGame()
    // {
    //     if (!isGameActive)
    //     {
    //         isGameActive = true;
    //         StartCoroutine(StartGameCoroutine());
    //     }
    // }

    IEnumerator StartGameCoroutine()
    {
        SwapUI();

        yield return new WaitForSeconds(1f);

        Debug.Log("Game Started");

        _player.gameObject.SetActive(true);
        Debug.Log("Game Started");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GroceryListManager.Instance.GetNewOrder(3);
    }

    public void SwapUI()
    {
        mainMenuUI.GetComponent<Canvas>().enabled = !mainMenuUI.GetComponent<Canvas>().enabled;
        inGameUI.GetComponent<Canvas>().enabled = !inGameUI.GetComponent<Canvas>().enabled;

        thirdPersonCamera.Priority = inGameUI.GetComponent<Canvas>().enabled ? 10 : 0;
        mainMenuCamera.Priority = mainMenuUI.GetComponent<Canvas>().enabled ? 10 : 0;
    }
}
