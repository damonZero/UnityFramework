using System;
using System.Collections.Generic;
using Framework.Pool;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public class NavigationEditorFactory : NavigationFactory
    {
        private static readonly Dictionary<Type, Type> _editorTypeMap = new ()
        {
            { typeof(NavigateContainer), typeof(EditorNavigateContainer) },
            { typeof(NavigationFormLoader), typeof(EditorNavigationFormLoader) },
            { typeof(NavigationSceneLoader), typeof(EditorNavigationSceneLoader) },
        };

        public override T Get<T>() where T : class
        {
            if (_editorTypeMap.TryGetValue(typeof(T), out var editorType))
            {
                // 原实现依赖类型键控对象池（Pool.Get(editorType) as T）；KJ 的 Framework.Pool 无此 API。
                // 改用 Framework.Pool.TypePool，按 T 缓存一个工厂来实例化编辑器子类型。
                var pool = TypePool.GetOrCreate<T>(
                    factory: () => (T)Activator.CreateInstance(editorType),
                    reset: null,
                    maxIdle: 64);
                return pool.Rent();
            }

            Debug.LogError($"{nameof(NavigationEditorFactory)} 未注册类型 {typeof(T).FullName} 的编辑器实现");
            return base.Get<T>();
        }

    }
}
