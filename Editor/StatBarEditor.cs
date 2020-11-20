using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;
using Visage.Runtime;

namespace Visage.Editor
{
    [CustomEditor(typeof(StatBar), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the StatBar Component.
    /// </summary>
    public class StatBarEditor : UnityEditor.Editor
    {
        SerializedProperty m_Direction;
        SerializedProperty m_FillRect;
        SerializedProperty m_HandleRect;
        SerializedProperty m_MinValue;
        SerializedProperty m_MaxValue;
        SerializedProperty m_WholeNumbers;
        SerializedProperty m_Value;
        SerializedProperty m_OnValueChanged;

        protected void OnEnable()
        {
            m_FillRect = serializedObject.FindProperty("m_FillRect");
            m_HandleRect = serializedObject.FindProperty("m_HandleRect");
            m_Direction = serializedObject.FindProperty("m_Direction");
            m_MinValue = serializedObject.FindProperty("m_MinValue");
            m_MaxValue = serializedObject.FindProperty("m_MaxValue");
            m_WholeNumbers = serializedObject.FindProperty("m_WholeNumbers");
            m_Value = serializedObject.FindProperty("m_Value");
            m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_FillRect);
            EditorGUILayout.PropertyField(m_HandleRect);

            if (m_FillRect.objectReferenceValue != null || m_HandleRect.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_Direction);
                if (EditorGUI.EndChangeCheck())
                {
                    StatBar.Direction direction = (StatBar.Direction)m_Direction.enumValueIndex;
                    foreach (var obj in serializedObject.targetObjects)
                    {
                        StatBar statBar = obj as StatBar;
                        statBar.SetDirection(direction, true);
                    }
                }

                EditorGUILayout.PropertyField(m_MinValue);
                EditorGUILayout.PropertyField(m_MaxValue);
                EditorGUILayout.PropertyField(m_WholeNumbers);
                EditorGUILayout.Slider(m_Value, m_MinValue.floatValue, m_MaxValue.floatValue);

               

                // Draw the event notification options
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_OnValueChanged);
            }
            else
            {
                EditorGUILayout.HelpBox("Specify a RectTransform for the slider fill or the slider handle or both. Each must have a parent RectTransform that it can slide within.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}