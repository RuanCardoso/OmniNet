using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Omni.Editor
{
	[CustomPropertyDrawer(typeof(ProgressBarAttribute))]
	public class ProgressBarPropertyDrawer : PropertyDrawerBase
	{
		protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
		{
			ProgressBarAttribute progressBarAttribute = PropertyUtility.GetAttribute<ProgressBarAttribute>(property);
			var maxValue = GetMaxValue(property, progressBarAttribute);

			return IsNumber(property) && IsNumber(maxValue)
				? GetPropertyHeight(property)
				: GetPropertyHeight(property) + GetHelpBoxHeight();
		}

		protected override void OnGUI_Internal(Rect rect, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(rect, label, property);

			if (!IsNumber(property))
			{
				string message = string.Format("Field {0} is not a number", property.name);
				DrawDefaultPropertyAndHelpBox(rect, property, message, MessageType.Warning);
				return;
			}

			ProgressBarAttribute progressBarAttribute = PropertyUtility.GetAttribute<ProgressBarAttribute>(property);
			var value = property.propertyType == SerializedPropertyType.Integer ? property.intValue : property.floatValue;
			var valueFormatted = property.propertyType == SerializedPropertyType.Integer ? value.ToString() : string.Format("{0:0.00}", value);
			var maxValue = GetMaxValue(property, progressBarAttribute);

			if (maxValue != null && IsNumber(maxValue))
			{
				// Define the layout for the progress bar and input field
				Rect progressBarRect = rect;
				progressBarRect.width -= EditorGUIUtility.singleLineHeight + 23; // Adjust for input field
				Rect inputFieldRect = new Rect(progressBarRect.xMax + 5, rect.y, EditorGUIUtility.singleLineHeight * 2, EditorGUIUtility.singleLineHeight);

				// Handle mouse input
				Event guiEvent = Event.current;
				bool isMouseDrag = (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && rect.Contains(guiEvent.mousePosition));
				bool isMouseClick = (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && rect.Contains(guiEvent.mousePosition));

				if (isMouseDrag || isMouseClick)
				{
					float normalizedValue = Mathf.Clamp01((guiEvent.mousePosition.x - progressBarRect.x) / progressBarRect.width);
					value = Mathf.Lerp(0f, CastToFloat(maxValue), normalizedValue);
					if (property.propertyType == SerializedPropertyType.Integer)
					{
						property.intValue = Mathf.RoundToInt(value);
					}
					else
					{
						property.floatValue = value;
					}
					property.serializedObject.ApplyModifiedProperties();
				}

				// Calculate color
				Color barColor = progressBarAttribute.Color.GetColor();
				if (progressBarAttribute.Color == EColor.Auto)
				{
					float normalizedValue = value / CastToFloat(maxValue);
					barColor = Color.Lerp(Color.red, Color.green, normalizedValue);
				}

				// Draw progress bar
				var fillPercentage = value / CastToFloat(maxValue);
				var barLabel = (!string.IsNullOrEmpty(progressBarAttribute.Name) ? "[" + progressBarAttribute.Name + "] " : "[" + label.text + "] ") + valueFormatted + "/" + maxValue;
				var labelColor = Color.white;

				var indentLength = NaughtyEditorGUI.GetIndentLength(progressBarRect);
				Rect barRect = new Rect()
				{
					x = progressBarRect.x + indentLength,
					y = progressBarRect.y,
					width = progressBarRect.width - indentLength,
					height = EditorGUIUtility.singleLineHeight
				};

				DrawBar(barRect, Mathf.Clamp01(fillPercentage), barLabel, barColor, labelColor);

				// Draw input field
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(inputFieldRect, property, GUIContent.none);
				if (EditorGUI.EndChangeCheck())
				{
					// Ensure the value stays within bounds
					if (property.propertyType == SerializedPropertyType.Integer)
					{
						property.intValue = Mathf.Clamp(property.intValue, 0, (int)CastToFloat(maxValue));
					}
					else
					{
						property.floatValue = Mathf.Clamp(property.floatValue, 0f, CastToFloat(maxValue));
					}
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			else
			{
				string message = string.Format(
					"The provided dynamic max value for the progress bar is not correct. Please check if the '{0}' is correct, or the return type is float/int",
					nameof(progressBarAttribute.MaxValueName));

				DrawDefaultPropertyAndHelpBox(rect, property, message, MessageType.Warning);
			}

			EditorGUI.EndProperty();
		}

		private object GetMaxValue(SerializedProperty property, ProgressBarAttribute progressBarAttribute)
		{
			if (string.IsNullOrEmpty(progressBarAttribute.MaxValueName))
			{
				return progressBarAttribute.MaxValue;
			}
			else
			{
				object target = PropertyUtility.GetTargetObjectWithProperty(property);

				FieldInfo valuesFieldInfo = ReflectionUtility.GetField(target, progressBarAttribute.MaxValueName);
				if (valuesFieldInfo != null)
				{
					return valuesFieldInfo.GetValue(target);
				}

				PropertyInfo valuesPropertyInfo = ReflectionUtility.GetProperty(target, progressBarAttribute.MaxValueName);
				if (valuesPropertyInfo != null)
				{
					return valuesPropertyInfo.GetValue(target);
				}

				MethodInfo methodValuesInfo = ReflectionUtility.GetMethod(target, progressBarAttribute.MaxValueName);
				if (methodValuesInfo != null &&
					(methodValuesInfo.ReturnType == typeof(float) || methodValuesInfo.ReturnType == typeof(int)) &&
					methodValuesInfo.GetParameters().Length == 0)
				{
					return methodValuesInfo.Invoke(target, null);
				}

				return null;
			}
		}

		private void DrawBar(Rect rect, float fillPercent, string label, Color barColor, Color labelColor)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var fillRect = new Rect(rect.x, rect.y, rect.width * fillPercent, rect.height);

			EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));
			EditorGUI.DrawRect(fillRect, barColor);

			// set alignment and cache the default
			var align = GUI.skin.label.alignment;
			GUI.skin.label.alignment = TextAnchor.UpperCenter;

			// set the color and cache the default
			var c = GUI.contentColor;
			GUI.contentColor = labelColor;

			// calculate the position
			var labelRect = new Rect(rect.x, rect.y - 2, rect.width, rect.height);

			// draw~
			EditorGUI.DropShadowLabel(labelRect, label);

			// reset color and alignment
			GUI.contentColor = c;
			GUI.skin.label.alignment = align;
		}

		private bool IsNumber(SerializedProperty property)
		{
			bool isNumber = property.propertyType == SerializedPropertyType.Float || property.propertyType == SerializedPropertyType.Integer;
			return isNumber;
		}

		private bool IsNumber(object obj)
		{
			return (obj is float) || (obj is int);
		}

		private float CastToFloat(object obj)
		{
			if (obj is int)
			{
				return (int)obj;
			}
			else
			{
				return (float)obj;
			}
		}
	}
}
