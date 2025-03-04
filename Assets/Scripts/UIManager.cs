using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas inGameUI;
    public Canvas levelBeatUI;
    public Canvas transitionCanvas;
    
    [Header("In Game UI Elements")]
    public TMP_Text timerText;
    
    [Header("Level Beat UI Elements")]
    public TMP_Text levelBeatTimeText;
    public TMP_Text bestTimeText;
    
    [Header("Transition Canvas")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;
    
    private Tween _currentFadeTween;
    
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
        // Initialize UI state based on current game state
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameManager.OnStateChanged -= HandleGameStateChanged;
        TimerManager.OnTimerUpdated -= UpdateTimerDisplay;
        
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
    }
    
    private void UpdateTimerDisplay(float time)
    {
        if (timerText != null && TimerManager.Instance != null)
        {
            timerText.text = TimerManager.Instance.GetFormattedTime(time);
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
                
            case GameManager.GameState.EndGame:
            case GameManager.GameState.GameOver:
                ShowInGameUI(false);
                ShowLevelBeatUI(true);
                UpdateLevelBeatUI();
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
            float bestTime = timerManager.GetBestTime();
            
            // Check if this is a new best time
            if (timerManager.GetCurrentTime() < bestTime)
            {
                timerManager.SetBestTime();
                bestTime = timerManager.GetCurrentTime();
                bestTimeText.text = $"New Best Time: {timerManager.GetFormattedTime(bestTime)}";
            }
            else if (bestTime < float.MaxValue)
            {
                bestTimeText.text = $"Best Time: {timerManager.GetFormattedTime(bestTime)}";
            }
            else
            {
                bestTimeText.text = "No Best Time Yet";
            }
        }
    }
    
    public void LevelBeatMainMenuBtn()
    {
        Debug.Log("GAME OVER Main Menu BTN PRESSED");
    
        PerformFadeOut(() => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene("Main Menu");
            }
            else
            {
                SceneManager.LoadScene("Main Menu");
            }
        });
    }

    public void RestartLevelBtn()
    {
        Debug.Log("Restart Level BTN PRESSED");
    
        PerformFadeOut(() => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartCurrentScene();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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

    private void SetCameraPriority(CinemachineVirtualCameraBase camera, int priority)
    {
        if (camera != null)
        {
            camera.Priority = priority;
        }
    }
}
