using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    // [SerializeField] private AudioSource sfxSource;
    
    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip endGameMusic;
    
    [Header("Audio Settings")]
    [SerializeField] private GameSettingsManager _gameSettingsManager;
    
    private AudioClip _currentMusicClip;
    private Tween _fadeInTween;
    private Tween _fadeOutTween;
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create audio sources if not assigned in inspector
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        // if (sfxSource == null)
        // {
        //     sfxSource = gameObject.AddComponent<AudioSource>();
        //     sfxSource.loop = false;
        //     sfxSource.playOnAwake = false;
        // }
    }
    
    private void Start()
    {
        // Subscribe to game state changes
        GameManager.OnStateChanged += HandleGameStateChanged;
        
        // Set initial volume
        if (_gameSettingsManager != null)
        {
            SetMusicVolume(_gameSettingsManager.Volume);
        }
        else
        {
            // Try to find the settings manager if not assigned
            _gameSettingsManager = FindObjectOfType<GameSettingsManager>();
            if (_gameSettingsManager != null)
            {
                SetMusicVolume(_gameSettingsManager.Volume);
            }
        }
        
        // Start playing appropriate music based on current scene
        PlayMusicForCurrentScene();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameManager.OnStateChanged -= HandleGameStateChanged;
        
        if (_fadeInTween != null)
            _fadeInTween.Kill();
            
        if (_fadeOutTween != null)
            _fadeOutTween.Kill();
    }
    
    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.LoadingIn:
                // Keep current music during loading
                break;
            case GameManager.GameState.PreGame:
                CrossFadeToMusic(gameplayMusic);
                break;
            case GameManager.GameState.InProgress:
            case GameManager.GameState.EndGame:
                CrossFadeToMusic(endGameMusic, 0.15f);
                break;
            case GameManager.GameState.GameOver:
                CrossFadeToMusic(gameplayMusic);
                break;
            case GameManager.GameState.Pause:
                // You could lower volume during pause or just keep it playing
                break;
        }
    }
    
    public void PlayMusicForCurrentScene()
    {
        // Determine which music to play based on the current scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main Menu")
        {
            PlayMusic(mainMenuMusic);
        }
        else
        {
            // For level scenes
            PlayMusic(gameplayMusic);
        }
    }
    
    public void PlayMusic(AudioClip musicClip, bool fade = true)
    {
        if (musicClip == null) return;
        
        if (musicClip == _currentMusicClip && musicSource.isPlaying)
            return;
            
        _currentMusicClip = musicClip;
        
        if (fade)
        {
            StartCoroutine(FadeMusicIn(musicClip));
        }
        else
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }
    
    public void CrossFadeToMusic(AudioClip newClip, float crossfadeTime = 0.2f)
    {
        if (newClip == null || newClip == _currentMusicClip) return;
        
        StartCoroutine(CrossFadeMusicCoroutine(newClip, crossfadeTime));
    }
    
    private IEnumerator CrossFadeMusicCoroutine(AudioClip newClip, float crossFade = 0.2f)
    {
        if (musicSource.isPlaying)
        {
            // Fade out current music
            if (_fadeOutTween != null)
                _fadeOutTween.Kill();
                
            _fadeOutTween = DOTween.To(() => musicSource.volume, 
                x => musicSource.volume = x, 0, crossFade / 2)
                .SetEase(Ease.OutQuad);
                
            yield return _fadeOutTween.WaitForCompletion();
            musicSource.Stop();
        }
        
        // Switch clip and fade in
        musicSource.clip = newClip;
        _currentMusicClip = newClip;
        
        musicSource.volume = 0f;
        musicSource.Play();
        
        if (_fadeInTween != null)
            _fadeInTween.Kill();
            
        float targetVolume = _gameSettingsManager != null ? _gameSettingsManager.Volume : 1f;
        _fadeInTween = DOTween.To(() => musicSource.volume, 
            x => musicSource.volume = x, targetVolume, crossFade / 2)
            .SetEase(Ease.InQuad);
    }
    
    private IEnumerator FadeMusicIn(AudioClip musicClip, float crossFade = 0.2f)
    {
        // If music is already playing, fade it out first
        if (musicSource.isPlaying)
        {
            if (_fadeOutTween != null)
                _fadeOutTween.Kill();
                
            float startVolume = musicSource.volume;
            _fadeOutTween = DOTween.To(() => musicSource.volume, 
                x => musicSource.volume = x, 0, crossFade / 2)
                .SetEase(Ease.OutQuad);
                
            yield return _fadeOutTween.WaitForCompletion();
            musicSource.Stop();
        }
        
        // Start new music
        musicSource.clip = musicClip;
        musicSource.volume = 0f;
        musicSource.Play();
        
        // Fade in
        if (_fadeInTween != null)
            _fadeInTween.Kill();
            
        float targetVolume = _gameSettingsManager != null ? _gameSettingsManager.Volume : 1f;
        _fadeInTween = DOTween.To(() => musicSource.volume, 
            x => musicSource.volume = x, targetVolume, crossFade / 2)
            .SetEase(Ease.InQuad);
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }
    
    // public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    // {
    //     if (clip == null || sfxSource == null) return;
    //     
    //     sfxSource.PlayOneShot(clip, volumeScale);
    // }
}
