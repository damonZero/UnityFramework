using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework.View.Editor
{
    public enum VarBindType
    {
        Scene, // 全局场景绑定
        PrefabInScene, // 局部场景绑定
        Prefab // 预制体绑定
    }

    public class VarBind
    {
        private VarBindType _currentType;

        private VarBaseBind _handler;

        /// <summary>
        /// 自动变量绑定
        /// </summary>
        public void AutoBinding(ViewObject behaviour)
        {
            GetHandler(behaviour).Update(behaviour);
        }

        /// <summary>
        /// 绑定现有变量（仅绑定已在 .Binding 文件中声明的字段）
        /// </summary>
        public void BindExistingVariables(ViewObject behaviour, HashSet<string> declaredFields)
        {
            GetHandler(behaviour).BindExisting(behaviour, declaredFields);
        }

        public void ClearBinding(ViewObject behaviour)
        {
            GetHandler(behaviour).ClearBinding(behaviour);
        }

        private VarBaseBind GetHandler(ViewObject behaviour)
        {
            var type = GetBindType(behaviour);
            if (_handler != null && _currentType == type) return _handler;

            _currentType = type;
            if (type == VarBindType.Prefab)
                _handler = new VarPrefabBind();
            else if (type == VarBindType.Scene)
                _handler = new VarSceneBind();
            else
                _handler = new VarPrefabInSceneBind();
            return _handler;
        }

        private VarBindType GetBindType(ViewObject behaviour)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage)
                return VarBindType.Prefab;

            if (behaviour.TryGetComponent<BaseScene>(out _))
                return VarBindType.Scene;
            return VarBindType.PrefabInScene;
        }
    }
}
