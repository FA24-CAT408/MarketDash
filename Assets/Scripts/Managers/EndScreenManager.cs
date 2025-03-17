using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenManager : MonoBehaviour
{
    // Keep track of the current state of the end screen
    private enum EndScreenState
    {
        LoadingIn,
        DisplayingStats,
        Finished,
    }
    
    [Header("End Screen State")]
    [SerializeField]
    private EndScreenState currentState;
    
    [Header("Debug")]
    [SerializeField] private bool restartSequence;
    
    [Header("Game Save")]
    // Get Level Times from SOAP object
    [SerializeField] private GameSaveManager gameSave;
    
    [Header("Completion Times")]
    // Create All Level Time TMP Prefabs
    public GameObject completionTimeParent;
    public GameObject completionTimePrefab;
    public List<GameObject> completionTimes;
    public float completionXOffset = 10f;
    
    [Header("Fade In Group")]
    // Transition into the Scene
    public CanvasGroup endScreenCanvasGroup;
    public CanvasGroup transitionCanvasGroup;
    public float fadeDuration = 1.5f;
    
    // Have Total Time Appear, "PUNCH" in,Total Time "random" number effect, then land on FINAL TOTAL TIME
    // Have Try Again? / Main Menu button appear one at a time
    [Header("Other")]
    public GameObject totalTimeParent;
    public TMP_Text totalTimeText;
    public GameObject buttonsHolder;
    public float buttonXOffset = 20f;
    public float buttonAnimationDelay = 0.2f;
    
    private void Start()
    {
        ChangeState(EndScreenState.LoadingIn);
    }
    
    private void Update()
    {
        // Debug restart button logic
        if (restartSequence)
        {
            restartSequence = false;
            RestartSequence();
        }
    }
    
    // Debug method to restart the sequence
    private void RestartSequence()
    {
        // Kill all active tweens
        DOTween.KillAll();
        
        // Clear existing completion times
        foreach (var time in completionTimes)
        {
            if (time != null)
                Destroy(time);
        }
        completionTimes.Clear();
        
        // Reset UI elements
        totalTimeParent.gameObject.SetActive(false);
        endScreenCanvasGroup.alpha = 0f;
        
        // Hide buttons and reset their positions
        SetupButtonsHolder(true);
        
        // Restart the sequence
        ChangeState(EndScreenState.LoadingIn);
    }
    
    // Helper method to setup buttons for animation
    private void SetupButtonsHolder(bool forReset = false)
    {
        if (buttonsHolder == null) return;
        
        // Get all button children
        for (int i = 0; i < buttonsHolder.transform.childCount; i++)
        {
            Transform buttonTransform = buttonsHolder.transform.GetChild(i);
            GameObject button = buttonTransform.gameObject;
            
            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
            }
            
            if (forReset)
            {
                // Move button off-screen to the right and make it invisible
                Vector3 originalPos = buttonTransform.localPosition;
                buttonTransform.localPosition = new Vector3(originalPos.x + buttonXOffset, originalPos.y, originalPos.z);
                canvasGroup.alpha = 0f;
            }
        }
    }
    
    private void ChangeState(EndScreenState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;

        Debug.Log("[ENDSCREEN] CHANGING TO STATE: " + newState);

        switch (newState)
        {
            case EndScreenState.LoadingIn:
                EnterLoadingIn();
                break;
            case EndScreenState.DisplayingStats:
                EnterDisplayStats();
                break;
            case EndScreenState.Finished:
                EnterFinished();
                break;
        }
    }

    private void EnterLoadingIn()
    {
        // Make sure the fade panel is active and fully opaque
        transitionCanvasGroup.gameObject.SetActive(true);
        transitionCanvasGroup.alpha = 1f;
        
        endScreenCanvasGroup.alpha = 0f;
        endScreenCanvasGroup.gameObject.SetActive(true);
        
        // Setup buttons for animation
        SetupButtonsHolder();
        
        // Create the fade-in animation
        transitionCanvasGroup.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // When fade completes, transition to PreGame state
                transitionCanvasGroup.gameObject.SetActive(false);
                ChangeState(EndScreenState.DisplayingStats);
            });
    }
    
    private void EnterDisplayStats()
    {
        totalTimeParent.gameObject.SetActive(false);
        
        endScreenCanvasGroup.DOFade(1f, 0.25f)
            .SetEase(Ease.OutQuad);
        
        // Generate All Stats
        foreach (var entry in gameSave.LevelTimeEntries)
        {
            GameObject entryGameObject =  Instantiate(completionTimePrefab, completionTimeParent.transform);
            
            TMP_Text entryLevelText = entryGameObject.transform.GetChild(0).GetComponent<TMP_Text>();
            entryLevelText.transform.localPosition = new Vector3(entryLevelText.transform.localPosition.x + completionXOffset, entryLevelText.transform.localPosition.y, 0);
            entryLevelText.SetText("Level " + entry.LevelId + ": " + FormatTime(entry.CompletionTime));
            
            entryGameObject.transform.GetComponent<CanvasGroup>().alpha = 0f;
            
            completionTimes.Add(entryGameObject);
        }

        // Create sequence for all entries with delay between each
        Sequence fullSequence = DOTween.Sequence();
        
        float delayBetweenEntries = 0.2f;
    
        for (int i = 0; i < completionTimes.Count; i++)
        {
            GameObject entryGameObject = completionTimes[i];
            CanvasGroup canvasGroup = entryGameObject.GetComponent<CanvasGroup>();
            TMP_Text entryLevelText = entryGameObject.transform.GetChild(0).GetComponent<TMP_Text>();
        
            // Create sequence for this entry
            Sequence entrySequence = DOTween.Sequence();
            
            // Move from right to final position
            entrySequence.Append(entryLevelText.transform
                .DOLocalMoveX(entryLevelText.transform.localPosition.x - completionXOffset, 0.5f)
                .SetEase(Ease.OutQuad));
        
            // Fade in simultaneously
            entrySequence.Join(canvasGroup
                .DOFade(1f, 0.5f)
                .SetEase(Ease.OutQuad));
        
            // Add to main sequence with appropriate delay
            fullSequence.Insert(i * delayBetweenEntries, entrySequence);
        }
        
        fullSequence.AppendCallback(() => {
            totalTimeParent.gameObject.SetActive(true);
            totalTimeText.text = FormatTime(gameSave.TotalTime);
        });
        
        // Have Total Time Appear, "PUNCH" in,Total Time "random" number effect, then land on FINAL TOTAL TIME
        // totalTimeText.text = "Total Time: 00:00.000";
        
        // Add the punch rotation effect
        // Create a sequence for the total time animations
        Sequence totalTimeSequence = DOTween.Sequence();
    
        // Add both punch effects
        totalTimeSequence.Append(totalTimeParent.transform.DOPunchRotation(new Vector3(0, 0, 5), 0.5f, 10, 1));
        totalTimeSequence.Join(totalTimeParent.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0), 0.5f, 1, 0.5f));
    
        // Add the total time sequence to the main sequence
        fullSequence.Append(totalTimeSequence);
        
        // Have Try Again? / Main Menu button appear one at a time
        // Add button animations - animate each child of the buttons holder
        if (buttonsHolder != null)
        {
            for (int i = 0; i < buttonsHolder.transform.childCount; i++)
            {
                Transform buttonTransform = buttonsHolder.transform.GetChild(i);
                GameObject button = buttonTransform.gameObject;
                
                CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
                Vector3 originalPos = new Vector3(
                    buttonTransform.localPosition.x - buttonXOffset,
                    buttonTransform.localPosition.y,
                    buttonTransform.localPosition.z
                );
                
                Sequence buttonSequence = DOTween.Sequence();
                buttonSequence.Append(buttonTransform.DOLocalMove(originalPos, 0.5f).SetEase(Ease.OutQuad));
                buttonSequence.Join(canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad));
                
                // Add delay between button animations
                if (i > 0)
                {
                    fullSequence.AppendInterval(buttonAnimationDelay);
                }
                
                fullSequence.Append(buttonSequence);
            }
        }
        
        fullSequence.AppendCallback(() => {
            ChangeState(EndScreenState.Finished);
        });
    
        // Play the full sequence
        fullSequence.Play();
    }
    
    private void EnterFinished()
    {
        // Implement any final state logic here
        Debug.Log("End screen sequence completed");
    }

    // Helper method to format time consistently
    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int centiseconds = (int)((time * 100) % 100);
        
        return string.Format("{0}:{1:00}.{2:00}", minutes, seconds, centiseconds);
    }

    void FadeToScene(string sceneName)
    {
        transitionCanvasGroup.gameObject.SetActive(true);
        transitionCanvasGroup.alpha = 0f;
        
        Sequence fadeSequence = DOTween.Sequence();

        fadeSequence.Append(endScreenCanvasGroup.DOFade(0f, 0.25f)
            .SetEase(Ease.OutQuad));
        
        fadeSequence.Insert(0, transitionCanvasGroup.DOFade(1f, fadeDuration)
            .SetEase(Ease.OutQuad));

        fadeSequence.OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName);
        });
       
        fadeSequence.Play();
    }

    public void RestartGame()
    {
        FadeToScene("Level 1");
    }

    public void GoToMainMenu()
    {
        FadeToScene("Main Menu");
    }
}
