using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenu;
    public CinemachineVirtualCamera mainMenuCamera;

    [Header("In Game UI")]
    public GameObject inGameUI;
    public PlayerStateMachine player;
    public CinemachineFreeLook thirdPersonCamera;

    CinemachineBrain _cinemachineBrain;

    public bool isGameActive;

    // Start is called before the first frame update
    void Start()
    {
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();

        // SwapUI();

        // player.gameObject.SetActive(false);

        // thirdPersonCamera.Priority = 0;
        // mainMenuCamera.Priority = 10;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GroceryListManager.Instance.GetNewOrder(3);

        // StartGame();

        // isGameActive = false;
    }

    public void StartGame()
    {
        if (!isGameActive)
        {
            isGameActive = true;
            StartCoroutine(StartGameCoroutine());
        }
    }

    IEnumerator StartGameCoroutine()
    {
        SwapUI();

        yield return new WaitForSeconds(1f);

        Debug.Log("Game Started");

        player.gameObject.SetActive(true);
        Debug.Log("Game Started");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GroceryListManager.Instance.GetNewOrder(3);
    }

    public void SwapUI()
    {
        mainMenu.GetComponent<Canvas>().enabled = !mainMenu.GetComponent<Canvas>().enabled;
        inGameUI.GetComponent<Canvas>().enabled = !inGameUI.GetComponent<Canvas>().enabled;

        thirdPersonCamera.Priority = inGameUI.GetComponent<Canvas>().enabled ? 10 : 0;
        mainMenuCamera.Priority = mainMenu.GetComponent<Canvas>().enabled ? 10 : 0;
    }
}
