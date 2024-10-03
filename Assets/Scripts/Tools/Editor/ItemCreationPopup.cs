using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ItemCreationPopup : EditorWindow
{
    private string itemName = "";

    private static GameObject itemPrefab;

    public static void ShowWindow(GameObject prefab)
    {
        itemPrefab = prefab;

        // Open the popup window
        var window = GetWindow<ItemCreationPopup>(true, "Create New Item", true);
        window.maxSize = new Vector2(500, 500);
        window.minSize = new Vector2(250, 200);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter Item Name", EditorStyles.boldLabel, GUILayout.Height(30));

        // Input field for item name
        itemName = EditorGUILayout.TextField("Item Name", itemName);

        GUILayout.Space(10);

        // Button to confirm item creation
        if (GUILayout.Button("Create Item"))
        {
            if (!string.IsNullOrEmpty(itemName))
            {
                Item item = CreateItem(itemName);

                this.Close();  // Close the popup after creation

                AddItemToGroceryList(item);

            }
            else
            {
                EditorGUILayout.HelpBox("Item name cannot be empty!", MessageType.Warning);
            }
        }
    }

    private Item CreateItem(string itemName)
    {
        if (itemPrefab != null)
        {
            // Instantiate the prefab
            GameObject newItem = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab);
            newItem.name = itemName;  // Set the name to the user-provided item name

            Item itemComponent = newItem.GetComponent<Item>();
            itemComponent.itemName = itemName;

            GameObject itemsParent = GameObject.Find("Items");
            newItem.transform.parent = itemsParent.transform;

            Debug.Log($"Item '{itemName}' created from prefab in the scene.");

            return newItem.GetComponent<Item>();
        }
        else
        {
            Debug.LogError("Prefab is not assigned.");
        }

        return null;
    }

    private void AddItemToGroceryList(Item item)
    {
        GroceryListManager groceryListManager = FindObjectOfType<GroceryListManager>();

        if (groceryListManager != null && item != null)
        {
            groceryListManager.groceryList.Add(item);
        }
        else
        {
            GUILayout.Label("Grocery List Manager not found in the scene", EditorStyles.boldLabel);
        }
    }
}
