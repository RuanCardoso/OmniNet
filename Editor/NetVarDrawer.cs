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
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Omni.Core.Inspector
{
	[CustomPropertyDrawer(typeof(NetVarAttribute), true)]
	[CanEditMultipleObjects]
	public class NetVarDrawer : PropertyDrawerBase
	{
		private Texture2D quadTexture;
		private PropertyInfo propertyInfo;
		protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			if (property.serializedObject.targetObject is NetworkBehaviour monoBehaviour)
			{
				// Validate naming convetion
				string fieldName = fieldInfo.Name;
				if (fieldName.Contains("M_") || char.IsUpper(fieldName[0]))
				{
					OmniLogger.PrintError("NetVar fields must always begin with the first lowercase letter.");
					return;
				}

				// Find the property
				Type type = monoBehaviour.GetType();
				string propertyName = fieldName.Replace("m_", "");
				propertyName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
				propertyInfo ??= type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly); // ??= Optimization.
				if (propertyInfo != null)
				{
					label.text = $" | {property.displayName}";
					label.image = GetTexture();
					// Update the property!
					EditorGUI.BeginChangeCheck();
					EditorGUI.PropertyField(position, property, label, true);
					if (EditorGUI.EndChangeCheck() && Application.isPlaying)
					{
						try
						{
							if (propertyInfo.PropertyType == property.boxedValue.GetType())
							{
								propertyInfo.SetValue(monoBehaviour, property.boxedValue);
							}
							else
							{
								property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
								propertyInfo.SetValue(monoBehaviour, fieldInfo.GetValue(monoBehaviour));
							}
						}
						catch (Exception ex)
						{
							OmniLogger.PrintError($"Fatal Error: {ex.Message}");
							propertyInfo.SetValue(monoBehaviour, fieldInfo.GetValue(monoBehaviour));
						}
					}

					if (UpdateWhenLengthChanges(monoBehaviour) && Application.isPlaying)
					{
						propertyInfo.SetValue(monoBehaviour, fieldInfo.GetValue(monoBehaviour));
					}
				}
			}
			EditorGUI.EndProperty();
		}

		private int lastCount = 0;
		private bool UpdateWhenLengthChanges(MonoBehaviour monoBehaviour)
		{
			if (fieldInfo.GetValue(monoBehaviour) is IEnumerable enumerator)
			{
				int count = 0;
				foreach (var item in enumerator)
					count++;

				if (count != lastCount)
				{
					lastCount = count;
					return true;
				}
				else return false;
			}
			else return false;
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