using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cinemachine;
using DG.Tweening;
using Obvious.Soap;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        LoadingIn,
        PreGame,
        InProgress,
        EndGame,
        GameOver
    }
    public static GameManager Instance { get; private set; }
    public static event Action<GameState> OnStateChanged;
    
    public int currentLevel = 0;
    
    [SerializeField] private GameSaveManager _gameSave;
    
    [SerializeField]
    private GameState currentState;
    
    [SerializeField]
    private bool setCursorVisible = true;

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
    private SceneEventManager _sceneEventManager;
    
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find references to scene-specific components
        _groceryListManager = FindObjectOfType<GroceryListManager>();
        _timerManager = FindObjectOfType<TimerManager>();
        _sceneEventManager = FindObjectOfType<SceneEventManager>();
        
        // Initialize the scene based on current state
        ChangeState(GameState.LoadingIn);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GAME MANAGER START METHOD");
        
        ChangeState(GameState.LoadingIn);
    }

    // Update is called once per frame
    void Update()
    {
        Cursor.visible = setCursorVisible;
        Cursor.lockState = setCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        
        if (SceneManager.GetActiveScene().name == "Main Menu") currentLevel = 0;
    }
    
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        CurrentState = newState;

        Debug.Log("CHANGING TO STATE: " + newState);

        switch (newState)
        {
            case GameState.LoadingIn:
                EnterLoadingIn();
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
    
    public void EnterLoadingIn()
    {
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.canMove = false;
    }
    
    public void EnterPreGame()
    {
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.canMove = true;

        Debug.Log("Entered PreGame");
    }
    
    public void EnterEndGame()
    {
        Debug.Log("Game Over! EndGame state triggered.");
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.MaxStableMoveSpeed *= 2f;
        
        if (_sceneEventManager != null)
            _sceneEventManager.OnEndGame?.Invoke();
    }


    public void StartGame()
    {
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.canMove = true;
        
        UpdateCursorVisible(false);
        
        // _freeLookCamera.GetComponent<CinemachineInputProvider>().enabled = true;
        
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        _groceryListManager.CreateAndShowList();
        
        if (_timerManager != null)
            _timerManager.StartTimer();
        
        if (_sceneEventManager != null)
            _sceneEventManager.OnGameStart?.Invoke();

        Debug.Log("Game Started");
    }

    private void StopGame()
    {
        UpdateCursorVisible(true);
        
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.canMove = false;
            
        if (_timerManager != null)
            _timerManager.StopTimer();
        
        // Get the final time and pass it to the event
        float finalTime = _timerManager != null ? _timerManager.GetCurrentTime() : 0f;
        
        if (_sceneEventManager != null)
        {
            _sceneEventManager.OnGameOver?.Invoke();
            _sceneEventManager.OnLevelComplete?.Invoke(finalTime);
        }
        
        _gameSave.SetLevelTime(currentLevel, finalTime);

        currentLevel += 1;
        
        Debug.Log("Level Beat");
    }

    public void GameStart()
    {
        ChangeState(GameState.InProgress);
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }
    
    public void ResetToLoadingState()
    {
        ChangeState(GameState.LoadingIn);
    }
    
    // Add this method to GameManager
    public void LoadScene(string sceneName)
    {
        // Set state to LoadingIn before loading the scene
        ChangeState(GameState.LoadingIn);
    
        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNextScene()
    {
        // Set state to LoadingIn
        ChangeState(GameState.LoadingIn);
    
        // Get the next scene index
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
    
        // Check if there is a next scene
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // If there's no next level, go back to main menu
            SceneManager.LoadScene("Main Menu");
        }
    }
    
    public void RespawnPlayer(Transform spawnPoint)
    {
        StartCoroutine(RespawnRoutine(spawnPoint));
    }
    
    public void RegisterSceneEvents(SceneEventManager manager)
    {
        _sceneEventManager = manager;
    }

    public void UnregisterSceneEvents()
    {
        _sceneEventManager = null;
    }

    public void UpdateCursorVisible(bool visible)
    {
        setCursorVisible = visible;
    }

    private IEnumerator RespawnRoutine(Transform spawnPoint)
    {
        KCCPlayerController player = FindObjectOfType<KCCPlayerController>();
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("No valid spawn point provided for respawn.");
            yield break;
        }

        // Disable player controls and camera
        Debug.Log("Disabling player...");
        player.enabled = false;
        player.gameObject.SetActive(false);
        
        // TimerManager.Instance.AddTime(5f);

        // _freeLookCamera.m_LookAt = spawnPoint;
        // _freeLookCamera.m_Follow = spawnPoint;

        // Add a short delay
        yield return new WaitForSeconds(1f);

        // Move player to the spawn point
        Debug.Log($"Respawning player at: {spawnPoint.position}");
        player.transform.position = spawnPoint.position;

        // Re-enable player controls and camera
        Debug.Log("Re-enabling player...");
        player.gameObject.SetActive(true);
        player.enabled = true;
        // _freeLookCamera.m_LookAt = player.transform.GetChild(0);
        // _freeLookCamera.m_Follow = player.transform.GetChild(0);
    }
}
