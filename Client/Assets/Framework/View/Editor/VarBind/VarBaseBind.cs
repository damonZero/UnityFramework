using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.View;
using UnityEditor;
using UnityEngine;

namespace Framework.View.Editor
{
    public abstract class VarBaseBind
    {
        internal static Dictionary<string, Type> PrefixTypeDict { get; }

        internal const string CONFIG_NAME = "CSharpAutoBindingConfig";

        static VarBaseBind()
        {
            var guids = AssetDatabase.FindAssets($"t:{CONFIG_NAME}");
            if (guids.Length <= 0)
            {
                // KJ 适配：未创建 config asset 时回退到应用层注册表，使工具开箱即用。
                PrefixTypeDict = CSharpAutoBindingEditor.Register?.PrefixTypeDict;
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var config = AssetDatabase.LoadAssetAtPath<CSharpAutoBindingConfig>(path);
            PrefixTypeDict = config.GetPrefixTypeDict()
                .OrderByDescending(kv => kv.Key.Length)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        #region 收集绑定信息

        protected readonly List<VarBindData> _bindInfo = new();

        public virtual void Update(ViewObject bindRoot)
        {
            _bindInfo.Clear();
        }

        /// <summary>
        /// 绑定现有变量（仅绑定已在 .Binding 文件中声明的字段）
        /// </summary>
        public virtual void BindExisting(ViewObject bindRoot, HashSet<string> declaredFields)
        {
            _bindInfo.Clear();

            if (declaredFields == null || declaredFields.Count == 0)
            {
                return;
            }

            CollectAllBindings(bindRoot);

            _bindInfo.RemoveAll(info => !declaredFields.Contains(info.OriginalName));

            bindRoot.UpdateBinding(_bindInfo);
        }

        protected abstract void CollectAllBindings(ViewObject bindRoot);

        protected void BindInfoCollect(Transform tr, Transform bindRoot)
        {
            VarBindTool.TraverseTrans(tr, TryCollectBindInfo, bindRoot);
            _bindInfo.ForEach(b => b.FinalHandle());
        }

        private void TryCollectBindInfo(Transform tr)
        {
            var needBind = VarBindTool.CollectVarBind(tr, PrefixTypeDict,
                out var varName, out var varType, out var varObj);

            if (!needBind) return;

            var existing = _bindInfo.FirstOrDefault(b => b.OriginalName == varName);
            if (existing != null)
                existing.AddObject(varObj);
            else
                _bindInfo.Add(new VarBindData(varName, varType, varObj));
        }

        #endregion

        #region 处理绑定变量关系

        protected void Bind2Serialize(ViewObject go)
        {
            go.UpdateBinding(_bindInfo);
            InsertFieldsToScript(go, _bindInfo);
        }

        #endregion

        #region 生成绑定代码

        /// <summary>
        /// 插入绑定代码字段
        /// </summary>
        private static void InsertFieldsToScript(ViewObject targetObj, List<VarBindData> insert)
        {
            var monoScript = MonoScript.FromMonoBehaviour(targetObj);
            var assetPath = AssetDatabase.GetAssetPath(monoScript);
            var className = targetObj.GetType().Name;

            VarBindTool.EnsureClassIsPartial(assetPath, className);

            var parentFieldNames = VarBindTool.CollectParentBindingFieldNames(targetObj.GetType());
            var (bindingFields, usingNamespaces) = VarBindTool.ParseBindingFields(insert, parentFieldNames);

            var bindingFilePath = Path.Combine(Path.GetDirectoryName(assetPath)!,
                $"{Path.GetFileNameWithoutExtension(assetPath)}.Binding.cs");

            VarBindTool.InsertFileContent(bindingFilePath, className, targetObj.GetType().Namespace,
                bindingFields, usingNamespaces);
        }

        #endregion

        #region 清理

        public virtual void ClearBinding(ViewObject behaviour)
        {
            VarBindTool.CleanSerializedBindings(behaviour);
        }

        #endregion
    }
}
