using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StagingAreaController : MonoBehaviour
{
    // public GameObject[] stagingAreas; // Not used in this context
    public Transform[] splinePoints; // Assign Spline(1), Spline(2), ... in Inspector

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
        if(GameManager.Instance.CurrentState == GameManager.GameState.EndGame) 
            glow.gameObject.SetActive(true);
    }

    public IEnumerator SpawnItemsCoroutine()
    {
        var items = groceryListManager.groceryList;
        for (int i = 0; i < items.Count && i < splinePoints.Length; i++)
        {
            var item = items[i];
            if (item.prefab == null) continue; // Skip if no prefab

            GameObject itemObj = Instantiate(item.prefab, splinePoints[i].position, Quaternion.identity);
            itemObj.SetActive(true);
            itemObj.transform.GetChild(0).gameObject.SetActive(false);
            itemObj.transform.localScale = Vector3.zero;
            itemObj.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.75f);
        }
        
        uIManager.ShowInGameUI(false);
        uIManager.ShowLevelBeatUI(true);
        uIManager.UpdateLevelBeatUI();
    }
}