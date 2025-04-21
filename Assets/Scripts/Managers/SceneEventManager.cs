using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SceneEventManager : MonoBehaviour
{
    [Header("Scene-Specific Events")]
    public UnityEvent OnPreGame;
    public UnityEvent OnGameInProgress;
    public UnityEvent OnGameOver;
    public UnityEvent OnEndGame;
    public UnityEvent OnGamePause;
    public UnityEvent<float> OnLevelComplete;

    private void Awake()
    {
        // Register with GameManager when scene loads
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSceneEvents(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister when scene unloads
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterSceneEvents();
        }
    }
}
