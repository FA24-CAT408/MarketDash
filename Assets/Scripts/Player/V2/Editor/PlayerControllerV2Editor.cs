#if UNITY_EDITOR
using CrazyMarket.Player.V2.Unity;
using UnityEditor;
using UnityEngine;

namespace CrazyMarket.Player.V2.Editor
{
    [CustomEditor(typeof(PlayerControllerV2))]
    public sealed class PlayerControllerV2Editor : UnityEditor.Editor
    {
        private SerializedProperty profileProperty;
        private LocomotionTuning workingTuning;
        private string workingProfileId;
        private bool hasWorkingCopy;
        private string status;
        private MessageType statusType = MessageType.Info;
        private bool showGround = true;
        private bool showAir = true;
        private bool showJump = true;
        private bool showForcesAndOrientation = true;

        private void OnEnable()
        {
            profileProperty = serializedObject.FindProperty("profile");
            hasWorkingCopy = false;
            status = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Player Controller V2", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime locomotion tuning is available in Play Mode. Edits stay in memory unless you explicitly save them.",
                    MessageType.Info);
                return;
            }

            DrawRuntimeTuning();
        }

        private void DrawRuntimeTuning()
        {
            PlayerControllerV2 controller = (PlayerControllerV2)target;
            PlayerRuntimeProfile runtime = controller.CaptureRuntimeProfile();
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("The runtime profile is not available yet.", MessageType.Warning);
                return;
            }

            if (!hasWorkingCopy || workingProfileId != runtime.ProfileId.Value)
            {
                workingTuning = runtime.Locomotion;
                workingProfileId = runtime.ProfileId.Value;
                hasWorkingCopy = true;
            }

            EditorGUILayout.HelpBox(
                "Play Mode edits use a working copy and are applied at the next safe controller step. They do not change the profile asset.",
                MessageType.Info);

            bool changed = false;
            showGround = EditorGUILayout.Foldout(showGround, "Ground", true);
            if (showGround)
            {
                EditorGUI.indentLevel++;
                changed |= FloatField(ref workingTuning.StableMoveSpeed, "Stable Move Speed");
                changed |= FloatField(ref workingTuning.StableMovementSharpness, "Stable Movement Sharpness");
                EditorGUI.indentLevel--;
            }

            showAir = EditorGUILayout.Foldout(showAir, "Air", true);
            if (showAir)
            {
                EditorGUI.indentLevel++;
                changed |= FloatField(ref workingTuning.AirMoveSpeed, "Air Move Speed");
                changed |= FloatField(ref workingTuning.AirAcceleration, "Air Acceleration");
                changed |= FloatField(ref workingTuning.Drag, "Drag");
                EditorGUI.indentLevel--;
            }

            showJump = EditorGUILayout.Foldout(showJump, "Jump", true);
            if (showJump)
            {
                EditorGUI.indentLevel++;
                changed |= FloatField(ref workingTuning.JumpSpeed, "Jump Speed");
                changed |= FloatField(ref workingTuning.JumpBufferTime, "Jump Buffer Time");
                changed |= FloatField(ref workingTuning.CoyoteTime, "Coyote Time");
                EditorGUI.indentLevel--;
            }

            showForcesAndOrientation = EditorGUILayout.Foldout(showForcesAndOrientation,
                "Forces & Orientation", true);
            if (showForcesAndOrientation)
            {
                EditorGUI.indentLevel++;
                changed |= FloatField(ref workingTuning.Gravity, "Gravity");
                changed |= FloatField(ref workingTuning.FallGravityMultiplier, "Fall Gravity Multiplier");
                changed |= FloatField(ref workingTuning.OrientationSharpness, "Orientation Sharpness");
                EditorGUI.indentLevel--;
            }

            if (changed)
                ApplyWorkingTuning(controller);

            EditorGUILayout.Space(4f);
            DrawProfileActions(controller);
            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.HelpBox(status, statusType);
        }

        private void DrawProfileActions(PlayerControllerV2 controller)
        {
            PlayerProfile selected = profileProperty == null
                ? null
                : profileProperty.objectReferenceValue as PlayerProfile;

            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                selected == null
                    ? "No profile asset is selected. The controller is using its production fallback."
                    : "Changing the profile reference does not replace the active runtime profile until you apply it.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(selected == null))
            {
                if (GUILayout.Button("Apply Selected Profile at Next Safe Step"))
                {
                    PlayerOperationResult result = controller.SelectProfile(selected);
                    SetResult(result, "Selected profile queued for the next safe step.");
                }
            }

            using (new EditorGUI.DisabledScope(selected == null || !IsFinite(workingTuning)))
            {
                if (GUILayout.Button("Save Runtime Values to Profile…"))
                    SaveWorkingValuesToProfile(selected);
            }

            if (selected != null && !IsFinite(workingTuning))
            {
                EditorGUILayout.HelpBox(
                    "NaN and infinity are rejected by the runtime profile result. Correct those values before saving.",
                    MessageType.Error);
            }
        }

        private void ApplyWorkingTuning(PlayerControllerV2 controller)
        {
            PlayerRuntimeProfile captured = controller.CaptureRuntimeProfile();
            PlayerRuntimeProfile replacement = captured == null ? null : captured.WithLocomotion(workingTuning);
            PlayerOperationResult result = controller.ReplaceRuntimeProfile(replacement);
            SetResult(result, "Runtime locomotion tuning queued for the next safe step.");
            if (result == PlayerOperationResult.RejectedInvalidProfile)
                status = "Runtime tuning was rejected (the profile requires finite values).";
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void SaveWorkingValuesToProfile(PlayerProfile profile)
        {
            if (profile == null || !IsFinite(workingTuning))
            {
                status = "Runtime values were not saved because the tuning contains NaN or infinity.";
                statusType = MessageType.Error;
                return;
            }

            if (!EditorUtility.DisplayDialog("Save Runtime Locomotion Values",
                    "This writes the working locomotion values into the selected profile asset. Ability loadout data will remain unchanged.",
                    "Save to Profile", "Cancel"))
                return;

            Undo.RecordObject(profile, "Save Player V2 Runtime Locomotion Tuning");
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty data = serializedProfile.FindProperty("data");
            SerializedProperty locomotion = data == null ? null : data.FindPropertyRelative("locomotion");
            if (locomotion == null)
            {
                status = "Profile data could not be found; nothing was saved.";
                statusType = MessageType.Error;
                return;
            }

            SetFloat(locomotion, "StableMoveSpeed", workingTuning.StableMoveSpeed);
            SetFloat(locomotion, "AirMoveSpeed", workingTuning.AirMoveSpeed);
            SetFloat(locomotion, "AirAcceleration", workingTuning.AirAcceleration);
            SetFloat(locomotion, "StableMovementSharpness", workingTuning.StableMovementSharpness);
            SetFloat(locomotion, "OrientationSharpness", workingTuning.OrientationSharpness);
            SetFloat(locomotion, "JumpSpeed", workingTuning.JumpSpeed);
            SetFloat(locomotion, "JumpBufferTime", workingTuning.JumpBufferTime);
            SetFloat(locomotion, "CoyoteTime", workingTuning.CoyoteTime);
            SetFloat(locomotion, "Gravity", workingTuning.Gravity);
            SetFloat(locomotion, "FallGravityMultiplier", workingTuning.FallGravityMultiplier);
            SetFloat(locomotion, "Drag", workingTuning.Drag);
            serializedProfile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            status = "Runtime locomotion values saved. The ability loadout was left unchanged.";
            statusType = MessageType.Info;
        }

        private static bool FloatField(ref float value, string label)
        {
            float before = value;
            value = EditorGUILayout.FloatField(label, value);
            return !SameFloat(before, value);
        }

        private static bool SameFloat(float left, float right)
        {
            if (float.IsNaN(left) || float.IsNaN(right)) return float.IsNaN(left) && float.IsNaN(right);
            if (float.IsInfinity(left) || float.IsInfinity(right)) return left == right;
            return Mathf.Approximately(left, right);
        }

        private static bool IsFinite(LocomotionTuning tuning)
        {
            return Finite(tuning.StableMoveSpeed) && Finite(tuning.AirMoveSpeed) &&
                Finite(tuning.AirAcceleration) && Finite(tuning.StableMovementSharpness) &&
                Finite(tuning.OrientationSharpness) && Finite(tuning.JumpSpeed) &&
                Finite(tuning.JumpBufferTime) && Finite(tuning.CoyoteTime) &&
                Finite(tuning.Gravity) && Finite(tuning.FallGravityMultiplier) &&
                Finite(tuning.Drag);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void SetFloat(SerializedProperty parent, string name, float value)
        {
            SerializedProperty child = parent.FindPropertyRelative(name);
            if (child != null) child.floatValue = value;
        }

        private void SetResult(PlayerOperationResult result, string acceptedMessage)
        {
            statusType = result == PlayerOperationResult.Accepted ? MessageType.Info : MessageType.Warning;
            status = result == PlayerOperationResult.Accepted
                ? acceptedMessage
                : "Runtime request rejected: " + result + ".";
        }
    }
}
#endif
