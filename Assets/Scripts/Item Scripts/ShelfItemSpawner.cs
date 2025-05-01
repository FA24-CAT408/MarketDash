using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShelfItemSpawner : MonoBehaviour
{
    public List<GameObject> shelfItemPrefabs = new List<GameObject>();
    private SplineInstantiate[] splines;
    
    public bool randomizeOnStart = true;
    public bool useGroceryList = false;
    
    private GroceryListManager groceryListManager;
    
    void Awake()
    {
        Debug.Log("ShelfItemSpawner: Awake called");
        groceryListManager = FindObjectOfType<GroceryListManager>();
        
        splines = GetComponentsInChildren<SplineInstantiate>();
        Debug.Log($"ShelfItemSpawner: Found {(splines != null ? splines.Length : 0)} splines in Awake");
    }
    
    void Start()
    {
        Debug.Log("ShelfItemSpawner: Start called");
        Debug.Log($"ShelfItemSpawner: Have {shelfItemPrefabs.Count} prefabs available");
        
        // if (!randomizeOnStart) {
        //     Debug.Log("ShelfItemSpawner: randomizeOnStart is false, skipping");
        //     return;
        // }
        //
        // if (useGroceryList && groceryListManager != null)
        // {
        //     Debug.Log("ShelfItemSpawner: Filtering prefabs based on grocery list");
        //     shelfItemPrefabs = shelfItemPrefabs.Where(prefab => 
        //         groceryListManager.groceryList.All(item => 
        //             prefab.GetComponent<Item>()?.itemName != item.itemName))
        //         .ToList();
        //     Debug.Log($"ShelfItemSpawner: After filtering, {shelfItemPrefabs.Count} prefabs remain");
        // }
        //
        // // Use coroutine for delayed spawn instead of immediate execution
        // StartCoroutine(DelayedSpawn());
    }
    
    IEnumerator DelayedSpawn()
    {
        Debug.Log("ShelfItemSpawner: Starting delayed spawn");
        // Wait a short time to ensure everything is initialized
        yield return new WaitForSeconds(0.2f);
        
        // Re-get splines to ensure we have the latest references
        splines = GetComponentsInChildren<SplineInstantiate>();
        Debug.Log($"ShelfItemSpawner: Found {(splines != null ? splines.Length : 0)} splines after delay");
        
        ApplyAllPrefabsToSplines();
        RegenerateSplineItems();
        
        Debug.Log("ShelfItemSpawner: Delayed spawn completed");
    }
    
    public void ApplyAllPrefabsToSplines()
    {
        Debug.Log("ShelfItemSpawner: ApplyAllPrefabsToSplines called");
        if (splines == null || splines.Length == 0) {
            splines = GetComponentsInChildren<SplineInstantiate>();
            Debug.Log($"ShelfItemSpawner: Re-acquired splines, found {(splines != null ? splines.Length : 0)}");
        }
            
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0) {
            Debug.LogWarning("ShelfItemSpawner: No shelf item prefabs available!");
            return;
        }
            
        // Calculate equal probability for each item
        float equalProbability = 100f / shelfItemPrefabs.Count;
        
        // Create ONE array of InstantiableItems containing ALL prefabs
        var allItems = new SplineInstantiate.InstantiableItem[shelfItemPrefabs.Count];
        
        for (int i = 0; i < shelfItemPrefabs.Count; i++)
        {
            if (shelfItemPrefabs[i] == null) {
                Debug.LogWarning($"ShelfItemSpawner: Prefab at index {i} is null!");
                continue;
            }
            
            allItems[i] = new SplineInstantiate.InstantiableItem
            {
                Prefab = shelfItemPrefabs[i],
                Probability = equalProbability // Set equal probability for each item
            };
        }
        
        // Assign this SAME complete list to EACH spline
        foreach (var spline in splines)
        {
            if (spline == null) {
                Debug.LogWarning("ShelfItemSpawner: Found null spline reference!");
                continue;
            }
            
            // Each spline gets the complete array of ALL prefabs
            spline.itemsToInstantiate = allItems;
            Debug.Log($"ShelfItemSpawner: Assigned {allItems.Length} items to spline {spline.name}");
        }
    }
    
    public void RegenerateSplineItems()
    {
        Debug.Log("ShelfItemSpawner: RegenerateSplineItems called");
        if (splines == null || splines.Length == 0) {
            splines = GetComponentsInChildren<SplineInstantiate>();
            Debug.Log($"ShelfItemSpawner: Re-acquired splines, found {(splines != null ? splines.Length : 0)}");
        }
            
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0) {
            Debug.LogWarning("ShelfItemSpawner: No shelf item prefabs available!");
            return;
        }
            
        // For each spline, keep the same itemsToInstantiate but force regeneration
        foreach (var spline in splines)
        {
            if (spline == null) {
                Debug.LogWarning("ShelfItemSpawner: Found null spline reference!");
                continue;
            }
            
            // This is equivalent to clicking the "Regenerate" button in the SplineInstantiate component
            // Keep all prefabs assigned, just regenerate their placement
            try {
                spline.Randomize();
                Debug.Log($"ShelfItemSpawner: Randomized spline {spline.name}");
            }
            catch (System.Exception e) {
                Debug.LogError($"ShelfItemSpawner: Error randomizing spline {spline.name}: {e.Message}");
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ShelfItemSpawner))]
public class ShelfItemSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ShelfItemSpawner spawner = (ShelfItemSpawner)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Apply ALL Prefabs to Splines"))
        {
            spawner.ApplyAllPrefabsToSplines();
            EditorUtility.SetDirty(spawner);
        }
        
        if (GUILayout.Button("Regenerate Spline Items"))
        {
            spawner.RegenerateSplineItems();
            EditorUtility.SetDirty(spawner);
        }
    }
}
#endif
