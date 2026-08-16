using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Framework.View.Editor
{
    #region 自动绑定变量

    /// <summary>
    /// 自动绑定变量
    /// </summary>
    [CustomEditor(typeof(ViewObject), true)]
    public partial class ViewObjectEditor : UnityEditor.Editor
    {
        private readonly VarBind _bindTool = new();

        private SerializedProperty _bindData;
        private SerializedProperty _keys;
        private SerializedProperty _values;
        private static bool _showParentFields;

        private CachedBindingInfo _cachedInfo;
        private int _lastBindDataHash;
        private Type _lastTargetType;
        private bool _cacheValid;

        private class CachedBindingInfo
        {
            public readonly Dictionary<Type, List<string>> typeToFields = new();
            public readonly Dictionary<string, Type> resolvedTypes = new();
            public readonly List<Type> typeHierarchy = new();
            public readonly HashSet<string> fieldsFromBindingFile = new();
        }

        private struct FieldDisplayInfo
        {
            public string FieldName { get; set; }
            public string DisplayName { get; set; }
            public string TypeStr { get; set; }
            public Type ResolvedType { get; set; }

            public bool IsValid => !string.IsNullOrEmpty(FieldName) && ResolvedType != null;
        }

        private void OnEnable()
        {
            _showParentFields = EditorPrefs.GetBool("ShowParentFields", false);
            if (serializedObject == null) return;

            _bindData = serializedObject.FindProperty("bindData");
            _keys = _bindData.FindPropertyRelative("keys");
            _values = _bindData.FindPropertyRelative("values");

            _cachedInfo = new CachedBindingInfo();
            _cacheValid = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            RefreshCacheIfNeeded();

            CustomDrawSerializedFields();

            serializedObject.ApplyModifiedProperties();

            AutoBinding();
            Replace();
        }

        private void RefreshCacheIfNeeded()
        {
            var targetBehaviour = target as ViewObject;
            if (targetBehaviour == null) return;

            var currentType = targetBehaviour.GetType();
            var currentHash = GetBindDataHash();

            if (_cacheValid && _lastTargetType == currentType && _lastBindDataHash == currentHash) return;

            RefreshCache(currentType);
            _lastTargetType = currentType;
            _lastBindDataHash = currentHash;
            _cacheValid = true;
        }

        private int GetBindDataHash()
        {
            if (_keys == null || _values == null) return 0;

            var hash = 17;
            hash = hash * 31 + _keys.arraySize;

            for (var i = 0; i < _keys.arraySize; i++)
            {
                var value = _values.GetArrayElementAtIndex(i);
                if (value == null) continue;
                var nameProp = value.FindPropertyRelative("_name");
                var typeStrProp = value.FindPropertyRelative("_typeStr");

                if (nameProp != null)
                    hash = hash * 31 + (nameProp.stringValue?.GetHashCode() ?? 0);
                if (typeStrProp != null)
                    hash = hash * 31 + (typeStrProp.stringValue?.GetHashCode() ?? 0);
            }

            return hash;
        }

        private void RefreshCache(Type currentType)
        {
            _cachedInfo.typeToFields.Clear();
            _cachedInfo.resolvedTypes.Clear();
            _cachedInfo.typeHierarchy.Clear();
            _cachedInfo.fieldsFromBindingFile.Clear();

            BuildTypeHierarchy(currentType);
            LoadFieldsFromBindingFiles(currentType);
            CacheFieldClassification(currentType);
            CacheTypeResolution();
        }

        private void BuildTypeHierarchy(Type currentType)
        {
            var parentType = currentType.BaseType;
            var tempType = parentType;

            while (tempType != null && tempType != typeof(MonoBehaviour) && tempType != typeof(object))
            {
                var bindingPath = VarBindTool.GetBindingFilePathForType(tempType);
                if (bindingPath != null)
                {
                    _cachedInfo.typeHierarchy.Add(tempType);
                }

                tempType = tempType.BaseType;
            }

            _cachedInfo.typeHierarchy.Reverse();
        }

        private void LoadFieldsFromBindingFiles(Type currentType)
        {
            _cachedInfo.typeToFields[currentType] = new List<string>();
            foreach (var type in _cachedInfo.typeHierarchy)
            {
                _cachedInfo.typeToFields[type] = new List<string>();
            }

            LoadFieldsFromBindingFile(currentType);

            foreach (var parentType in _cachedInfo.typeHierarchy)
            {
                LoadFieldsFromBindingFile(parentType);
            }
        }

        private void LoadFieldsFromBindingFile(Type targetType)
        {
            var bindingFilePath = VarBindTool.GetBindingFilePathForType(targetType);
            if (string.IsNullOrEmpty(bindingFilePath) || !System.IO.File.Exists(bindingFilePath))
            {
                return;
            }

            try
            {
                var content = System.IO.File.ReadAllText(bindingFilePath);
                var fields = ParseBindingFields(content);

                foreach (var fieldName in fields)
                {
                    _cachedInfo.typeToFields[targetType].Add(fieldName);
                    _cachedInfo.fieldsFromBindingFile.Add(fieldName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ViewObjectEditor] 读取 .Binding 文件失败: {e.Message}");
            }
        }

        private List<string> ParseBindingFields(string content)
        {
            var fields = new List<string>();

            var pattern = @"protected\s+[\w<>\[\]]+\s+(?<name>_\w+)\s*=>\s*GetBindField<[^>]+>\(nameof\(\k<name>\)\);";
            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Multiline);

            var matches = regex.Matches(content);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                fields.Add(match.Groups["name"].Value);
            }

            return fields;
        }

        private void CacheFieldClassification(Type currentType)
        {
            if (_keys == null || _values == null) return;

            for (var i = 0; i < _keys.arraySize; i++)
            {
                var value = _values.GetArrayElementAtIndex(i);

                var nameProp = value?.FindPropertyRelative("_name");
                if (nameProp == null) continue;

                var fieldName = nameProp.stringValue;
                if (string.IsNullOrEmpty(fieldName)) continue;

                if (_cachedInfo.fieldsFromBindingFile.Contains(fieldName))
                {
                    continue;
                }

                if (IsFieldInCurrentTypeOnly(fieldName, currentType))
                {
                    _cachedInfo.typeToFields[currentType].Add(fieldName);
                }
                else
                {
                    foreach (var parentType in _cachedInfo.typeHierarchy)
                    {
                        if (!VarBindTool.IsFieldInType(fieldName, parentType)) continue;
                        _cachedInfo.typeToFields[parentType].Add(fieldName);
                        break;
                    }
                }
            }
        }

        private void CacheTypeResolution()
        {
            if (_values == null) return;

            for (var i = 0; i < _values.arraySize; i++)
            {
                var value = _values.GetArrayElementAtIndex(i);

                var typeStrProp = value?.FindPropertyRelative("_typeStr");
                if (typeStrProp == null || string.IsNullOrEmpty(typeStrProp.stringValue)) continue;

                var typeStr = typeStrProp.stringValue;
                if (_cachedInfo.resolvedTypes.ContainsKey(typeStr)) continue;
                var resolvedType = ResolveFieldTypeInternal(typeStr);
                _cachedInfo.resolvedTypes[typeStr] = resolvedType;
            }
        }

        private bool IsFieldInCurrentTypeOnly(string fieldName, Type type)
        {
            if (!VarBindTool.IsFieldInType(fieldName, type))
                return false;

            var parentType = type.BaseType;
            while (parentType != null && parentType != typeof(MonoBehaviour) && parentType != typeof(object))
            {
                if (VarBindTool.IsFieldInType(fieldName, parentType))
                    return false;
                parentType = parentType.BaseType;
            }

            return true;
        }

        private void CustomDrawSerializedFields()
        {
            if (_bindData == null || _keys == null || _values == null || !_cacheValid)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("自定义变量绑定", EditorStyles.boldLabel);

            var targetBehaviour = target as ViewObject;
            if (!targetBehaviour) return;

            var currentType = targetBehaviour.GetType();

            EditorGUI.indentLevel++;
            CustomDrawBindingItemsCached("当前类字段", currentType);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            var showParentFields = EditorGUILayout.Foldout(_showParentFields, "继承的绑定");
            if (showParentFields != _showParentFields)
            {
                _showParentFields = showParentFields;
                EditorPrefs.SetBool("ShowParentFields", _showParentFields);
            }

            if (!_showParentFields) return;
            EditorGUI.indentLevel++;
            DrawAllParentFieldsCached();
            EditorGUI.indentLevel--;
        }

        private void DrawAllParentFieldsCached()
        {
            foreach (var type in _cachedInfo.typeHierarchy)
            {
                if (_cachedInfo.typeToFields[type].Count <= 0) continue;
                CustomDrawBindingItemsCached(type.Name + " (父类)", type);
            }
        }

        private void CustomDrawBindingItemsCached(string groupName, Type type)
        {
            if (!_cachedInfo.typeToFields.ContainsKey(type) || _cachedInfo.typeToFields[type].Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(groupName, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var fieldsToShow = _cachedInfo.typeToFields[type];

            for (var i = 0; i < _keys.arraySize; i++)
            {
                var value = _values.GetArrayElementAtIndex(i);
                if (value == null) continue;

                var nameProp = value.FindPropertyRelative("_name");
                if (nameProp == null) continue;

                var fieldName = nameProp.stringValue;
                if (!fieldsToShow.Contains(fieldName)) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);

                var hasSerializedData = _cachedInfo.fieldsFromBindingFile.Contains(fieldName) ||
                                      value.FindPropertyRelative("_singleObj")?.objectReferenceValue != null ||
                                      value.FindPropertyRelative("_multiObjs")?.arraySize > 0;

                if (_cachedInfo.fieldsFromBindingFile.Contains(fieldName) && !hasSerializedData)
                {
                    var labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    labelStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"{fieldName} (缺少数据)", labelStyle);
                    EditorGUILayout.HelpBox("该绑定缺少序列化数据，可能需要重新绑定", MessageType.Warning);
                }
                else
                {
                    var displayNameProp = value.FindPropertyRelative("_displayName");
                    if (displayNameProp != null)
                    {
                        EditorGUILayout.PropertyField(displayNameProp, new GUIContent("显示名称（可更改）"));
                    }

                    var multiObjsProp = value.FindPropertyRelative("_multiObjs");

                    if (multiObjsProp is { isArray: true, arraySize: > 0 })
                    {
                        CustomDrawMultiObjectBindingCached(multiObjsProp);
                    }
                    else
                    {
                        DrawSingleObjectBindingCached(value);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            foreach (var fieldName in fieldsToShow)
            {
                var hasSerializedData = false;
                for (var j = 0; j < _keys.arraySize; j++)
                {
                    var key = _keys.GetArrayElementAtIndex(j).stringValue;
                    if (key == fieldName)
                    {
                        hasSerializedData = true;
                        break;
                    }
                }

                if (hasSerializedData) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                var labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField($"{fieldName} (缺少数据)", labelStyle);
                EditorGUILayout.HelpBox("该绑定缺少序列化数据，可能需要重新绑定", MessageType.Warning);
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        private void CustomDrawMultiObjectBindingCached(SerializedProperty multiObjsProp)
        {
            var parentProp = GetParentProperty(multiObjsProp);
            if (parentProp == null)
            {
                EditorGUILayout.HelpBox("无法获取绑定数据", MessageType.Warning);
                return;
            }

            var fieldInfo = GetFieldDisplayInfo(parentProp);
            if (!fieldInfo.IsValid)
            {
                EditorGUILayout.HelpBox($"字段信息无效: {fieldInfo.FieldName}", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"{fieldInfo.DisplayName} (列表):", EditorStyles.boldLabel);

            DrawObjectArray(multiObjsProp, fieldInfo.ResolvedType);
        }

        private void DrawSingleObjectBindingCached(SerializedProperty bindDataProperty)
        {
            var fieldInfo = GetFieldDisplayInfo(bindDataProperty);
            if (!fieldInfo.IsValid)
            {
                EditorGUILayout.HelpBox($"字段信息无效: {fieldInfo.FieldName}", MessageType.Warning);
                return;
            }

            var singleObjProp = bindDataProperty.FindPropertyRelative("_singleObj");
            if (singleObjProp == null)
            {
                EditorGUILayout.HelpBox("未找到绑定对象", MessageType.Warning);
                return;
            }

            DrawSingleObjectField(singleObjProp, fieldInfo.DisplayName, fieldInfo.ResolvedType);
        }

        private void DrawObjectArray(SerializedProperty arrayProp, Type elementType)
        {
            var isValidType = elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType);

            EditorGUI.indentLevel++;
            for (var i = 0; i < arrayProp.arraySize; i++)
            {
                var item = arrayProp.GetArrayElementAtIndex(i);
                var label = new GUIContent($"元素 {i}");

                if (isValidType)
                {
                    EditorGUI.BeginChangeCheck();
                    var newObj = EditorGUILayout.ObjectField(label, item.objectReferenceValue, elementType, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        item.objectReferenceValue = newObj;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(item, label);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSingleObjectField(SerializedProperty objProp, string displayName, Type fieldType)
        {
            var label = new GUIContent(displayName);

            if (fieldType != null && typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                EditorGUI.BeginChangeCheck();
                var newObj = EditorGUILayout.ObjectField(label, objProp.objectReferenceValue, fieldType, true);
                if (EditorGUI.EndChangeCheck())
                {
                    objProp.objectReferenceValue = newObj;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(objProp, label);
            }
        }

        private Type GetCachedResolvedType(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return null;
            var found = _cachedInfo.resolvedTypes.TryGetValue(typeStr, out var type);
            return found ? type : null;
        }

        private Type ResolveFieldTypeInternal(string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
                return null;

            Type fieldType = null;
            try
            {
                fieldType = Type.GetType(typeStr);
                if (fieldType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        fieldType = assembly.GetType(typeStr);
                        if (fieldType == null) continue;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ViewObjectEditor] 类型解析错误: {typeStr}, 错误: {e.Message}");
            }

            return fieldType;
        }

        private FieldDisplayInfo GetFieldDisplayInfo(SerializedProperty bindDataProperty)
        {
            var info = new FieldDisplayInfo();

            var nameProp = bindDataProperty.FindPropertyRelative("_name");
            info.FieldName = nameProp?.stringValue ?? "Unknown";

            var displayNameProp = bindDataProperty.FindPropertyRelative("_displayName");
            info.DisplayName = !string.IsNullOrEmpty(displayNameProp?.stringValue)
                ? displayNameProp.stringValue
                : info.FieldName;

            var typeStrProp = bindDataProperty.FindPropertyRelative("_typeStr");
            info.TypeStr = typeStrProp?.stringValue;
            info.ResolvedType = GetCachedResolvedType(info.TypeStr);

            return info;
        }

        private SerializedProperty GetParentProperty(SerializedProperty childProperty)
        {
            var propertyPath = childProperty.propertyPath;
            var lastDotIndex = propertyPath.LastIndexOf('.');

            if (lastDotIndex == -1) return null;

            var parentPath = propertyPath[..lastDotIndex];
            return childProperty.serializedObject.FindProperty(parentPath);
        }

        private void InvalidateCache()
        {
            _cacheValid = false;
        }

        private HashSet<string> GetAllDeclaredFields()
        {
            var declaredFields = new HashSet<string>();

            RefreshCacheIfNeeded();

            foreach (var fieldList in _cachedInfo.fieldsFromBindingFile)
            {
                declaredFields.Add(fieldList);
            }

            return declaredFields;
        }

        private void AutoBinding()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("变量绑定工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var targetBehaviour = target as ViewObject;
            var bindingCount = 0;

            if (_keys != null && _values != null)
            {
                bindingCount = _keys.arraySize;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"当前绑定数量:", GUILayout.Width(100));

            var style = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = bindingCount > 0 ? new Color(0.2f, 0.6f, 1f) : new Color(0.6f, 0.6f, 0.6f) },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"{bindingCount} 个变量", style);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            var originalBackColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

            if (GUILayout.Button("🔗 自动绑定变量", GUILayout.Height(30)))
            {
                try
                {
                    _bindTool.AutoBinding(target as ViewObject);
                    InvalidateCache();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ViewObjectEditor] 自动绑定错误: {e}");
                }
            }

            GUI.backgroundColor = originalBackColor;
            GUILayout.Space(10);

            GUI.backgroundColor = bindingCount > 0 ? new Color(0.8f, 0.4f, 0.4f) : new Color(0.8f, 0.8f, 0.8f);

            if (GUILayout.Button("🗑️ 清理自动绑定", GUILayout.Height(30)))
            {
                try
                {
                    _bindTool.ClearBinding(target as ViewObject);
                    InvalidateCache();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ViewObjectEditor] 清理绑定错误: {e}");
                }
            }

            GUI.backgroundColor = originalBackColor;
            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);

            if (GUILayout.Button("📌 绑定现有变量", GUILayout.Height(30)))
            {
                try
                {
                    var declaredFields = GetAllDeclaredFields();
                    _bindTool.BindExistingVariables(target as ViewObject, declaredFields);
                    InvalidateCache();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ViewObjectEditor] 绑定现有变量错误: {e}");
                }
            }

            GUI.backgroundColor = originalBackColor;
            EditorGUILayout.EndHorizontal();

            if (bindingCount == 0)
            {
                EditorGUILayout.Space(3);
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("点击「自动绑定」将自动扫描并绑定场景中的组件引用", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.Space(3);
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox($"已绑定 {bindingCount} 个变量。可以继续绑定或清理现有绑定", MessageType.None);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }

    #endregion

    #region 自动替换脚本

    public partial class ViewObjectEditor
    {
        private bool _waitingForComponentSelection;

        public void Replace()
        {
            if (_waitingForComponentSelection && Event.current.commandName == "ObjectSelectorClosed")
            {
                _waitingForComponentSelection = false;
                OnComponentSelected();
            }

            var targetObj = (ViewObject)serializedObject.targetObject;
            var gameObject = targetObj.gameObject;
            var prefabName = GetPrefabName(gameObject);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("脚本替换工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (!string.IsNullOrEmpty(prefabName))
            {
                EditorGUILayout.LabelField("预制名称:", GUILayout.Width(70));

                var originalStyle = GUI.skin.label;
                GUI.skin.label = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(0.2f, 0.6f, 1f) },
                    fontStyle = FontStyle.Bold
                };

                EditorGUILayout.LabelField(prefabName, GUILayout.Width(150));
                GUI.skin.label = originalStyle;
            }
            else
            {
                EditorGUILayout.LabelField("非预制对象", GUILayout.Width(150));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            var originalBackColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.9f);

            if (GUILayout.Button("🔍 手动选择脚本", GUILayout.Height(30)))
            {
                ShowUnityComponentPicker();
            }

            GUI.backgroundColor = originalBackColor;

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(prefabName))
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

                if (GUILayout.Button("⚡ 快速替换同名脚本", GUILayout.Height(30)))
                {
                    FindAndSelectScript(prefabName);
                }

                GUI.backgroundColor = originalBackColor;
            }
            else
            {
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
                GUI.enabled = false;
                GUILayout.Button("⚡ 快速替换不可用", GUILayout.Height(30));
                GUI.enabled = true;
                GUI.backgroundColor = originalBackColor;
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(prefabName))
            {
                EditorGUILayout.Space(3);
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox($"点击「快速替换」将自动查找并替换为 {prefabName}.cs 脚本", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowUnityComponentPicker()
        {
            _waitingForComponentSelection = true;
            EditorGUIUtility.ShowObjectPicker<MonoScript>(
                null,
                false,
                "t:MonoScript",
                GUIUtility.GetControlID(FocusType.Passive));
        }

        private void OnComponentSelected()
        {
            var selectedScript = EditorGUIUtility.GetObjectPickerObject() as MonoScript;
            if (selectedScript == null) return;

            var newComponentType = selectedScript.GetClass();
            if (newComponentType == null || !typeof(ViewObject).IsAssignableFrom(newComponentType))
            {
                EditorUtility.DisplayDialog("错误", "选择的脚本必须继承自 BaseView", "确定");
                return;
            }

            ReplaceComponent(newComponentType);
        }

        private void ReplaceComponent(Type newComponentType)
        {
            var targetObj = (ViewObject)serializedObject.targetObject;
            var gameObject = targetObj.gameObject;

            var components = gameObject.GetComponents<Component>();
            var oldIndex = Array.FindIndex(components, c => c == targetObj);

            var newComp = Undo.AddComponent(gameObject, newComponentType);

            for (var i = gameObject.GetComponents<Component>().Length - 1; i > oldIndex; i--)
            {
                ComponentUtility.MoveComponentUp(newComp);
            }

            Undo.DestroyObjectImmediate(targetObj);

            EditorUtility.SetDirty(gameObject);
        }

        private string GetPrefabName(GameObject gameObject)
        {
            var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                }
            }

            return gameObject.name;
        }

        private void FindAndSelectScript(string scriptName)
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");

            MonoScript targetScript = null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null) continue;

                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                if (fileName == scriptName)
                {
                    var scriptClass = script.GetClass();
                    if (scriptClass != null && typeof(ViewObject).IsAssignableFrom(scriptClass))
                    {
                        targetScript = script;
                        break;
                    }
                }
            }

            if (targetScript != null)
            {
                var newComponentType = targetScript.GetClass();
                if (newComponentType != null && typeof(ViewObject).IsAssignableFrom(newComponentType))
                {
                    ReplaceComponent(newComponentType);
                    EditorUtility.DisplayDialog("成功", $"已将脚本替换为: {targetScript.name}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", $"脚本 {targetScript.name} 必须继承自 BaseView", "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("未找到", $"未找到名为 '{scriptName}' 的脚本\n或该脚本未继承自 BaseView", "确定");
            }
        }
    }

    #endregion
}
