using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public TMP_Text timerText;

    [Header("Timers")]
    public GameObject startTimerObj;
    public GameObject stopTimerObj;

    public GameObject startWall;
    public GameObject stopWall;

    public float timer;
    public bool timerActive;

    public void StartTimer()
    {
        timer = 0;

        timerActive = true;
        startTimerObj.SetActive(false);
        stopTimerObj.SetActive(true);

        startWall.SetActive(true);
        stopWall.SetActive(false);

        StartCoroutine(Timer());
    }

    public void StopTimer()
    {
        timerActive = false;
        stopTimerObj.SetActive(false);
        startTimerObj.SetActive(true);

        startWall.SetActive(false);
        stopWall.SetActive(true);

        UpdateTimerUI();
        StopCoroutine(Timer());
    }

    IEnumerator Timer()
    {
        while (timerActive)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }
    }

    void UpdateTimerUI()
    {
        int minutes = (int)(timer / 60);
        int seconds = (int)(timer % 60);
        int centiseconds = (int)((timer * 100) % 100);

        if (timer < 60)
        {
            // Display format: "SS.CC"
            timerText.text = string.Format("{0}.{1:00}", seconds, centiseconds);
        }
        else
        {
            // Display format: "MM:SS.CC"
            timerText.text = string.Format("{0}:{1:00}.{2:00}", minutes, seconds, centiseconds);
        }
    }
}
