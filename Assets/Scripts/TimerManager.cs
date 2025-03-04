using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; } 
    
    public static event Action<float> OnTimerUpdated;

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

    private void Awake()
    {
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
        if(PlayerPrefs.HasKey("BestTime"))
        {
            Debug.Log("Has Best Time: " + PlayerPrefs.GetFloat("BestTime"));
            return PlayerPrefs.GetFloat("BestTime");
        }
        else
        {
            Debug.Log("No Best Time");
            return float.MaxValue;
        }
    }
    
    public void SetBestTime()
    {
        Debug.Log("setting best time");
        
        var bestTime = GetBestTime();
        
        Debug.Log("Best Time: " + bestTime);
        
        if (_timer < bestTime)
        {
            Debug.Log($"New Best Time: {_timer}");
            PlayerPrefs.SetFloat("BestTime", _timer);
        }
        else
        {
            Debug.Log($"No update: Current Time: {_timer} | Best Time: {bestTime}");
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
