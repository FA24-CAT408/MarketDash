using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static event Action<float> OnTimerUpdated;
    
    [SerializeField] private GameSaveManager _gameSave;

    private float _timer;
    private bool _timerActive;
    
    public float Timer
    {
        get => _timer;
        set => _timer = value;
    }
    
    public bool TimerActive
    {
        get => _timerActive;
        set => _timerActive = value;
    }

    public void StartTimer()
    {
        _timer = 0;

        _timerActive = true;

        StartCoroutine(TimerCoroutine());
    }

    public void StopTimer()
    {
        _timerActive = false;
        
        StopCoroutine(TimerCoroutine());
    }

    public float GetCurrentTime()
    {
        return _timer;
    }
    
    public void AddTime(float time)
    {
        _timer += time;
        OnTimerUpdated?.Invoke(_timer);
    }

    public void SubtractTime(float time)
    {
        _timer -= time;
        OnTimerUpdated?.Invoke(_timer);
    }
    
    public void ResetTimer()
    {
        _timer = 0;
        OnTimerUpdated?.Invoke(_timer);
    }

    IEnumerator TimerCoroutine()
    {
        while (_timerActive)
        {
            _timer += Time.deltaTime;
            OnTimerUpdated?.Invoke(_timer);
            yield return null;
        }
    }
    
    public float GetBestTime()
    {
        if(_gameSave != null && _gameSave.IsLevelCompleted(GameManager.Instance.currentLevel))
        {
            return _gameSave.GetLevelTime(GameManager.Instance.currentLevel);
        }
        else
        {
            Debug.Log($"No best time for level {GameManager.Instance.currentLevel}");
            return float.MaxValue;
        }
    }
    
    public void SetBestTime()
    {
        if (_gameSave == null)
        {
            Debug.LogError("GameSaveManager reference is missing!");
            return;
        }
        
        Debug.Log($"Setting best time for level {GameManager.Instance.currentLevel}");
        
        float bestTime = GetBestTime();
        
        if (_timer < bestTime)
        {
            Debug.Log($"New best time: {_timer}");
            _gameSave.SetLevelTime(GameManager.Instance.currentLevel, _timer);
        }
        else
        {
            Debug.Log($"No update: Current time: {_timer} | Best time: {bestTime}");
        }
    }
    
    public string GetFormattedTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int centiseconds = (int)((time * 100) % 100);

        if (time < 60)
        {
            // Display format: "SS.CC"
            return string.Format("{0}.{1:00}", seconds, centiseconds);
        }
        else
        {
            // Display format: "MM:SS.CC"
            return string.Format("{0}:{1:00}.{2:00}", minutes, seconds, centiseconds);
        }
    }
}
