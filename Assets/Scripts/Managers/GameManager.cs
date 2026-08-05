using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        LoadingIn,
        PreGame,
        InProgress,
        EndGame,
        GameOver,
        Pause
    }
    public static GameManager Instance { get; private set; }
    public static event Action<GameState> OnStateChanged;
    
    public int currentLevel = 1;
    
    [SerializeField] private GameSaveManager _gameSave;
    [SerializeField] private GameSettingsData _gameSettingsData;
    [SerializeField] private CinemachineCamera _playerCamera;
    [SerializeField] private CinemachineInputAxisController _cameraInputController;
    
    [SerializeField]
    private GameState currentState;
    private GameState previousState;
    
    [SerializeField]
    private bool setCursorVisible = true;
    
    private bool _hasDoubledSpeed = false;

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
            return;
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
        SetGroceryListManager(null);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find references to scene-specific components
        SetGroceryListManager(FindObjectOfType<GroceryListManager>());
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

        AssignSaveData();
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
        
        if (newState == GameState.Pause && currentState != GameState.Pause)
            previousState = currentState;
        
        CurrentState = newState;

        Debug.Log("CHANGING TO STATE: " + newState);

        switch (newState)
        {
            case GameState.LoadingIn:
                EnterLoadingInState();
                break;
            case GameState.PreGame:
                EnterPreGameState();
                break;
            case GameState.InProgress:
                EnterInProgressGameState();
                break;
            case GameState.EndGame:
                EnterEndGameState();
                break;
            case GameState.GameOver:
                EnterStopGameState();
                break;
            case GameState.Pause:
                EnterPauseGameState();
                break;
        }
    }
    
    public void EnterLoadingInState()
    {
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.SetMovementEnabled(false);
    }
    
    public void EnterPreGameState()
    {
        if (_sceneEventManager != null) 
            _sceneEventManager.OnPreGame?.Invoke();
        
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.SetMovementEnabled(true);

        Debug.Log("Entered PreGame");
    }
    
    public void EnterEndGameState()
    {
        Debug.Log("Game Over! EndGame state triggered.");
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null && !_hasDoubledSpeed)
        {
            player.MaxStableMoveSpeed *= 2f;
            _hasDoubledSpeed = true;
        }

        // Resume timer if it exists and isn't already active
        if (_timerManager != null && !_timerManager.TimerActive)
        {
            _timerManager.StartTimer();
        }
    
        if (_sceneEventManager != null)
            _sceneEventManager.OnEndGame?.Invoke();
    }

    public void EnterInProgressGameState()
    {
        UpdateCursorVisible(false);

        if (_groceryListManager != null)
            _groceryListManager.CreateAndShowList();
        
        if (_timerManager != null)
            _timerManager.StartTimer();
        
        if (_sceneEventManager != null)
            _sceneEventManager.OnGameInProgress?.Invoke();

        Debug.Log("Game Started");
    }

    private void EnterStopGameState()
    {
        UpdateCursorVisible(true);
        _hasDoubledSpeed = false;
        
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.SetMovementEnabled(false);
            
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

        var stagingArea = FindObjectOfType<StagingAreaController>();
        if (stagingArea != null)
        {
            StartCoroutine(stagingArea.SpawnItemsCoroutine());
        }
        
        Debug.Log("Level Beat");
    }

    private void EnterPauseGameState()
    {
        UpdateCursorVisible(true);
        
        var player = FindObjectOfType<KCCPlayerController>();
        if (player != null)
            player.SetMovementEnabled(false);
        
        if (_timerManager != null)
            _timerManager.StopTimer();
    }

    public void GameStart()
    {
        ChangeState(GameState.InProgress);
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }

    public void PauseGame()
    {
        if (currentState is GameState.InProgress or GameState.PreGame or GameState.EndGame)
            ChangeState(GameState.Pause);
    }

    public void UnpauseGame()
    {
        Debug.Log("UNPAUSING GAME");
        
        if (currentState == GameState.Pause && previousState != GameState.Pause && previousState != GameState.LoadingIn)
        {
            var player = FindObjectOfType<KCCPlayerController>();
            
            if (player != null)
                player.SetMovementEnabled(true);
            
            UpdateCursorVisible(false);
            
            ChangeState(previousState);
        }
    }
    
    public void ResetToLoadingState()
    {
        ChangeState(GameState.LoadingIn);
    }
    
    /// <summary>
    /// Loads a scene by name, transitioning through LoadingIn state.
    /// </summary>
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
    
    public void RegisterSceneEvents(SceneEventManager manager)
    {
        _sceneEventManager = manager;
    }

    public void UnregisterSceneEvents()
    {
        _sceneEventManager = null;
    }

    private void SetGroceryListManager(GroceryListManager manager)
    {
        if (_groceryListManager != null)
        {
            _groceryListManager.OrderCompleted -= HandleOrderCompleted;
        }

        _groceryListManager = manager;

        if (_groceryListManager != null)
        {
            _groceryListManager.OrderCompleted += HandleOrderCompleted;
        }
    }

    private void HandleOrderCompleted()
    {
        ChangeState(GameState.EndGame);
    }

    public void UpdateCursorVisible(bool visible)
    {
        setCursorVisible = visible;
    }

    void AssignSaveData()
    {
        // Sensitivity and invert are now controlled via CinemachineInputAxisController's Gain property.
        // Each controller entry maps to an axis on the camera (horizontal/vertical).
        if (_cameraInputController != null)
        {
            var controllers = _cameraInputController.Controllers;
            for (int i = 0; i < controllers.Count; i++)
            {
                var c = controllers[i];
                
                // Horizontal axis (orbit around target)
                if (i == 0)
                {
                    c.Input.Gain = 120f * _gameSettingsData.Sensitivity;
                    controllers[i] = c;
                }
                // Vertical axis (orbit up/down)
                else if (i == 1)
                {
                    float gain = _gameSettingsData.Sensitivity;
                    c.Input.Gain = _gameSettingsData.InvertCamera ? -gain : gain;
                    controllers[i] = c;
                }
            }
        }
        
        //Volume
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(_gameSettingsData.Volume);
    }
}
