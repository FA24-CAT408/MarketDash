#if UNITY_EDITOR
using UnityEditor;

namespace CrazyMarket.Player.V2.Editor
{
    [CustomEditor(typeof(PlayerProfile))]
    public sealed class PlayerProfileEditor : UnityEditor.Editor
    {
        private bool showGround = true;
        private bool showAir = true;
        private bool showMiscellaneous = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("profileId"));

            SerializedProperty data = serializedObject.FindProperty("data");
            SerializedProperty tuning = data == null ? null : data.FindPropertyRelative("locomotion");
            if (tuning == null)
                EditorGUILayout.HelpBox("This profile has no locomotion data.", MessageType.Error);
            else
                DrawTuning(tuning, ref showGround, ref showAir, ref showMiscellaneous);

            serializedObject.ApplyModifiedProperties();
        }

        internal static void DrawTuning(SerializedProperty tuning, ref bool showGround,
            ref bool showAir, ref bool showMiscellaneous)
        {
            showGround = EditorGUILayout.Foldout(showGround, "Ground Movement", true);
            if (showGround)
            {
                EditorGUI.indentLevel++;
                Draw(tuning, "StableMoveSpeed", "Stable Move Speed");
                Draw(tuning, "StableMovementSharpness", "Stable Movement Sharpness");
                EditorGUI.indentLevel--;
            }

            showAir = EditorGUILayout.Foldout(showAir, "Air Movement", true);
            if (showAir)
            {
                EditorGUI.indentLevel++;
                Draw(tuning, "AirMoveSpeed", "Air Move Speed");
                Draw(tuning, "AirAcceleration", "Air Acceleration");
                Draw(tuning, "JumpSpeed", "Jump Speed");
                Draw(tuning, "JumpBufferTime", "Jump Buffer Time");
                Draw(tuning, "CoyoteTime", "Coyote Time");
                Draw(tuning, "Gravity", "Gravity");
                Draw(tuning, "FallGravityMultiplier", "Fall Gravity Multiplier");
                Draw(tuning, "Drag", "Drag");
                EditorGUI.indentLevel--;
            }

            showMiscellaneous = EditorGUILayout.Foldout(showMiscellaneous, "Miscellaneous", true);
            if (showMiscellaneous)
            {
                EditorGUI.indentLevel++;
                Draw(tuning, "OrientationSharpness", "Orientation Sharpness");
                EditorGUI.indentLevel--;
            }
        }

        private static void Draw(SerializedProperty tuning, string propertyName, string label)
        {
            SerializedProperty property = tuning.FindPropertyRelative(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, new UnityEngine.GUIContent(label));
        }
    }
}
#endif
