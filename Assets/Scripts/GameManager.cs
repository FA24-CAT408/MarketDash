using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event Action<GameState> OnStateChanged;

    public enum GameState
    {
        MainMenu,
        PreGame,
        InProgress,
        EndGame,
        GameOver
    }
    
    [SerializeField]
    private GameState currentState;

    public GameState CurrentState
    {
        get => currentState;
        private set
        {
            currentState = value;
            OnStateChanged?.Invoke(currentState);
        }
    }
    
    // [Header("Game Components")]
    private GroceryListManager _groceryListManager;
    private TimerManager _timerManager;
    private PlayerStateMachine _playerStateMachine;
    private CinemachineFreeLook _freeLookCamera;
    
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
        _groceryListManager = FindObjectOfType<GroceryListManager>();
        _timerManager = FindObjectOfType<TimerManager>();
        _playerStateMachine = FindObjectOfType<PlayerStateMachine>();
        _freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        
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
        _playerStateMachine.gameObject.SetActive(false);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Entered Main Menu");
    }
    
    private void EnterPreGame()
    {
        _playerStateMachine.gameObject.SetActive(true);
        _timerManager.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Entered PreGame");
    }
    
    private void EnterEndGame()
    {
        //TODO: SET EXIT AS TARGET HIGHLIGHTED IN MINIMAP

        Debug.Log("Game Over! EndGame state triggered.");
    }


    private void StartGame()
    {
        _playerStateMachine.enabled = true;
        _freeLookCamera.GetComponent<CinemachineInputProvider>().enabled = true;
        
        _timerManager.gameObject.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _groceryListManager.ResetLists();
        _groceryListManager.GetNewOrder(1, Random.Range(1, _groceryListManager.allAvailableItems.Count + 1));
        
        _timerManager.StartTimer();

        Debug.Log("Game Started");
    }

    private void StopGame()
    {
        _playerStateMachine.enabled = false;
        _freeLookCamera.GetComponent<CinemachineInputProvider>().enabled = false;

        TimerManager.Instance.StopTimer();

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
    
    public void RespawnPlayer(Transform spawnPoint)
    {
        StartCoroutine(RespawnRoutine(spawnPoint));
    }

    private IEnumerator RespawnRoutine(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("No valid spawn point provided for respawn.");
            yield break;
        }

        // Disable player controls and camera
        Debug.Log("Disabling player...");
        _playerStateMachine.enabled = false;
        _playerStateMachine.gameObject.SetActive(false);
        
        TimerManager.Instance.AddTime(5f);

        _freeLookCamera.m_LookAt = spawnPoint;
        _freeLookCamera.m_Follow = spawnPoint;

        // Add a short delay
        yield return new WaitForSeconds(1f);

        // Move player to the spawn point
        Debug.Log($"Respawning player at: {spawnPoint.position}");
        _playerStateMachine.transform.position = spawnPoint.position;

        // Re-enable player controls and camera
        Debug.Log("Re-enabling player...");
        _playerStateMachine.gameObject.SetActive(true);
        _playerStateMachine.enabled = true;
        _freeLookCamera.m_LookAt = _playerStateMachine.transform.GetChild(0);
        _freeLookCamera.m_Follow = _playerStateMachine.transform.GetChild(0);
    }
}
