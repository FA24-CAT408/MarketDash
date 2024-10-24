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

    // Start is called before the first frame update
    void Start()
    {
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();

        mainMenu.SetActive(true);
        inGameUI.SetActive(false);

        player.gameObject.SetActive(false);

        thirdPersonCamera.Priority = 0;
        mainMenuCamera.Priority = 10;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        mainMenu.SetActive(false);

        thirdPersonCamera.Priority = 10;
        mainMenuCamera.Priority = 0;

        // cause IsBlending has little bit delay so it's need to wait
        yield return new WaitUntil(() => _cinemachineBrain.IsBlending);

        // wait until blending is finished
        yield return new WaitUntil(() => !_cinemachineBrain.IsBlending);

        inGameUI.SetActive(true);
        player.gameObject.SetActive(true);
        Debug.Log("Game Started");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SwapUI()
    {
        mainMenu.GetComponent<Canvas>().enabled = !mainMenu.GetComponent<Canvas>().enabled;
        inGameUI.GetComponent<Canvas>().enabled = !inGameUI.GetComponent<Canvas>().enabled;
    }
}
