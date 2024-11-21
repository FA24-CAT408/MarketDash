using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        PreGame,
        InProgress,
        EndGame,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    
    // [Header("Game Components")]
    private MainMenu mainMenu;
    private GroceryListManager groceryListManager;
    private TimerManager timerManager;
    private PlayerStateMachine playerStateMachine;
    
    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        mainMenu = FindObjectOfType<MainMenu>();
        groceryListManager = FindObjectOfType<GroceryListManager>();
        timerManager = FindObjectOfType<TimerManager>();
        playerStateMachine = FindObjectOfType<PlayerStateMachine>();
        
        ChangeState(GameState.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                EnterMainMenu();
                break;
            case GameState.PreGame:
                EnterPreGame();
                break;
            case GameState.InProgress:
                StartGame();
                break;
            case GameState.EndGame:
                EnterEndGame();
                break;
            case GameState.GameOver:
                StopGame();
                break;
        }
    }
    
    private void EnterMainMenu()
    {
        mainMenu.SwapUI();
        playerStateMachine.gameObject.SetActive(false);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Entered Main Menu");
    }
    
    private void EnterPreGame()
    {
        mainMenu.SwapUI();
        
        playerStateMachine.gameObject.SetActive(true);
        timerManager.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Entered PreGame");
    }
    
    private void EnterEndGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Over! EndGame state triggered.");
    }


    private void StartGame()
    {
        mainMenu.gameObject.SetActive(false);
        timerManager.gameObject.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        groceryListManager.ResetLists();
        groceryListManager.GetNewOrder(1, 1);
        
        timerManager.StartTimer();

        Debug.Log("Game Started");
    }

    private void StopGame()
    {
        mainMenu.gameObject.SetActive(false);
        timerManager.StopTimer();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Over");
    }

    public void GameStart()
    {
        ChangeState(GameState.InProgress);
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }
}
