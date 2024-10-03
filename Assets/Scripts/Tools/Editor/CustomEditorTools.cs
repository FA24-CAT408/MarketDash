using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CustomEditorTools : EditorWindow
{
    [MenuItem("Tools/CrazyMarket/Crazy Market Window")]
    private static void ShowWindow()
    {
        var window = GetWindow<CustomEditorTools>();
        window.titleContent = new GUIContent("Crazy Market Tools");
        window.Show();
    }

    private void OnGUI()
    {

    }
}
