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
            if (_handler != null) return _handler;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage)
            {
                _handler = new VarPrefabBind();
                _currentType = VarBindType.Prefab;
                return _handler;
            }

            if (behaviour.TryGetComponent<BaseScene>(out _))
            {
                _handler = new VarSceneBind();
                _currentType = VarBindType.Scene;
            }
            else
            {
                _handler = new VarPrefabInSceneBind();
                _currentType = VarBindType.PrefabInScene;
            }

            return _handler;
        }
    }
}
