using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework.View.Editor
{
    public class VarPrefabBind : VarBaseBind
    {
        public override void Update(ViewObject bindRoot)
        {
            base.Update(bindRoot);

            var tr = bindRoot.transform;
            BindInfoCollect(tr, tr);
            Bind2Serialize(bindRoot);

            SaveAssets(bindRoot);
        }

        private static void SaveAssets(ViewObject bindRoot)
        {
            if (SaveInsidePrefab(bindRoot)) return;

            var prefabRoot = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabStageUtility.GetCurrentPrefabStage().assetPath);
        }

        public static bool SaveInsidePrefab(ViewObject bindRoot)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(bindRoot)) return false;

            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(bindRoot.gameObject);
            if (prefabInstanceRoot == null) return false;

            var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstanceRoot);
            if (rootPrefab == null) return false;

            var rootPath = AssetDatabase.GetAssetPath(rootPrefab);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                prefabInstanceRoot,
                rootPath,
                InteractionMode.AutomatedAction
            );

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
            }

            return true;
        }

        public static void SavePrefabChanges(ViewObject behaviour)
        {
            try
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(behaviour))
                    {
                        SaveNestedPrefab(behaviour);
                        PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
                        EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
                        return;
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
                    EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
                    return;
                }

                if (!PrefabUtility.IsPartOfAnyPrefab(behaviour)) return;

                SaveNestedPrefab(behaviour);

                EditorUtility.SetDirty(behaviour);
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                EditorSceneManager.MarkSceneDirty(behaviour.gameObject.scene);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"保存预制体更改时出错: {ex.Message}");
            }
        }

        private static void SaveNestedPrefab(ViewObject behaviour)
        {
            var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(behaviour.gameObject);
            if (prefabInstanceRoot == null) return;

            var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstanceRoot);
            if (rootPrefab != null)
            {
                var rootPath = AssetDatabase.GetAssetPath(rootPrefab);
                PrefabUtility.SaveAsPrefabAssetAndConnect(
                    prefabInstanceRoot,
                    rootPath,
                    InteractionMode.AutomatedAction
                );
            }
        }

        public override void ClearBinding(ViewObject behaviour)
        {
            base.ClearBinding(behaviour);
            SavePrefabChanges(behaviour);
        }

        public override void BindExisting(ViewObject bindRoot, HashSet<string> declaredFields)
        {
            base.BindExisting(bindRoot, declaredFields);
            SavePrefabChanges(bindRoot);
        }

        protected override void CollectAllBindings(ViewObject bindRoot)
        {
            var tr = bindRoot.transform;

            // 预制体绑定：扫描整个预制体
            BindInfoCollect(tr, tr);
        }
    }

    public class VarPrefabInSceneBind : VarBaseBind
    {
        public override void Update(ViewObject bindRoot)
        {
            base.Update(bindRoot);

            BindInfoCollect(bindRoot.transform, bindRoot.transform);
            Bind2Serialize(bindRoot);

            VarPrefabBind.SaveInsidePrefab(bindRoot);
            EditorUtility.SetDirty(bindRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bindRoot);
            EditorSceneManager.MarkSceneDirty(bindRoot.gameObject.scene);
        }

        public override void ClearBinding(ViewObject behaviour)
        {
            base.ClearBinding(behaviour);
            VarPrefabBind.SavePrefabChanges(behaviour);
        }

        public override void BindExisting(ViewObject bindRoot, HashSet<string> declaredFields)
        {
            base.BindExisting(bindRoot, declaredFields);
            VarPrefabBind.SavePrefabChanges(bindRoot);
        }

        protected override void CollectAllBindings(ViewObject bindRoot)
        {
            // 场景局部绑定：只绑定当前子树
            BindInfoCollect(bindRoot.transform, bindRoot.transform);
        }
    }
}
