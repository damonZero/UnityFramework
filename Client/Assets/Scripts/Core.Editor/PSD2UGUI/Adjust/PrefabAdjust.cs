using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Added for .Where() and .Take()

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 预制体校准
    /// </summary>
    public class PrefabAdjust : EditorWindow
    {
        [MenuItem("开发中/预制体校准")]
        public static void test()
        {
            PrefabAdjust window = GetWindow<PrefabAdjust>();
            window.titleContent = new GUIContent("预制体校准");
            window.Show();
        }

        private GameObject srcPrefab;
        private GameObject destPrefab;
        
        private Texture2D srcTexture;
        private Texture2D destTexture;
        
        // 新增：匹配策略选择
        private SmartNodeMatcher.MatchStrategy matchStrategy = SmartNodeMatcher.MatchStrategy.Smart;
        private bool useIdentityComponents = true;
        
        // 新增：高级匹配选项
        private AdvancedNodeMatcher.MatchAlgorithm advancedAlgorithm = AdvancedNodeMatcher.MatchAlgorithm.Hybrid;
        private bool useAdvancedMatching = false;
        
        // 新增：还原功能
        private bool enableRestore = true;
        private Dictionary<GameObject, PrefabBackup> prefabBackups = new Dictionary<GameObject, PrefabBackup>();

        public void OnGUI()
        {
            GUILayout.Space(20);
            srcPrefab = EditorGUILayout.ObjectField("美术预制体", srcPrefab, typeof(GameObject), false) as GameObject;
            destPrefab = EditorGUILayout.ObjectField("程序预制体", destPrefab, typeof(GameObject), false) as GameObject;
            
            // 新增：匹配策略选择
            GUILayout.Space(10);
            GUILayout.Label("匹配策略设置", EditorStyles.boldLabel);
            
            useAdvancedMatching = EditorGUILayout.Toggle("使用高级匹配", useAdvancedMatching);
            
            if (useAdvancedMatching)
            {
                advancedAlgorithm = (AdvancedNodeMatcher.MatchAlgorithm)EditorGUILayout.EnumPopup("高级匹配算法", advancedAlgorithm);
            }
            else
            {
                matchStrategy = (SmartNodeMatcher.MatchStrategy)EditorGUILayout.EnumPopup("智能匹配策略", matchStrategy);
                useIdentityComponents = EditorGUILayout.Toggle("使用身份组件", useIdentityComponents);
            }
            
            // 新增：还原设置
            GUILayout.Space(10);
            GUILayout.Label("还原设置", EditorStyles.boldLabel);
            enableRestore = EditorGUILayout.Toggle("启用还原功能", enableRestore);
            
            // 显示备份状态
            if (destPrefab != null && prefabBackups.ContainsKey(destPrefab))
            {
                var backup = prefabBackups[destPrefab];
                GUILayout.Label($"备份状态: 已备份 ({backup.backupTime:HH:mm:ss})", EditorStyles.helpBox);
            }
            else
            {
                GUILayout.Label("备份状态: 未备份", EditorStyles.helpBox);
            }
            
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("校准"))
            {
                if (srcPrefab != null && destPrefab != null)
                {
                    adjust(srcPrefab, destPrefab);
                    comparePrefab(srcPrefab, destPrefab);
                    GUIUtility.ExitGUI();
                }
            }
            
            if (GUILayout.Button("对比"))
            {
                if (srcPrefab != null && destPrefab != null)
                {
                    comparePrefab(srcPrefab, destPrefab);
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.EndHorizontal();
            
            // 新增：还原按钮
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("还原程序预制体"))
            {
                if (destPrefab != null)
                {
                    RestorePrefab(destPrefab);
                }
            }
            
            if (GUILayout.Button("备份程序预制体"))
            {
                if (destPrefab != null)
                {
                    BackupPrefab(destPrefab);
                }
            }
            GUILayout.EndHorizontal();
            
            // 新增：身份组件管理按钮
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("添加身份组件"))
            {
                if (srcPrefab != null)
                {
                    SmartNodeMatcher.AddIdentityComponents(srcPrefab);
                    EditorUtility.SetDirty(srcPrefab);
                }
                if (destPrefab != null)
                {
                    SmartNodeMatcher.AddIdentityComponents(destPrefab);
                    EditorUtility.SetDirty(destPrefab);
                }
            }
            
            if (GUILayout.Button("更新身份组件"))
            {
                if (srcPrefab != null)
                {
                    SmartNodeMatcher.UpdateIdentityComponents(srcPrefab);
                    EditorUtility.SetDirty(srcPrefab);
                }
                if (destPrefab != null)
                {
                    SmartNodeMatcher.UpdateIdentityComponents(destPrefab);
                    EditorUtility.SetDirty(destPrefab);
                }
            }
            GUILayout.EndHorizontal();
            
            // 新增：高级匹配测试按钮
            GUILayout.Space(10);
            if (GUILayout.Button("测试高级匹配"))
            {
                if (srcPrefab != null && destPrefab != null)
                {
                    TestAdvancedMatching();
                }
            }


            var width = Screen.width / 2;
            // 宽高比
            var height = width * 1624 / 750f;
            // 显示对比的图片
            if (srcTexture != null)
            {
                GUI.Box(new Rect(0, 300, width, height), srcTexture);
            }
            if (destTexture != null)
            {
                GUI.Box(new Rect(width, 300, width, height), destTexture);
            }

        }
        
        /// <summary>
        /// 比较预制体
        /// </summary>
        /// <param name="srcPrefab"></param>
        /// <param name="destPrefab"></param>
        public void comparePrefab(GameObject srcPrefab, GameObject destPrefab)
        {
            var srcRenderTexture = PrefabPreview.GetPrefabPreview(srcPrefab, 750, 1624, true);
            srcTexture = RenderTextureToTexture2D(srcRenderTexture as RenderTexture);
            var destRenderTexture = PrefabPreview.GetPrefabPreview(destPrefab, 750, 1624, true);
            destTexture = RenderTextureToTexture2D(destRenderTexture as RenderTexture);
            
            
            // 对比2张图片，把destTexture的红色部分标记出来
            var width = srcTexture.width;
            var height = srcTexture.height;
            Color colorA, colorB;
            Color coverColor = new Color(1, 0, 0, 0.3f);
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    colorA = srcTexture.GetPixel(i, j);
                    colorB = destTexture.GetPixel(i, j);
                    if (!ColorEqual(colorA, colorB))
                    {
                        destTexture.SetPixel(i, j, new Color(
                            colorB.r * (1 - coverColor.a) + coverColor.r * coverColor.a,
                            colorB.g * (1 - coverColor.a) + coverColor.g * coverColor.a,
                            colorB.b * (1 - coverColor.a) + coverColor.b * coverColor.a,
                            colorB.a
                            ));
                    }
                }
            }
            destTexture.Apply();
            
        }
        
        public bool ColorEqual(Vector4 lhs, Vector4 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            return (double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3
                   < 9.999999439624929E-4;

        }
        
        /// <summary>
        /// rt转texture2d
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public Texture2D RenderTextureToTexture2D(RenderTexture rt)
        {
            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(rt.width, rt.height);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            return texture;
        }
        

        /// <summary>
        /// 校准
        /// </summary>
        /// <param name="srcPrefabPath"></param>
        /// <param name="destPrefabPath"></param>
        public void adjust(GameObject srcPrefab, GameObject destPrefab)
        {
            // 参数验证
            if (srcPrefab == null || destPrefab == null)
            {
                Debug.LogError("预制体校准失败：源预制体或目标预制体为空");
                return;
            }

            try
            {
                // 如果启用还原功能，先备份程序预制体
                if (enableRestore)
                {
                    BackupPrefab(destPrefab);
                }
                
                Dictionary<GameObject, GameObject> nodeMatches;
                
                if (useAdvancedMatching)
                {
                    Debug.Log($"开始校准预制体：使用高级匹配算法 {advancedAlgorithm}");
                    nodeMatches = AdvancedNodeMatcher.AdvancedMatch(srcPrefab, destPrefab, advancedAlgorithm);
                }
                else
                {
                    Debug.Log($"开始校准预制体：使用智能匹配策略 {matchStrategy}");
                    nodeMatches = SmartNodeMatcher.SmartMatch(srcPrefab, destPrefab, matchStrategy);
                }
                
                Debug.Log($"匹配完成：找到 {nodeMatches.Count} 个匹配节点");

                // 第一步：先处理所有布局组件，确保布局系统稳定
                ProcessLayoutComponents(destPrefab);

                // 第二步：校准对应节点的位置和大小
                int adjustedCount = 0;
                foreach (var match in nodeMatches)
                {
                    GameObject srcObj = match.Key;
                    GameObject destObj = match.Value;
                    
                    adjectGameObject(srcObj, destObj);
                    adjustedCount++;
                }
                
                Debug.Log($"校准完成：成功校准 {adjustedCount} 个节点");

                // 第三步：再次处理布局组件，确保校准后的布局正确
                ProcessLayoutComponents(destPrefab);

                // 第四步：强制重建所有布局
                ForceRebuildAllLayouts(destPrefab);
                
                // 第五步：等待一帧后再次校准，处理可能的延迟布局更新
                EditorApplication.delayCall += () => {
                    FinalAdjustment(srcPrefab, destPrefab, nodeMatches);
                };

                EditorUtility.SetDirty(destPrefab);
                AssetDatabase.SaveAssets();
                
                Debug.Log("预制体校准完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"预制体校准过程中发生错误：{e.Message}\n{e.StackTrace}");
                
                // 如果校准失败且启用了还原功能，自动还原
                if (enableRestore)
                {
                    Debug.Log("校准失败，正在自动还原程序预制体...");
                    RestorePrefab(destPrefab);
                }
            }
        }
        
        /// <summary>
        /// 最终校准，处理延迟布局更新
        /// </summary>
        /// <param name="srcPrefab"></param>
        /// <param name="destPrefab"></param>
        /// <param name="nodeMatches"></param>
        private void FinalAdjustment(GameObject srcPrefab, GameObject destPrefab, Dictionary<GameObject, GameObject> nodeMatches)
        {
            // 最终校准位置和大小
            foreach (var match in nodeMatches)
            {
                GameObject srcObj = match.Key;
                GameObject destObj = match.Value;
                adjectGameObject(srcObj, destObj);
            }
            
            // 最终布局重建
            ForceRebuildAllLayouts(destPrefab);
            
            EditorUtility.SetDirty(destPrefab);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 处理布局组件
        /// </summary>
        /// <param name="rootObject"></param>
        private void ProcessLayoutComponents(GameObject rootObject)
        {
            ProcessLayoutComponentsRecursive(rootObject);
            
            // 处理Canvas Scaler的影响
            ProcessCanvasScaler(rootObject);
        }
        
        /// <summary>
        /// 递归处理布局组件
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessLayoutComponentsRecursive(GameObject obj)
        {
            if (obj == null) return;
            
            // 处理当前节点的布局组件
            ProcessSingleNodeLayout(obj);
            
            // 递归处理子节点
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                ProcessLayoutComponentsRecursive(obj.transform.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// 处理单个节点的布局组件
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessSingleNodeLayout(GameObject obj)
        {
            // 处理水平/垂直布局组 (原 P33 依赖 customSpace/CollectChildSpaces 扩展, 已剥离)
            var horizontalVerticalGroup = obj.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (horizontalVerticalGroup != null)
            {
                PrefabAdjustUtil.FitChildSize(obj.GetComponent<RectTransform>());
            }
            
            // 处理网格布局组
            var gridGroup = obj.GetComponent<GridLayoutGroup>();
            if (gridGroup != null)
            {
                // 网格布局需要重新计算
                LayoutRebuilder.ForceRebuildLayoutImmediate(obj.GetComponent<RectTransform>());
            }
            
            // 处理内容大小适配器
            var contentSizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                // 强制重新计算内容大小
                contentSizeFitter.SetLayoutHorizontal();
                contentSizeFitter.SetLayoutVertical();
            }
            
            // 处理布局元素
            var layoutElement = obj.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                // 标记布局需要重建
                LayoutRebuilder.MarkLayoutForRebuild(obj.GetComponent<RectTransform>());
            }
        }

        /// <summary>
        /// 处理Canvas Scaler的影响
        /// </summary>
        /// <param name="rootObject"></param>
        private void ProcessCanvasScaler(GameObject rootObject)
        {
            // 查找Canvas组件
            Canvas canvas = rootObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                // 递归查找Canvas组件
                canvas = FindCanvasRecursive(rootObject);
            }
            
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    // 强制Canvas Scaler重新计算
                    // scaler.Handle();
                }
            }
        }
        
        /// <summary>
        /// 递归查找Canvas组件
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Canvas FindCanvasRecursive(GameObject obj)
        {
            if (obj == null) return null;
            
            Canvas canvas = obj.GetComponent<Canvas>();
            if (canvas != null) return canvas;
            
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                canvas = FindCanvasRecursive(obj.transform.GetChild(i).gameObject);
                if (canvas != null) return canvas;
            }
            
            return null;
        }

        /// <summary>
        /// 强制重建所有布局
        /// </summary>
        /// <param name="rootObject"></param>
        private void ForceRebuildAllLayouts(GameObject rootObject)
        {
            // 递归重建所有子节点的布局
            RebuildLayoutRecursive(rootObject.transform);
        }

        /// <summary>
        /// 递归重建布局
        /// </summary>
        /// <param name="transform"></param>
        private void RebuildLayoutRecursive(Transform transform)
        {
            // 先处理子节点
            for (int i = 0; i < transform.childCount; i++)
            {
                RebuildLayoutRecursive(transform.GetChild(i));
            }
            
            // 再处理当前节点
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                // 强制重建布局
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }


        /// <summary>
        /// 校验对象
        /// </summary>
        /// <param name="srcObj"></param>
        /// <param name="destObj"></param>
        private void adjectGameObject(GameObject srcObj, GameObject destObj)
        {
            RectTransform srcRect = srcObj.GetComponent<RectTransform>();
            RectTransform destRect = destObj.GetComponent<RectTransform>();
            if (srcRect != null && destRect != null)
            {
                // 同步锚点设置
                destRect.anchorMin = srcRect.anchorMin;
                destRect.anchorMax = srcRect.anchorMax;
                
                // 同步轴心点
                destRect.pivot = srcRect.pivot;
                
                // 同步位置（世界坐标）
                destRect.position = srcRect.position;
                
                // 同步大小
                destRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, srcRect.rect.width);
                destRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, srcRect.rect.height);
                
                // 同步缩放
                destRect.localScale = srcRect.localScale;
                
                // 同步旋转
                destRect.localRotation = srcRect.localRotation;
                
                // 同步锚点位置（anchoredPosition）
                destRect.anchoredPosition = srcRect.anchoredPosition;
                
                // 同步尺寸增量（sizeDelta）
                destRect.sizeDelta = srcRect.sizeDelta;
            }
            
            // 同步其他重要组件
            SyncLayoutComponents(srcObj, destObj);
        }

        /// <summary>
        /// 同步布局组件属性
        /// </summary>
        /// <param name="srcObj"></param>
        /// <param name="destObj"></param>
        private void SyncLayoutComponents(GameObject srcObj, GameObject destObj)
        {
            // 同步HorizontalOrVerticalLayoutGroup
            var srcLayout = srcObj.GetComponent<HorizontalOrVerticalLayoutGroup>();
            var destLayout = destObj.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (srcLayout != null && destLayout != null)
            {
                destLayout.spacing = srcLayout.spacing;
                destLayout.padding = srcLayout.padding;
                destLayout.childAlignment = srcLayout.childAlignment;
                destLayout.childControlWidth = srcLayout.childControlWidth;
                destLayout.childControlHeight = srcLayout.childControlHeight;
                destLayout.childForceExpandWidth = srcLayout.childForceExpandWidth;
                destLayout.childForceExpandHeight = srcLayout.childForceExpandHeight;
            }
            
            // 同步GridLayoutGroup
            var srcGrid = srcObj.GetComponent<GridLayoutGroup>();
            var destGrid = destObj.GetComponent<GridLayoutGroup>();
            if (srcGrid != null && destGrid != null)
            {
                destGrid.cellSize = srcGrid.cellSize;
                destGrid.spacing = srcGrid.spacing;
                destGrid.padding = srcGrid.padding;
                destGrid.startCorner = srcGrid.startCorner;
                destGrid.startAxis = srcGrid.startAxis;
                destGrid.childAlignment = srcGrid.childAlignment;
                destGrid.constraint = srcGrid.constraint;
                destGrid.constraintCount = srcGrid.constraintCount;
            }
            
            // 同步ContentSizeFitter
            var srcFitter = srcObj.GetComponent<ContentSizeFitter>();
            var destFitter = destObj.GetComponent<ContentSizeFitter>();
            if (srcFitter != null && destFitter != null)
            {
                destFitter.horizontalFit = srcFitter.horizontalFit;
                destFitter.verticalFit = srcFitter.verticalFit;
            }
            
            // 同步AspectRatioFitter
            var srcAspect = srcObj.GetComponent<AspectRatioFitter>();
            var destAspect = destObj.GetComponent<AspectRatioFitter>();
            if (srcAspect != null && destAspect != null)
            {
                destAspect.aspectMode = srcAspect.aspectMode;
                destAspect.aspectRatio = srcAspect.aspectRatio;
            }
            
            // 同步LayoutElement
            var srcLayoutElement = srcObj.GetComponent<LayoutElement>();
            var destLayoutElement = destObj.GetComponent<LayoutElement>();
            if (srcLayoutElement != null && destLayoutElement != null)
            {
                destLayoutElement.minWidth = srcLayoutElement.minWidth;
                destLayoutElement.minHeight = srcLayoutElement.minHeight;
                destLayoutElement.preferredWidth = srcLayoutElement.preferredWidth;
                destLayoutElement.preferredHeight = srcLayoutElement.preferredHeight;
                destLayoutElement.flexibleWidth = srcLayoutElement.flexibleWidth;
                destLayoutElement.flexibleHeight = srcLayoutElement.flexibleHeight;
                destLayoutElement.ignoreLayout = srcLayoutElement.ignoreLayout;
            }
            
            // 同步CanvasGroup（影响子节点布局）
            var srcCanvasGroup = srcObj.GetComponent<CanvasGroup>();
            var destCanvasGroup = destObj.GetComponent<CanvasGroup>();
            if (srcCanvasGroup != null && destCanvasGroup != null)
            {
                destCanvasGroup.alpha = srcCanvasGroup.alpha;
                destCanvasGroup.interactable = srcCanvasGroup.interactable;
                destCanvasGroup.blocksRaycasts = srcCanvasGroup.blocksRaycasts;
                destCanvasGroup.ignoreParentGroups = srcCanvasGroup.ignoreParentGroups;
            }
        }
        
        /// <summary>
        /// 测试高级匹配
        /// </summary>
        private void TestAdvancedMatching()
        {
            try
            {
                Debug.Log("开始测试高级匹配...");
                
                var matches = AdvancedNodeMatcher.AdvancedMatch(srcPrefab, destPrefab, advancedAlgorithm);
                
                Debug.Log($"高级匹配测试结果：找到 {matches.Count} 个匹配");
                
                foreach (var match in matches)
                {
                    Debug.Log($"匹配: {match.Key.name} -> {match.Value.name}");
                }
                
                // 分析匹配质量
                AnalyzeMatchQuality(matches);
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"高级匹配测试失败：{e.Message} {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 分析匹配质量
        /// </summary>
        private void AnalyzeMatchQuality(Dictionary<GameObject, GameObject> matches)
        {
            if (matches.Count == 0) return;
            
            var srcNodes = GetAllNodes(srcPrefab);
            var destNodes = GetAllNodes(destPrefab);
            
            int totalSrcNodes = srcNodes.Count;
            int totalDestNodes = destNodes.Count;
            int matchedNodes = matches.Count;
            
            float srcCoverage = (float)matchedNodes / totalSrcNodes * 100f;
            float destCoverage = (float)matchedNodes / totalDestNodes * 100f;
            
            Debug.Log($"匹配质量分析：");
            Debug.Log($"源预制体节点数: {totalSrcNodes}");
            Debug.Log($"目标预制体节点数: {totalDestNodes}");
            Debug.Log($"匹配节点数: {matchedNodes}");
            Debug.Log($"源预制体覆盖率: {srcCoverage:F1}%");
            Debug.Log($"目标预制体覆盖率: {destCoverage:F1}%");
            
            // 分析未匹配的节点
            var unmatchedSrc = srcNodes.Where(n => !matches.ContainsKey(n)).ToList();
            var unmatchedDest = destNodes.Where(n => !matches.ContainsValue(n)).ToList();
            
            if (unmatchedSrc.Count > 0)
            {
                Debug.LogWarning($"未匹配的源节点 ({unmatchedSrc.Count}):");
                foreach (var node in unmatchedSrc.Take(5)) // 只显示前5个
                {
                    Debug.LogWarning($"  - {node.name}");
                }
            }
            
            if (unmatchedDest.Count > 0)
            {
                Debug.LogWarning($"未匹配的目标节点 ({unmatchedDest.Count}):");
                foreach (var node in unmatchedDest.Take(5)) // 只显示前5个
                {
                    Debug.LogWarning($"  - {node.name}");
                }
            }
        }
        
        /// <summary>
        /// 获取所有节点
        /// </summary>
        private List<GameObject> GetAllNodes(GameObject root)
        {
            var nodes = new List<GameObject>();
            GetAllNodesRecursive(root, nodes);
            return nodes;
        }
        
        /// <summary>
        /// 递归获取所有节点
        /// </summary>
        private void GetAllNodesRecursive(GameObject obj, List<GameObject> nodes)
        {
            nodes.Add(obj);
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GetAllNodesRecursive(obj.transform.GetChild(i).gameObject, nodes);
            }
        }

        /// <summary>
        /// 备份预制体
        /// </summary>
        private void BackupPrefab(GameObject prefab)
        {
            if (prefab == null) return;
            
            try
            {
                var backup = new PrefabBackup(prefab);
                prefabBackups[prefab] = backup;
                
                Debug.Log($"预制体备份完成: {prefab.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"备份预制体失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 还原预制体
        /// </summary>
        private void RestorePrefab(GameObject prefab)
        {
            if (prefab == null) return;
            
            try
            {
                if (prefabBackups.TryGetValue(prefab, out PrefabBackup backup))
                {
                    backup.Restore(prefab);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"预制体还原完成: {prefab.name}");
                }
                else
                {
                    Debug.LogWarning($"没有找到预制体的备份: {prefab.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"还原预制体失败: {e.Message}");
            }
        }
    }
}