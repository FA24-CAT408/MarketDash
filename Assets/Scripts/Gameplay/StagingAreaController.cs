using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;

public class StagingAreaController : MonoBehaviour
{
    // public GameObject[] stagingAreas; // Not used in this context
    public SplineContainer[] splineContainers; // Assign Spline(1), Spline(2), ... in Inspector

    [SerializeField] private bool showLegacyGlow = true;
    [SerializeField, Min(0)] private float presentationDelay;
    public bool PresentationComplete { get; private set; }
    private GameObject glow;
    private UIManager uIManager;
    private GroceryListManager groceryListManager;

    void Start()
    {
        glow = transform.GetChild(0).gameObject;
        uIManager = FindObjectOfType<UIManager>();
        groceryListManager = FindObjectOfType<GroceryListManager>();
    }

    void Update()
    {
        if(showLegacyGlow && GameManager.Instance.CurrentState == GameManager.GameState.EndGame) 
            glow.gameObject.SetActive(true);
    }
    
    public IEnumerator SpawnItemsCoroutine()
    {
        PresentationComplete = false;
        if (presentationDelay > 0) yield return new WaitForSeconds(presentationDelay);
        var items = groceryListManager.GroceryList;
        int count = items.Count;

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            if (item.prefab == null) continue;

            // Evenly distribute items along the spline (0 to 1)
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            Vector3 spawnPos = splineContainers[0].EvaluatePosition(t);

            GameObject itemObj = Instantiate(item.prefab, spawnPos, Quaternion.identity);
            itemObj.gameObject.SetActive(true);
            itemObj.transform.localScale = Vector3.zero;
            itemObj.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.6f);
        }

        PresentationComplete = true;
        if (uIManager != null)
        {
            uIManager.ShowInGameUI(false);
            uIManager.ShowLevelBeatUI(true);
            uIManager.UpdateLevelBeatUI();
        }
    }
}
