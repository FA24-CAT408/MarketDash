using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCameraBase mainMenuCamera;
    public CinemachineVirtualCameraBase thirdPersonCamera;
    
    [Header("UI Elements")]
    public GameObject mainMenuUI;
    public GameObject inGameUI;
    public GameObject gameOverUI;

    [Header("Main Menu UI Elements")] 
    public TMP_Text mainMenuBestTimeText;
    
    [Header("Game Over UI Elements")]
    public TMP_Text gameOverTimeText;
    public TMP_Text bestTimeText;
    
    // private Dictionary<GameManager.GameState, GameObject> stateToCanvas;
    
    private void Awake()
    {
        // Map states to their respective UIs
        // stateToCanvas = new Dictionary<GameManager.GameState, GameObject>
        // {
        //     { GameManager.GameState.MainMenu, mainMenuUI },
        //     { GameManager.GameState.PreGame, inGameUI },
        //     { GameManager.GameState.InProgress, inGameUI },
        //     { GameManager.GameState.EndGame, inGameUI },
        //     { GameManager.GameState.GameOver, gameOverUI }
        // };
    }
    
    // private void HandleStateChanged(GameManager.GameState newState)
    // {
    //     UpdateUI(newState);
    //     UpdateCamera(newState);
    // }

    // private void UpdateUI(GameManager.GameState state)
    // {
    //     // foreach (var canvas in stateToCanvas.Values)
    //     // {
    //     //     // Enable the canvas if it matches the state, disable otherwise
    //     //     canvas.SetActive(stateToCanvas[state] == canvas);
    //     // }
    //     //
    //     // switch (state)
    //     // {
    //     //     case GameManager.GameState.MainMenu:
    //     //         UpdateMainMenuUI();
    //     //         break;
    //     //     case GameManager.GameState.GameOver:
    //     //         UpdateGameOverUI();
    //     //         break;
    //     // }
    // }

    // private void UpdateCamera(GameManager.GameState state)
    // {
    //     switch (state)
    //     {
    //         case GameManager.GameState.MainMenu:
    //             SetCameraPriority(mainMenuCamera, 10);
    //             SetCameraPriority(thirdPersonCamera, 0);
    //             break;
    //         case GameManager.GameState.GameOver:
    //             SetCameraPriority(mainMenuCamera, 10);
    //             SetCameraPriority(thirdPersonCamera, 0);
    //             break;
    //         default:
    //             SetCameraPriority(mainMenuCamera, 0);
    //             SetCameraPriority(thirdPersonCamera, 10);
    //             break;
    //     }
    // }

    private void SetCameraPriority(CinemachineVirtualCameraBase camera, int priority)
    {
        if (camera != null)
        {
            camera.Priority = priority;
        }
    }
    
    private void UpdateMainMenuUI()
    {
        if (mainMenuBestTimeText != null)
        {
            // if(PlayerPrefs.HasKey("BestTime"))
            // {
            //     float bestTime = TimerManager.Instance.GetBestTime();
            //     mainMenuBestTimeText.text = $"Best Time: {TimerManager.Instance.GetFormattedTime(bestTime)}";
            // }
            // else
            // {
            //     mainMenuBestTimeText.text = "Best Time: --:--";
            // }
        }
    }
    
    private void UpdateGameOverUI()
    {
        if (gameOverTimeText != null && bestTimeText != null)
        {
            // TimerManager.Instance.SetBestTime();
            //
            // float finalTime = TimerManager.Instance.Timer; 
            // float bestTime = TimerManager.Instance.GetBestTime();
            //
            // gameOverTimeText.text = $"{TimerManager.Instance.GetFormattedTime(finalTime)}";
            // bestTimeText.text = $"Best Time: {TimerManager.Instance.GetFormattedTime(bestTime)}";
        }
    }

    
    public void StartGameButton()
    {
        // GameManager.Instance.ChangeState(GameManager.GameState.PreGame);
    }
    
    public void GameOverMainMenuBtn()
    {
        Debug.Log("GAME OVER Main Menu BTN PRESSED");
        Debug.Log("RELOADING THE SCENE");
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private void OnEnable()
    {
        // GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // GameManager.OnStateChanged -= HandleStateChanged;
    }
}
