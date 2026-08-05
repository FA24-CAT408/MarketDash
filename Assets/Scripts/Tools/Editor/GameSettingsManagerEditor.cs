using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSettingsManager))]
public class GameSettingsManagerEditor : UnityEditor.Editor
{
    private float Spacing => EditorGUIUtility.singleLineHeight * 0.5f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameSettingsManager scriptableSave = (GameSettingsManager)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Useful Methods", EditorStyles.boldLabel);
        GUILayout.Space(Spacing);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Height(30f)))
        {
            scriptableSave.Save();
        }

        if (GUILayout.Button("Load", GUILayout.Height(30f)))
        {
            scriptableSave.Load();
        }

        if (GUILayout.Button("Print", GUILayout.Height(30f)))
        {
            scriptableSave.PrintToConsole();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Delete", GUILayout.Height(30f)))
        {
            scriptableSave.Delete();
        }

        if (GUILayout.Button("Open Save Location", GUILayout.Height(30f)))
        {
            scriptableSave.OpenSaveLocation();
        }

        EditorGUILayout.EndHorizontal();
    }
}