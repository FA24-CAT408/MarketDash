using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using Obvious.Soap;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas inGameUI;
    public Canvas levelBeatUI;
    public Canvas pauseUI;
    
    [Header("In Game UI Elements")]
    public TMP_Text timerText;
    
    [Header("Level Beat UI Elements")]
    public TMP_Text levelBeatTimeText;
    public TMP_Text bestTimeText;
    
    [Header("Pause UI Elements")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeText;
    public Toggle invertCameraToggle;
    
    [Header("Transition Canvas")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;
    
    [Header("Save Data")]
    [SerializeField] private GameSaveManager _gameSave;
    [SerializeField] private GameSettingsManager _gameSettingsManager;
    
    [Header("Player")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private CinemachineFreeLook playerFreeLook;
    
    private Tween _currentFadeTween;
    private TimerManager _timerManager;

    public bool uiIsPaused;
    
    private void Awake()
    {
        // Subscribe to game state changes
        GameManager.OnStateChanged += HandleGameStateChanged;
        
        TimerManager.OnTimerUpdated += UpdateTimerDisplay;
        
        if (fadePanel != null)
        {
            fadePanel.alpha = 1f; // Start fully opaque
            fadePanel.gameObject.SetActive(true);
        }
    }
    
    private void Start()
    {
        _timerManager = FindObjectOfType<TimerManager>();
        
        // Initialize UI state based on current game state
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
        
        // --- SETTINGS UI HOOKUP ---
        if (_gameSettingsManager != null)
        {
            // Set UI values from saved settings
            sensitivitySlider.value = _gameSettingsManager.Sensitivity;
            musicVolumeSlider.value = _gameSettingsManager.Volume;
            invertCameraToggle.isOn = _gameSettingsManager.InvertCamera;
        }

        // Add listeners to update settings when UI changes
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        invertCameraToggle.onValueChanged.AddListener(OnInvertCameraChanged);

        _inputReader.PauseEvent += HandlePauseUI;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameManager.OnStateChanged -= HandleGameStateChanged;
        TimerManager.OnTimerUpdated -= UpdateTimerDisplay;
        
        _inputReader.PauseEvent -= HandlePauseUI;
        
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
    }

    void HandlePauseUI()
    {
        if(DebugController.Instance.ShowConsole)  return;
        
        uiIsPaused = !uiIsPaused;

        ShowPauseUI(uiIsPaused);

        if (GameManager.Instance != null)
        {
            if (uiIsPaused)
                GameManager.Instance.PauseGame();
            else
                GameManager.Instance.UnpauseGame();
        }
        
        if (sensitivityText != null)
        {
            sensitivityText.text = sensitivitySlider.value.ToString("F2");
        }
        
        if (musicVolumeText != null)
        {
            musicVolumeText.text = musicVolumeSlider.value.ToString("F2");
        }
        
        CinemachineInputProvider cinemachineInputProvider = playerFreeLook.transform.GetComponent<CinemachineInputProvider>();

        if (cinemachineInputProvider != null)
        {
            cinemachineInputProvider.enabled = !uiIsPaused;
        }
    }
    
    private void UpdateTimerDisplay(float time)
    {
        if (timerText != null && _timerManager != null)
        {
            timerText.text = _timerManager.GetFormattedTime(time);
        }
    }
    
    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.LoadingIn:
                ShowInGameUI(false);
                ShowLevelBeatUI(false);
                PerformFadeIn();
                break;
                
            case GameManager.GameState.PreGame:
                ShowInGameUI(false);
                ShowLevelBeatUI(false);
                break;
                
            case GameManager.GameState.InProgress:
                ShowInGameUI(true);
                ShowLevelBeatUI(false);
                break;
                
            case GameManager.GameState.GameOver:
                // ShowInGameUI(false);
                // ShowLevelBeatUI(true);
                // UpdateLevelBeatUI();
                break;
        }
    }
    
    public void PerformFadeIn()
    {
        if (fadePanel == null) 
        {
            // If no fade panel, just transition to PreGame immediately
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.PreGame);
            }
            return;
        }
        
        // Make sure the fade panel is active and fully opaque
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;
        
        // Kill any existing tween
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
        
        // Create the fade-in animation
        _currentFadeTween = fadePanel.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // When fade completes, transition to PreGame state
                fadePanel.gameObject.SetActive(false);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameManager.GameState.PreGame);
                }
            });
    }
    
    public void PerformFadeOut(TweenCallback onComplete = null)
    {
        if (fadePanel == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        // Make sure the fade panel is active
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;
        
        // Kill any existing tween
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
        
        // Create the fade-out animation
        _currentFadeTween = fadePanel.DOFade(1f, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                onComplete?.Invoke();
            });
    }

    public void ShowInGameUI(bool show)
    {
        if (inGameUI != null)
            inGameUI.gameObject.SetActive(show);
    }

    public void ShowLevelBeatUI(bool show)
    {
        if (levelBeatUI != null)
            levelBeatUI.gameObject.SetActive(show);
    }

    public void UpdateLevelBeatUI()
    {
        TimerManager timerManager = FindObjectOfType<TimerManager>();
        if (timerManager == null) return;
        
        // Update current time display
        if (levelBeatTimeText != null)
        {
            float currentTime = timerManager.GetCurrentTime();
            levelBeatTimeText.text = $"{timerManager.GetFormattedTime(currentTime)}";
        }
        
        // Update best time display
        if (bestTimeText != null)
        {
            float currentTime = timerManager.GetCurrentTime();
            
            // Check if this is the first completion or a new best time
            if (!_gameSave.IsLevelCompleted(GameManager.Instance.currentLevel) || 
                currentTime < _gameSave.GetLevelTime(GameManager.Instance.currentLevel))
            {
                bestTimeText.text = $"New Best Time: {timerManager.GetFormattedTime(currentTime)}";
            }
            else if (_gameSave.IsLevelCompleted(GameManager.Instance.currentLevel))
            {
                float bestTime = _gameSave.GetLevelTime(GameManager.Instance.currentLevel);
                bestTimeText.text = $"Best Time: {timerManager.GetFormattedTime(bestTime)}";
            }
            else
            {
                bestTimeText.text = "No Best Time Yet";
            }
        }
    }

    public void ShowPauseUI(bool show)
    {
        if(pauseUI != null)
            pauseUI.gameObject.SetActive(show);
        
        if (show && _gameSettingsManager != null)
        {
            sensitivitySlider.value = _gameSettingsManager.Sensitivity;
            musicVolumeSlider.value = _gameSettingsManager.Volume;
            invertCameraToggle.isOn = _gameSettingsManager.InvertCamera;
        }
    }

    public void PauseBtn()
    {
        HandlePauseUI();
    }
    
    public void MainMenuBtn()
    {
        Debug.Log("GAME OVER Main Menu BTN PRESSED");
    
        PerformFadeOut(() => {
            SceneManager.LoadScene("Main Menu");

            FindObjectOfType<GameManager>().currentLevel = 0;
        });
    }

    public void RestartLevelBtn()
    {
        Debug.Log("Restart Level BTN PRESSED");
    
        PerformFadeOut(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            FindObjectOfType<GameManager>().currentLevel -= 1;
        });
    }

    public void NextLevelBtn()
    {
        Debug.Log("Next Level BTN PRESSED");

        PerformFadeOut(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextScene();
            }
            else
            {
                // Fallback if GameManager is not available
                int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextSceneIndex);
                }
                else
                {
                    SceneManager.LoadScene("Main Menu");
                }
            }
        });
    }
    
    private void OnSensitivityChanged(float value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.Sensitivity = value;
        
        if (playerFreeLook != null)
        {
            playerFreeLook.m_XAxis.m_MaxSpeed = 120f * value;
            playerFreeLook.m_YAxis.m_MaxSpeed = 1f * value;
        }

        if (sensitivityText != null)
        {
            float normalized = 0.1f + ((value - 0.1f) / (2f - 0.1f)) * (1f - 0.1f);
            normalized = Mathf.Clamp(normalized, 0.1f, 1f);
            sensitivityText.text = normalized.ToString("F2");
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.SetVolume(value);
        
        if (musicVolumeText != null)
        {
            musicVolumeText.text = value.ToString("F2");
        }
    }

    private void OnInvertCameraChanged(bool value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.InvertCamera = value;
        
        if (playerFreeLook != null)
        {
            playerFreeLook.m_YAxis.m_InvertInput = value;
        }
    }

}
