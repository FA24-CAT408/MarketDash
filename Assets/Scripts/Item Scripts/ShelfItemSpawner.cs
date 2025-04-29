using System.Collections;
using System.Collections.Generic;
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
    
    void Awake()
    {
        splines = GetComponentsInChildren<SplineInstantiate>();
    }
    
    void Start()
    {
        ApplyAllPrefabsToSplines();

        if (randomizeOnStart)
            RegenerateSplineItems();
    }
    
    public void ApplyAllPrefabsToSplines()
    {
        if (splines == null || splines.Length == 0)
            splines = GetComponentsInChildren<SplineInstantiate>();
            
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0)
            return;
            
        // Calculate equal probability for each item
        float equalProbability = 100f / shelfItemPrefabs.Count;
        
        // Create ONE array of InstantiableItems containing ALL prefabs
        var allItems = new SplineInstantiate.InstantiableItem[shelfItemPrefabs.Count];
        
        for (int i = 0; i < shelfItemPrefabs.Count; i++)
        {
            allItems[i] = new SplineInstantiate.InstantiableItem
            {
                Prefab = shelfItemPrefabs[i],
                Probability = equalProbability // Set equal probability for each item
            };
        }
        
        // Assign this SAME complete list to EACH spline
        foreach (var spline in splines)
        {
            // Each spline gets the complete array of ALL prefabs
            spline.itemsToInstantiate = allItems;
            //
            // Force the spline to regenerate items
            spline.Randomize();
        }
    }
    
    public void RegenerateSplineItems()
    {
        if (splines == null || splines.Length == 0)
            splines = GetComponentsInChildren<SplineInstantiate>();
            
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0)
            return;
            
        // For each spline, keep the same itemsToInstantiate but force regeneration
        foreach (var spline in splines)
        {
            // This is equivalent to clicking the "Regenerate" button in the SplineInstantiate component
            // Keep all prefabs assigned, just regenerate their placement
            spline.Randomize();
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
