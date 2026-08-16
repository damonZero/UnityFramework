using UnityEditor;
using UnityEngine;

namespace Framework.Coverage.Editor
{
    [CustomPropertyDrawer(typeof(AreaInfo))]
    public class ArenaInfoDrawer : PropertyDrawer
    {
        private static int[] offsetArray = new int[4];

        private static GUIContent[] offsetLabels =
        {
            new GUIContent("上"),
            new GUIContent("下"),
            new GUIContent("左"),
            new GUIContent("右")
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //获取绘制描述类
            var anchorTrans = property.FindPropertyRelative("anchorTrans");
            var offsetUp = property.FindPropertyRelative("offsetUp");
            var offsetDown = property.FindPropertyRelative("offsetDown");
            var offsetLeft = property.FindPropertyRelative("offsetLeft");
            var offsetRight = property.FindPropertyRelative("offsetRight");

            var rect = new Rect(position) {height = EditorGUIUtility.singleLineHeight};
            EditorGUI.LabelField(rect, "Anchor:");
            rect.x += 60;
            rect.width = 300;
            anchorTrans.objectReferenceValue =
                EditorGUI.ObjectField(rect, anchorTrans.objectReferenceValue, typeof(RectTransform), true);

            rect.x = position.x;
            rect.y += 20;
            EditorGUI.LabelField(rect, "Offset:");
            rect.x += 60;
            offsetArray[0] = offsetUp.intValue;
            offsetArray[1] = offsetDown.intValue;
            offsetArray[2] = offsetLeft.intValue;
            offsetArray[3] = offsetRight.intValue;
            EditorGUI.MultiIntField(rect, offsetLabels, offsetArray);
            offsetUp.intValue = offsetArray[0];
            offsetDown.intValue = offsetArray[1];
            offsetLeft.intValue = offsetArray[2];
            offsetRight.intValue = offsetArray[3];
        }
    }
}
