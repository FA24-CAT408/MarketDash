using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ShelfItemSpawner : MonoBehaviour
{
    // private List<SplineInstantiate> splines = new List<SplineInstantiate>();
    
    public List<GameObject> shelfItemPrefabs;
    
    // Start is called before the first frame update
    void Start()
    {
        // Find all spline instantiate components in children
        SplineInstantiate[] splines = GetComponentsInChildren<SplineInstantiate>();
        
        foreach (var spline in splines)
        {
            // Create a new item with a random prefab from our list
            GameObject randomPrefab = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
            
            var newItem = new SplineInstantiate.InstantiableItem
            {
                Prefab = randomPrefab
            };
            
            // Create a new array with just this item
            spline.itemsToInstantiate = new[] { newItem };
            
            // Force the spline to regenerate items
            spline.UpdateInstances();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
