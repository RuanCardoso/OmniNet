using UnityEditor;
using UnityEngine;

namespace Omni.Editor
{
    [CustomPropertyDrawer(typeof(HorizontalLineAttribute))]
    public class HorizontalLineDecoratorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            HorizontalLineAttribute lineAttr = (HorizontalLineAttribute)attribute;
            return EditorGUIUtility.singleLineHeight + lineAttr.Height;
        }

        public override void OnGUI(Rect position)
        {
            HorizontalLineAttribute lineAttr = (HorizontalLineAttribute)attribute;
            if (!lineAttr.Below)
            {
                Rect rect = EditorGUI.IndentedRect(position);
                rect.y += EditorGUIUtility.singleLineHeight / 3.0f;
                NaughtyEditorGUI.HorizontalLine(rect, lineAttr.Height, lineAttr.Color.GetColor());
            }
            else DrawBelow(Color.red);
        }

        public void DrawBelow(Color color, int thickness = 2, int padding = 10)
        {
            HorizontalLineAttribute lineAttr = (HorizontalLineAttribute)attribute;
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += padding / 2;
            NaughtyEditorGUI.HorizontalLine(rect, lineAttr.Height, lineAttr.Color.GetColor());
        }
    }
}