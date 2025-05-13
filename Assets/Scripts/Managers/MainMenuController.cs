using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [Header("Canvases")]
    public Canvas mainMenuCanvas;
    public Canvas settingsCanvas;
    
    [Header("Settings UI Elements")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public Slider volumeSlider;
    public TMP_Text volumeText;
    public Toggle invertCameraToggle;
    
    [Header("Transition")]
    public CanvasGroup transitionImage;
    public float fadeDuration = 1.5f;
    
    [Header("Settings")]
    [SerializeField] private GameSettingsManager _gameSettingsManager;
    [SerializeField] private GameSaveManager _gameSaveManager;
    
    private Tween _currentFadeTween;
    
    private void Start()
    {
        // Initialize UI
        ShowMainMenu(true);
        ShowSettings(false);
        
        // If we have transition image, start with fade-in
        if (transitionImage != null)
        {
            transitionImage.alpha = 1f;
            PerformFadeIn();
        }
        
        // Connect UI elements if we have a settings manager
        if (_gameSettingsManager != null)
        {
            // Initialize UI values from saved settings
            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = _gameSettingsManager.Sensitivity;
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }
            
            if (volumeSlider != null)
            {
                volumeSlider.value = _gameSettingsManager.Volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            
            if (invertCameraToggle != null)
            {
                invertCameraToggle.isOn = _gameSettingsManager.InvertCamera;
                invertCameraToggle.onValueChanged.AddListener(OnInvertCameraChanged);
            }
        }
    }
    
    private void OnDestroy()
    {
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
    }
    
    // Show or hide the main menu canvas
    public void ShowMainMenu(bool show)
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(show);
    }
    
    // Show or hide the settings canvas
    public void ShowSettings(bool show)
    {
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(show);
            
        if (show && _gameSettingsManager != null)
        {
            // Update UI elements with current settings values
            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = _gameSettingsManager.Sensitivity;
                if (sensitivityText != null)
                {
                    sensitivityText.text = sensitivitySlider.value.ToString("F2");
                }
            }
            
            if (volumeSlider != null)
            {
                volumeSlider.value = _gameSettingsManager.Volume;
                if (volumeText != null)
                {
                    volumeText.text = volumeSlider.value.ToString("F2");
                }
            }
            
            if (invertCameraToggle != null)
            {
                invertCameraToggle.isOn = _gameSettingsManager.InvertCamera;
            }
        }
    }
    
    // Called when Play button is clicked
    public void PlayGame()
    {
        // If we have a transition, use it when loading the next scene
        if (transitionImage != null)
        {
            PerformFadeOut(() => {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            });
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetGame()
    {
        _gameSaveManager.ResetAllTimes();
    }
    
    // Called when Settings button is clicked
    public void OpenSettings()
    {
        ShowMainMenu(false);
        ShowSettings(true);
    }
    
    // Called when Back button in Settings is clicked
    public void CloseSettings()
    {
        ShowSettings(false);
        ShowMainMenu(true);
    }
    
    // Fade-in animation
    public void PerformFadeIn()
    {
        if (transitionImage == null) return;
        
        // Make sure the transition image is active and fully opaque
        transitionImage.gameObject.SetActive(true);
        transitionImage.alpha = 1f;
        
        // Kill any existing tween
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
        
        // Create the fade-in animation
        _currentFadeTween = transitionImage.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transitionImage.gameObject.SetActive(false);
            });
    }
    
    // Fade-out animation
    public void PerformFadeOut(TweenCallback onComplete = null)
    {
        if (transitionImage == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        // Make sure the transition image is active
        transitionImage.gameObject.SetActive(true);
        transitionImage.alpha = 0f;
        
        // Kill any existing tween
        if (_currentFadeTween != null)
        {
            _currentFadeTween.Kill();
        }
        
        // Create the fade-out animation
        _currentFadeTween = transitionImage.DOFade(1f, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                onComplete?.Invoke();
            });
    }
    
    // Called when sensitivity slider value changes
    private void OnSensitivityChanged(float value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.Sensitivity = value;
        
        if (sensitivityText != null)
        {
            // float normalized = 0.1f + ((value - 0.1f) / (5f - 0.1f)) * (1f - 0.1f);
            // normalized = Mathf.Clamp(normalized, 0.1f, 1f);
            sensitivityText.text = value.ToString("F2");
        }
    }
    
    // Called when volume slider value changes
    private void OnVolumeChanged(float value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.SetVolume(value);
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
        
        if (volumeText != null)
        {
            volumeText.text = value.ToString("F2");
        }
    }
    
    // Called when invert camera toggle value changes
    private void OnInvertCameraChanged(bool value)
    {
        if (_gameSettingsManager != null)
            _gameSettingsManager.InvertCamera = value;
    }
}
