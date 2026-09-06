using System;
using System.Linq;
using CrazyMarket.Player.V2.Unity;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CrazyMarket.Butter.Editor
{
    public static class ButterVfxAuthoring
    {
        private const string Folder = "Assets/VFX/Butter";
        private const string ScenePath = "Assets/TestCampus/Scenes/TestCampus_Market_PlayerV2.unity";

        [MenuItem("CrazyMarket/Test Campus/Rebuild Butter VFX")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open the stopped Market Player V2 scene before rebuilding its butter VFX.");
            Mesh drop = SaveMesh(BuildDrop(), Folder + "/Butter Drop.asset");
            Mesh puddle = SaveMesh(BuildPuddle(), Folder + "/Butter Puddle.asset");
            Material liquid = Material("Butter Liquid", "CrazyMarket/Butter/Liquid");
            Material surface = Material("Butter Puddle", "CrazyMarket/Butter/Organic Puddle");
            var root = new GameObject("Butter VFX");
            try
            {
                var ps = root.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = main.playOnAwake = true;
                main.duration = 2;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.maxParticles = 384;
                main.startSize3D = true;
                main.startSizeX = .12f; main.startSizeY = .2f; main.startSizeZ = .12f;
                main.startSpeed = 0;
                main.startColor = Color.white;
                main.startLifetime = 1;
                main.gravityModifier = .85f;
                var shape = ps.shape; shape.enabled = false;
                var emission = ps.emission; emission.rateOverTime = 0;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,1),new Keyframe(.65f,1),new Keyframe(1,0)));
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.quality = ParticleSystemCollisionQuality.High;
                collision.collidesWith = 9;
                collision.enableDynamicColliders = false;
                collision.maxCollisionShapes = 64;
                collision.radiusScale = .3f;
                collision.lifetimeLoss = 1;
                collision.sendCollisionMessages = true;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.alignment = ParticleSystemRenderSpace.World;
                renderer.mesh = drop;
                renderer.sharedMaterial = liquid;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enableGPUInstancing = false;

                var fx = root.AddComponent<ButterVfx>();
                var so = new SerializedObject(fx);
                so.FindProperty("liquidMaterial").objectReferenceValue = liquid;
                so.FindProperty("puddleMaterial").objectReferenceValue = surface;
                so.FindProperty("recallPuffMaterial").objectReferenceValue = RecallPuffMaterial();
                so.FindProperty("recallDropMaterial").objectReferenceValue = RecallDropMaterial();
                so.FindProperty("dropletMesh").objectReferenceValue = drop;
                so.FindProperty("puddleMesh").objectReferenceValue = puddle;
                string[] bones = { "hand.L", "hand.R", "spine.003", "spine", "spine.006", "spine" };
                Vector3[] offsets = { new Vector3(0,-.035f,0),new Vector3(0,-.035f,0),new Vector3(.36f,-.08f,-.08f),new Vector3(-.37f,-.08f,-.1f),new Vector3(.25f,.34f,.14f),new Vector3(.32f,-.1f,-.25f) };
                float[] diameters = { .108f,.102f,.096f,.102f,.096f,.096f };
                var sources = so.FindProperty("dripSources"); sources.arraySize = bones.Length;
                for (int i = 0; i < bones.Length; i++)
                {
                    var source = sources.GetArrayElementAtIndex(i);
                    source.FindPropertyRelative("bone").stringValue = bones[i];
                    source.FindPropertyRelative("offset").vector3Value = offsets[i];
                    source.FindPropertyRelative("diameter").floatValue = diameters[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Butter VFX.prefab");
            }
            finally { Object.DestroyImmediate(root); }

            var player = Object.FindAnyObjectByType<PlayerControllerV2>();
            if (player == null) throw new InvalidOperationException("The market scene needs its V2 player.");
            ConfigureButterAnimation(player);
            var skin = player.GetComponentInChildren<SkinnedMeshRenderer>();
            string glazePath = Folder + "/Butter Character Glaze.mat";
            var glaze = AssetDatabase.LoadAssetAtPath<Material>(glazePath);
            if (glaze == null)
            {
                glaze = new Material(skin.sharedMaterial) { name = "Butter Character Glaze" };
                glaze.SetFloat("_SpecularEnabled",1);
                glaze.EnableKeyword("DR_SPECULAR_ON");
                glaze.SetColor("_FlatSpecularColor",new Color(1,.97f,.68f,1));
                glaze.SetFloat("_FlatSpecularSize",.35f);
                glaze.SetFloat("_FlatSpecularEdgeSmoothness",.6f);
                AssetDatabase.CreateAsset(glaze,glazePath);
            }
            skin.sharedMaterial = glaze;
            var old = player.GetComponent<PlayerMovementParticles>();
            if (old != null) Object.DestroyImmediate(old);
            var puffs = player.transform.Find("Butter Foot Puffs");
            if (puffs != null) Object.DestroyImmediate(puffs.gameObject);
            var previous = player.GetComponentInChildren<ButterVfx>(true);
            if (previous == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Butter VFX.prefab"));
                instance.transform.SetParent(player.transform, false);
            }
            Camera.main.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CrazyMarket/Test Campus/Rebuild Butter Recall Material")]
        public static void RebuildRecall()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved, stopped Market scene before configuring recall VFX.");
            var material = RecallPuffMaterial();
            var drops = RecallDropMaterial();
            string path = Folder + "/Butter VFX.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var settings = new SerializedObject(root.GetComponent<ButterVfx>());
                settings.FindProperty("recallPuffMaterial").objectReferenceValue = material;
                settings.FindProperty("recallDropMaterial").objectReferenceValue = drops;
                if (settings.ApplyModifiedPropertiesWithoutUndo()) PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Material RecallPuffMaterial()
        {
            const string shaderName = "CrazyMarket/Butter Recall Puff";
            var material = Material("Butter Recall Puff", shaderName);
            material.shader = Shader.Find(shaderName);
            material.shaderKeywords = Array.Empty<string>();
            material.SetColor("_BaseColor",new Color(.98f,.906f,.47f,1f));
            material.SetColor("_ShadowColor",new Color(.784f,.604f,.208f,1f));
            material.SetColor("_HighlightColor",new Color(1f,.957f,.69f,1f));
            material.SetColor("_OutlineColor",new Color(.125f,.106f,.071f,1f));
            material.SetFloat("_OutlineWidth",.055f);
            material.SetOverrideTag("RenderType","Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster",false);
            material.SetShaderPassEnabled("DepthOnly",false);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static Material RecallDropMaterial()
        {
            var material = Material("Butter Recall Drops", "Universal Render Pipeline/Unlit");
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor",new Color(1f,.82f,.25f,1f));
            material.SetFloat("_Surface",1f);
            material.SetFloat("_Blend",0f);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType","Transparent");
            material.renderQueue=(int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void ConfigureButterAnimation(PlayerControllerV2 player)
        {
            string path = Folder + "/Butter Player.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                if (!AssetDatabase.CopyAsset("Assets/Animations/BlobbyAnimationController.controller", path))
                    throw new InvalidOperationException("Could not create the Market-only butter animation controller.");
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            if (!controller.parameters.Any(p => p.name == "StrideSpeed"))
                controller.AddParameter(new AnimatorControllerParameter { name = "StrideSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                var state = child.state;
                if (state.name == "Running")
                {
                    state.speed = 1;
                    state.speedParameter = "StrideSpeed";
                    state.speedParameterActive = true;
                    EditorUtility.SetDirty(state);
                }
                if (state.name == "Idle") { state.speed = 1; EditorUtility.SetDirty(state); }
                foreach (var transition in state.transitions)
                {
                    if (transition.destinationState == null) continue;
                    if (transition.destinationState.name == "Jumping")
                    {
                        transition.hasFixedDuration = true;
                        transition.duration = .18f;
                        transition.hasExitTime = false;
                        // Short falls may land during the blend; allow that landing to take over.
                        transition.interruptionSource = TransitionInterruptionSource.Destination;
                        transition.orderedInterruption = false;
                        EditorUtility.SetDirty(transition);
                    }
                    else if (transition.destinationState.name == "Running" || transition.destinationState.name == "Idle")
                    {
                        transition.hasFixedDuration = true;
                        // An interrupted idle-to-jump blend can still report the long idle
                        // clip as its source. Normalized landing durations then take seconds.
                        transition.duration = .12f;
                        transition.hasExitTime = false;
                        transition.interruptionSource = TransitionInterruptionSource.Destination;
                        transition.orderedInterruption = false;
                        EditorUtility.SetDirty(transition);
                    }
                }
            }
            EditorUtility.SetDirty(controller);
            var animator = player.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = controller;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            var presenter = player.GetComponent<PlayerAnimationPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("inertialLocomotion").boolValue = true;
            so.FindProperty("runningSpeedThreshold").floatValue = .45f;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(presenter);
        }

        private static Material Material(string name, string shaderName)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Shader has not imported: " + shaderName);
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Mesh BuildPuddle()
        {
            var mesh = new Mesh { name = "Butter Puddle Quad" };
            mesh.vertices = new[] { new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f) };
            mesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildDrop()
        {
            const int rings = 12, sides = 16;
            var vertices = new Vector3[(rings+1)*(sides+1)];
            var triangles = new int[rings*sides*6];
            int index = 0;
            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r/rings;
                float y = -.5f*Mathf.Cos(t*Mathf.PI);
                float radius = .5f*Mathf.Sin(t*Mathf.PI)*Mathf.Lerp(1.25f,.28f,t);
                for (int s = 0; s <= sides; s++)
                {
                    float angle = s*Mathf.PI*2/sides;
                    vertices[r*(sides+1)+s] = new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
                    if (r == rings || s == sides) continue;
                    int a = r*(sides+1)+s, b = a+sides+1;
                    triangles[index++] = a; triangles[index++] = b; triangles[index++] = a+1;
                    triangles[index++] = a+1; triangles[index++] = b; triangles[index++] = b+1;
                }
            }
            var mesh = new Mesh { name = "Butter Teardrop" };
            mesh.vertices = vertices;
            mesh.colors = Enumerable.Repeat(Color.white,vertices.Length).ToArray();
            mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
