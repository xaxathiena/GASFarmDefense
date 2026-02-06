using UnityEditor;

namespace GAS.Editor
{
    [CustomEditor(typeof(GameplayAbility), true)]
    public class GameplayAbilityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Gameplay Ability", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}