using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Visage.StatBarUI.Runtime;

namespace Visage.StatBarUI.Editor
{
    [CustomEditor(typeof(StatBar), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the StatBar Component.
    /// </summary>
    public class StatBarEditor : UnityEditor.Editor
    {
        SerializedProperty _direction;
        SerializedProperty _fillRect;
        SerializedProperty _minValue;
        SerializedProperty _maxValue;
        SerializedProperty _wholeNumbers;
        SerializedProperty _value;
        SerializedProperty _onValueChanged;

        SerializedProperty _transition;
        SerializedProperty _normalColorTintTransition;
        SerializedProperty _lowColorTintTransition;
        SerializedProperty _criticalColorTintTransition;

        SerializedProperty _valueLabel;
        SerializedProperty _leadingZeroes;

        protected void OnEnable()
        {
            _fillRect = serializedObject.FindProperty("_fillRect");
            _direction = serializedObject.FindProperty("_direction");
            _minValue = serializedObject.FindProperty("_minValue");
            _maxValue = serializedObject.FindProperty("_maxValue");
            _wholeNumbers = serializedObject.FindProperty("_wholeNumbers");
            _value = serializedObject.FindProperty("_value");
            _onValueChanged = serializedObject.FindProperty("_onValueChanged");

            _transition = serializedObject.FindProperty("_transition");
            _normalColorTintTransition = serializedObject.FindProperty("_normalColorTintTransition");
            _lowColorTintTransition = serializedObject.FindProperty("_lowColorTintTransition");
            _criticalColorTintTransition = serializedObject.FindProperty("_criticalColorTintTransition");

            _valueLabel = serializedObject.FindProperty("_valueLabel");
            _leadingZeroes = serializedObject.FindProperty("_leadingZeroes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_fillRect);

            if (_fillRect.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_direction);
                if (EditorGUI.EndChangeCheck())
                {
                    StatBar.FillDirection direction = (StatBar.FillDirection)_direction.enumValueIndex;
                    foreach (var obj in serializedObject.targetObjects)
                    {
                        StatBar statBar = obj as StatBar;
                        statBar.SetDirection(direction, true);
                    }
                }

                EditorGUILayout.PropertyField(_minValue);
                EditorGUILayout.PropertyField(_maxValue);
                EditorGUILayout.PropertyField(_wholeNumbers);
                EditorGUILayout.Slider(_value, _minValue.floatValue, _maxValue.floatValue);

                DrawTransition();

                //Draw the info area
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_valueLabel);
                if (_valueLabel.objectReferenceValue != null)
                {
                    var textHolder = _valueLabel.objectReferenceValue as RectTransform;
                    if (textHolder.GetComponent<Text>() ||
                        textHolder.GetComponent<TMPro.TMP_Text>())
                    {
                        EditorGUILayout.IntSlider(_leadingZeroes, 0, 8);
                    }
                    else
                    {
                        _valueLabel.objectReferenceValue = null;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Specify a RectTransform whith a Text or TMP_Text component for the stat bar to set the value.", MessageType.Info);
                }


                // Draw the event notification options
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_onValueChanged);
            }
            else
            {
                EditorGUILayout.HelpBox("Specify a RectTransform for the stat bar to fill. It must have a parent RectTransform that it can slide within.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();

            void DrawTransition()
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_transition);
                StatBar.Transition transition = (StatBar.Transition)_transition.enumValueIndex;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in serializedObject.targetObjects)
                    {
                        StatBar statBar = obj as StatBar;
                        statBar.SetTransition(transition);
                    }
                }
                if (transition == StatBar.Transition.ColorTint)
                {
                    EditorGUI.indentLevel++;

                    DrawColorTintTransition(_normalColorTintTransition, "Normal");
                    DrawColorTintTransition(_lowColorTintTransition, "Low");
                    DrawColorTintTransition(_criticalColorTintTransition, "Critical");

                    EditorGUI.indentLevel--;
                }

                void DrawColorTintTransition(SerializedProperty property, string labelText)
                {
                    var percent = property.FindPropertyRelative("percent");
                    var color = property.FindPropertyRelative("color");
                    EditorGUILayout.LabelField(labelText);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(percent);
                    EditorGUILayout.PropertyField(color);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}