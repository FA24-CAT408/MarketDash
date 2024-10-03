using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ItemCreationPopup : EditorWindow
{
    private string itemName = "";

    public static void ShowWindow()
    {
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
                CreateItem(itemName);
                this.Close();  // Close the popup after creation
            }
            else
            {
                EditorGUILayout.HelpBox("Item name cannot be empty!", MessageType.Warning);
            }
        }
    }

    private void CreateItem(string itemName)
    {
        // Create a new GameObject in the scene with the provided item name
        GameObject newItem = new GameObject(itemName);
        Debug.Log($"Item '{itemName}' created in the scene.");
    }
}
