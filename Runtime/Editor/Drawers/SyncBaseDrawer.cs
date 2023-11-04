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
using Omni.Editor;
using UnityEditor;
using UnityEngine;

namespace Omni.Core.Inspector
{
    [CustomPropertyDrawer(typeof(SyncBase<>), true)]
    [CanEditMultipleObjects]
    public class SyncBaseDrawer : PropertyDrawer
    {
        private const string FIELD_NAME = "value";
        private Texture2D quadTexture;
        private SerializedProperty property;
        private object target;

        private ISyncBase GetISyncBase(object target) => target as ISyncBase;
        private object GetTarget(SerializedProperty property) => target ??= PropertyUtility.GetTargetObjectWithProperty(property);
        private SerializedProperty GetProperty(SerializedProperty property) => this.property ??= property.FindPropertyRelative(FIELD_NAME);
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty TValue = GetProperty(property);
#pragma warning disable IDE0046
            if (TValue != null)
                return EditorGUI.GetPropertyHeight(TValue, label, true);
            else return base.GetPropertyHeight(property, label);
#pragma warning restore IDE0046
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = $" | {property.displayName}";
            label.image = GetTexture();
            SerializedProperty TValue = GetProperty(property);
            if (TValue != null)
            {
                EditorGUI.BeginChangeCheck();
                ISyncBase ISyncBase = GetISyncBase(GetTarget(TValue));
                if (!ISyncBase.IsEnum())
                {
                    object value = PropertyUtility.GetTargetObjectOfProperty(TValue);
                    if (value is Trigger)
                    {
                        position = EditorGUI.PrefixLabel(position, label);
                        if (GUI.Button(position, "Trigger", new(EditorStyles.miniButton) { fontSize = 11 }))
                            ISyncBase.OnValueChanged();
                    }
                    else EditorGUI.PropertyField(position, TValue, label, true);
                }
                else
                    ISyncBase.SetEnum(EditorGUI.EnumPopup(position, label, ISyncBase.GetEnum()));
                if (EditorGUI.EndChangeCheck() && property.serializedObject.ApplyModifiedPropertiesWithoutUndo())
                    ISyncBase.OnValueChanged();
            }
            else
            {
                position = EditorGUI.PrefixLabel(position, label);
                EditorGUI.LabelField(position, "Is Not Serializable!");
            }
        }

        private Texture2D GetTexture()
        {
            if (quadTexture == null)
            {
                Texture2D whiteTxt = Texture2D.whiteTexture;
                quadTexture = new(whiteTxt.width, whiteTxt.height);
                #region Set Color
                for (int y = 0; y < quadTexture.height; y++)
                {
                    for (int x = 0; x < quadTexture.width; x++)
                    {
                        quadTexture.SetPixel(x, y, Color.green);
                    }
                }
                #endregion
                quadTexture.Apply();
            }
            return quadTexture;
        }
    }
}
#endif