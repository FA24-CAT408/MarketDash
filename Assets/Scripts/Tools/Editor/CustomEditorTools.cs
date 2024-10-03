using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class CustomEditorTools : EditorWindow
{
    GameObject playerSpawnPoint;
    GameObject itemPrefab;

    [MenuItem("Tools/CrazyMarket/Crazy Market Window")]
    private static void ShowWindow()
    {
        var window = GetWindow<CustomEditorTools>();
        window.titleContent = new GUIContent("Crazy Market Tools");
        window.Show();
    }

    private void OnGUI()
    {

        DrawPlayerTools();

        GUILayout.Space(10);

        DrawSceneTools();
    }

    // Grouping Player Tools into a separate method for easy maintenance
    private void DrawPlayerTools()
    {
        GUILayout.Label("Player Tools", EditorStyles.boldLabel);

        playerSpawnPoint = (GameObject)EditorGUILayout.ObjectField("Player Spawn Point", playerSpawnPoint, typeof(GameObject), true);

        GUILayout.Space(5);

        if (GUILayout.Button("Reset Spawn Point Position"))
        {
            ResetSpawnPointPosition();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Move Player To Spawn Point"))
        {
            if (playerSpawnPoint != null)
            {
                MovePlayerToSpawnPoint();
            }
            else
            {
                Debug.LogError("Player Spawn Point is null");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Move Spawn Point To Player"))
        {
            MoveSpawnPointToPlayer();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Move Spawn Point To Scene View Camera"))
        {
            MoveSpawnPointToSceneViewCamera();
        }

        if (playerSpawnPoint == null)
        {
            EditorGUILayout.HelpBox("Please assign the Player Spawn Point", MessageType.Warning);
        }
    }

    private void DrawSceneTools()
    {
        GUILayout.Label("Scene Tools", EditorStyles.boldLabel);

        GUILayout.Space(5);

        if (GUILayout.Button("Instantiate Item"))
        {
            // Open custom popup window to ask for item name
            GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item.prefab");
            ItemCreationPopup.ShowWindow(itemPrefab);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Find Grocery List"))
        {
            GroceryListManager groceryListManager = FindObjectOfType<GroceryListManager>();
            if (groceryListManager != null)
            {
                Selection.activeGameObject = groceryListManager.gameObject;
            }
            else
            {
                Debug.LogError("Grocery List Manager not found in the scene");
            }
        }
    }

    // Tool Methods (Modularize these to keep OnGUI clean)
    private void ResetSpawnPointPosition()
    {
        if (playerSpawnPoint != null)
        {
            playerSpawnPoint.transform.position = new Vector3(59, -3, -21);
            playerSpawnPoint.transform.rotation = Quaternion.identity;
        }
    }

    private void MovePlayerToSpawnPoint()
    {
        if (playerSpawnPoint != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = playerSpawnPoint.transform.position;
                player.transform.rotation = playerSpawnPoint.transform.rotation;
            }
            else
            {
                Debug.LogError("Player not found in the scene");
            }
        }
    }

    private void MoveSpawnPointToPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerSpawnPoint != null)
        {
            playerSpawnPoint.transform.position = player.transform.position;
            playerSpawnPoint.transform.rotation = player.transform.rotation;
        }
        else
        {
            Debug.LogError("Player or Spawn Point is missing");
        }
    }

    private void MoveSpawnPointToSceneViewCamera()
    {
        var sceneViewCamera = SceneView.lastActiveSceneView.camera;
        if (sceneViewCamera != null && playerSpawnPoint != null)
        {
            playerSpawnPoint.transform.position = sceneViewCamera.transform.position;
            playerSpawnPoint.transform.rotation = sceneViewCamera.transform.rotation;
        }
    }
}
