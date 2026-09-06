using System;
using System.Collections.Generic;
using CrazyMarket.Player.V2.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyMarket.Butter.Editor
{
    /// <summary>Derives torso morphs from the existing rigged body without changing its skeleton or extremities.</summary>
    public static class ButterBodyAuthoring
    {
        private const string SourcePath = "Assets/Models/Characters/Blobby_Colored Smooth Normals.asset";
        private const string OutputPath = "Assets/Vfx/Butter/Blobby Butter Body.asset";
        private const string ScenePath = "Assets/TestCampus/Scenes/TestCampus_Market_PlayerV2.unity";

        [MenuItem("CrazyMarket/Test Campus/Rebuild Butter Body States")]
        public static void Rebuild()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved, stopped Market scene to rebuild body states.");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerControllerV2>();
            var body = player.GetComponent<ButterBody>();
            var skin = player.GetComponentInChildren<SkinnedMeshRenderer>();
            var source = AssetDatabase.LoadAssetAtPath<Mesh>(SourcePath);
            if (source == null || body == null || skin == null || source.bindposes.Length != skin.bones.Length)
                throw new InvalidOperationException("The original rigged Blobby body and butter component are required.");

            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "Blobby Butter Body";
            mesh.ClearBlendShapes();
            var vertices = source.vertices;
            var normals = source.normals;
            var weights = source.boneWeights;
            var torsoBones = new bool[skin.bones.Length];
            for (int i = 0; i < torsoBones.Length; i++)
            {
                string name = skin.bones[i].name;
                torsoBones[i] = name == "spine" || name == "spine.001" || name == "spine.002" ||
                    name == "spine.003" || name.StartsWith("breast.") || name.StartsWith("pelvis.");
            }
            var deformed = new Vector3[vertices.Length];
            var delta = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var normalMesh = UnityEngine.Object.Instantiate(source);
            try
            {
                for (int frame = 1; frame <= 4; frame++)
                {
                    float spent = frame / 4f;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var v = vertices[i];
                        var weight = weights[i];
                        float torso = (torsoBones[weight.boneIndex0] ? weight.weight0 : 0f) +
                            (torsoBones[weight.boneIndex1] ? weight.weight1 : 0f) +
                            (torsoBones[weight.boneIndex2] ? weight.weight2 : 0f) +
                            (torsoBones[weight.boneIndex3] ? weight.weight3 : 0f);
                        // Imported mesh units: belly sits around .035; the head begins near .071.
                        float lower = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.018f, .029f, v.y));
                        float upper = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.050f, .068f, v.y));
                        float belly = lower * upper * torso;
                        float waist = Mathf.Exp(-Mathf.Pow((v.y-.040f)/.018f,2f));
                        Vector3 shaped = v;
                        shaped.x = .0004f + (v.x-.0004f) * (1f-spent*belly*(.34f+.12f*waist));
                        // Pull the belly in more than the back, then lift its lower sag slightly.
                        float front = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(-.008f,.014f,v.z));
                        shaped.z = -.001f + (v.z+.001f)*(1f-spent*belly*(.28f+.30f*front));
                        shaped.y += .003f*spent*spent*belly*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.032f,.05f,v.y)));
                        deformed[i] = shaped;
                        delta[i] = shaped-v;
                    }
                    normalMesh.vertices = deformed;
                    normalMesh.RecalculateNormals();
                    var shapedNormals = normalMesh.normals;
                    // Smooth imported UV splits together while retaining the original topology.
                    var smooth = new Dictionary<Vector3, Vector3>();
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        smooth.TryGetValue(deformed[i],out Vector3 sum);
                        smooth[deformed[i]] = sum + shapedNormals[i];
                    }
                    for (int i = 0; i < vertices.Length; i++)
                        deltaNormals[i] = delta[i].sqrMagnitude > 1e-12f
                            ? smooth[deformed[i]].normalized-normals[i] : Vector3.zero;
                    mesh.AddBlendShapeFrame("Butter reserve", frame*25f, delta, deltaNormals, null);
                }
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(OutputPath);
                if (existing == null) { AssetDatabase.CreateAsset(mesh, OutputPath); existing = mesh; }
                else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); }
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                skin.sharedMesh = existing;
                PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
                var settings = new SerializedObject(body);
                settings.FindProperty("bodySkin").objectReferenceValue = skin;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { UnityEngine.Object.DestroyImmediate(normalMesh); }
        }
    }
}
