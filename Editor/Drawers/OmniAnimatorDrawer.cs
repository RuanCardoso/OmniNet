/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Omni.Core.Inspector
{
    [CustomPropertyDrawer(typeof(AnimatorParameter), true)]
    [CanEditMultipleObjects]
    public class OmniAnimatorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty parameterMode = property.FindPropertyRelative("syncMode");
            SerializedProperty parameterName = property.FindPropertyRelative("parameterName");
            SerializedProperty parameterType = property.FindPropertyRelative("parameterType");

            if (parameterMode != null && parameterName != null && parameterType != null)
            {
                int indexEnumValue = parameterType.intValue;
                AnimatorControllerParameterType paramaterTypeName = (AnimatorControllerParameterType)indexEnumValue;
                float fieldWidth = EditorGUIUtility.labelWidth;
                label.text = $"{parameterName.stringValue} : {paramaterTypeName}";
                EditorGUIUtility.labelWidth = 0;
                EditorGUI.PropertyField(position, parameterMode, label);
                EditorGUIUtility.labelWidth = fieldWidth;
            }
            else base.OnGUI(position, property, label);
        }
    }
}
#endif