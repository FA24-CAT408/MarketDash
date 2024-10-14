using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class CustomEditorTools : EditorWindow
{
    GameObject _playerSpawnPoint;
    PlayerStateMachine _playerStateMachine;


    [MenuItem("Tools/CrazyMarket/Crazy Market Window")]
    private static void ShowWindow()
    {
        var window = GetWindow<CustomEditorTools>();
        window.titleContent = new GUIContent("Crazy Market Tools");
        window.Show();
    }

    private void OnGUI()
    {
        // Repaint();

        DrawPlayerTools();

        GUILayout.Space(10);

        DrawSceneTools();
    }

    // Grouping Player Tools into a separate method for easy maintenance
    private void DrawPlayerTools()
    {
        GUILayout.Label("Player Tools", EditorStyles.boldLabel);

        // _playerSpawnPoint = (GameObject)EditorGUILayout.ObjectField("Player Spawn Point", _playerSpawnPoint, typeof(GameObject), true);

        _playerSpawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");
        var player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            _playerStateMachine = player.GetComponent<PlayerStateMachine>();
            if (_playerStateMachine != null)
            {
                // Display Current State
                GUILayout.Label("Current State: " + _playerStateMachine.CurrentStateName, EditorStyles.label);

                // Display Sub-State if applicable
                var currentSubState = _playerStateMachine.CurrentState; // Assuming sub-state tracking in your PlayerStateMachine
                if (currentSubState != null)
                {
                    GUILayout.Label("Sub-State: " + currentSubState.GetType().Name, EditorStyles.label);
                }
            }
            else
            {
                GUILayout.Label("PlayerStateMachine component not found on Player", EditorStyles.helpBox);
            }
        }
        else
        {
            GUILayout.Label("Player not found in the scene", EditorStyles.helpBox);
        }

        GUILayout.Space(5);

        GUILayout.Space(5);

        if (GUILayout.Button("Reset Spawn Point Position"))
        {
            ResetSpawnPointPosition();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Move Player To Spawn Point"))
        {
            if (_playerSpawnPoint != null)
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

        if (_playerSpawnPoint == null)
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
        if (_playerSpawnPoint != null)
        {
            _playerSpawnPoint.transform.position = new Vector3(59, -3, -21);
            _playerSpawnPoint.transform.rotation = Quaternion.identity;
        }
    }

    private void MovePlayerToSpawnPoint()
    {
        if (_playerSpawnPoint != null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = _playerSpawnPoint.transform.position;
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
        if (player != null && _playerSpawnPoint != null)
        {
            _playerSpawnPoint.transform.position = player.transform.position;
        }
        else
        {
            Debug.LogError("Player or Spawn Point is missing");
        }
    }

    private void MoveSpawnPointToSceneViewCamera()
    {
        var sceneViewCamera = SceneView.lastActiveSceneView.camera;
        if (sceneViewCamera != null && _playerSpawnPoint != null)
        {
            _playerSpawnPoint.transform.position = sceneViewCamera.transform.position;
        }
    }
}
