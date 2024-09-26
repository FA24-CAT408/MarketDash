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
    public CinemachineFreeLook thirdPersonCamera;
    public ThirdPersonCam thirdPersonCameraScript;
    public PlayerMovementTutorial playerMovementTutorial;

    // Start is called before the first frame update
    void Start()
    {
        mainMenu.SetActive(true);
        inGameUI.SetActive(false);
        playerMovementTutorial.enabled = false;
        thirdPersonCameraScript.enabled = false;
        thirdPersonCamera.Priority = 0;
        mainMenuCamera.Priority = 10;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);
        inGameUI.SetActive(true);
        playerMovementTutorial.enabled = true;
        thirdPersonCameraScript.enabled = true;
        thirdPersonCamera.Priority = 10;
        mainMenuCamera.Priority = 0;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
