using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework.View.Editor
{
    public class VarSceneBind : VarBaseBind
    {
        public override void Update(ViewObject bindRoot)
        {
            base.Update(bindRoot);

            var rootGo = bindRoot.gameObject;
            var rootTr = bindRoot.transform;
            foreach (var go in rootGo.scene.GetRootGameObjects())
            {
                BindInfoCollect(go.transform, rootTr);
            }

            Bind2Serialize(bindRoot);

            EditorUtility.SetDirty(bindRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bindRoot);
            EditorSceneManager.MarkSceneDirty(bindRoot.gameObject.scene);
        }

        public override void BindExisting(ViewObject bindRoot, HashSet<string> declaredFields)
        {
            base.BindExisting(bindRoot, declaredFields);

            EditorUtility.SetDirty(bindRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bindRoot);
            EditorSceneManager.MarkSceneDirty(bindRoot.gameObject.scene);
        }

        protected override void CollectAllBindings(ViewObject bindRoot)
        {
            var rootGo = bindRoot.gameObject;
            var rootTr = bindRoot.transform;

            // 场景全局绑定：扫描整个场景
            foreach (var go in rootGo.scene.GetRootGameObjects())
            {
                BindInfoCollect(go.transform, rootTr);
            }
        }
    }
}
