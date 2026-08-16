using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class FileIDHelper
    {
        public static long GetFileID(Object obj)
        {
            if (obj == null)
            {
                Debug.LogError("Object is null");
                return 0;
            }

            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            SerializedObject serializedObject = new SerializedObject(obj);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
            return localIdProp.longValue;
        }
    }
}