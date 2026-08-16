using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 智能节点匹配管理器
    /// 基于NodeIdentityComponent进行智能匹配
    /// </summary>
    public class SmartNodeMatcher
    {
        /// <summary>
        /// 匹配结果
        /// </summary>
        public class MatchResult
        {
            public GameObject SourceNode;
            public GameObject DestNode;
            public float MatchScore;
            public string MatchReason;
            
            public MatchResult(GameObject source, GameObject dest, float score, string reason)
            {
                SourceNode = source;
                DestNode = dest;
                MatchScore = score;
                MatchReason = reason;
            }
        }
        
        /// <summary>
        /// 匹配策略
        /// </summary>
        public enum MatchStrategy
        {
            FileIDOnly,      // 仅FileID匹配
            IdentityOnly,    // 仅身份组件匹配
            Hybrid,          // 混合匹配
            Smart           // 智能匹配
        }
        
        /// <summary>
        /// 智能匹配节点
        /// </summary>
        /// <param name="srcPrefab">源预制体</param>
        /// <param name="destPrefab">目标预制体</param>
        /// <param name="strategy">匹配策略</param>
        /// <returns>匹配结果字典</returns>
        public static Dictionary<GameObject, GameObject> SmartMatch(
            GameObject srcPrefab, 
            GameObject destPrefab, 
            MatchStrategy strategy = MatchStrategy.Smart)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            switch (strategy)
            {
                case MatchStrategy.FileIDOnly:
                    result = MatchByFileID(srcPrefab, destPrefab);
                    break;
                case MatchStrategy.IdentityOnly:
                    result = MatchByIdentity(srcPrefab, destPrefab);
                    break;
                case MatchStrategy.Hybrid:
                    result = MatchByHybrid(srcPrefab, destPrefab);
                    break;
                case MatchStrategy.Smart:
                    result = MatchBySmart(srcPrefab, destPrefab);
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// 仅FileID匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByFileID(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            var srcFileIDCache = new Dictionary<long, GameObject>();
            var destFileIDCache = new Dictionary<long, GameObject>();
            
            CacheFileID(srcPrefab, srcFileIDCache);
            CacheFileID(destPrefab, destFileIDCache);
            
            foreach (var srcKvp in srcFileIDCache)
            {
                if (destFileIDCache.ContainsKey(srcKvp.Key))
                {
                    result[srcKvp.Value] = destFileIDCache[srcKvp.Key];
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 仅身份组件匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByIdentity(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            var srcIdentityCache = new Dictionary<long, GameObject>();
            var destIdentityCache = new Dictionary<long, GameObject>();
            
            CacheIdentity(srcPrefab, srcIdentityCache);
            CacheIdentity(destPrefab, destIdentityCache);
            
            foreach (var srcKvp in srcIdentityCache)
            {
                if (destIdentityCache.ContainsKey(srcKvp.Key))
                {
                    result[srcKvp.Value] = destIdentityCache[srcKvp.Key];
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 混合匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByHybrid(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            // 第一层：FileID精确匹配
            var fileIDMatches = MatchByFileID(srcPrefab, destPrefab);
            result = fileIDMatches;
            
            // 第二层：身份组件匹配
            var identityMatches = MatchByIdentity(srcPrefab, destPrefab);
            foreach (var kvp in identityMatches)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 智能匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchBySmart(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            // 收集所有匹配候选
            var matchCandidates = CollectMatchCandidates(srcPrefab, destPrefab);
            
            // 按匹配度排序并分配
            var sortedCandidates = matchCandidates
                .OrderByDescending(x => x.MatchScore)
                .ToList();
            
            var usedDestNodes = new HashSet<GameObject>();
            
            foreach (var candidate in sortedCandidates)
            {
                if (!result.ContainsKey(candidate.SourceNode) && 
                    !usedDestNodes.Contains(candidate.DestNode))
                {
                    result[candidate.SourceNode] = candidate.DestNode;
                    usedDestNodes.Add(candidate.DestNode);
                    
                    Debug.Log($"智能匹配: {candidate.SourceNode.name} -> {candidate.DestNode.name} " +
                            $"(分数: {candidate.MatchScore:F2}, 原因: {candidate.MatchReason})");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 收集匹配候选
        /// </summary>
        private static List<MatchResult> CollectMatchCandidates(GameObject srcPrefab, GameObject destPrefab)
        {
            var candidates = new List<MatchResult>();
            
            var srcNodes = GetAllNodes(srcPrefab);
            var destNodes = GetAllNodes(destPrefab);
            
            foreach (var srcNode in srcNodes)
            {
                var srcIdentity = srcNode.GetComponent<NodeIdentityComponent>();
                
                foreach (var destNode in destNodes)
                {
                    var destIdentity = destNode.GetComponent<NodeIdentityComponent>();
                    
                    float matchScore = 0f;
                    string matchReason = "";
                    
                    // 如果两个节点都有身份组件
                    if (srcIdentity != null && destIdentity != null)
                    {
                        matchScore = srcIdentity.CalculateMatchScore(destIdentity);
                        matchReason = "身份组件匹配";
                    }
                    else
                    {
                        // 降级到传统匹配方式
                        matchScore = CalculateTraditionalMatchScore(srcNode, destNode);
                        matchReason = "传统匹配";
                    }
                    
                    if (matchScore > 0.1f) // 只保留有意义的匹配
                    {
                        candidates.Add(new MatchResult(srcNode, destNode, matchScore, matchReason));
                    }
                }
            }
            
            return candidates;
        }
        
        /// <summary>
        /// 计算传统匹配分数
        /// </summary>
        private static float CalculateTraditionalMatchScore(GameObject srcNode, GameObject destNode)
        {
            float score = 0f;
            
            // 名称匹配
            if (srcNode.name == destNode.name)
            {
                score += 0.3f;
            }
            
            // 组件匹配
            var srcComponents = srcNode.GetComponents<Component>().Select(c => c.GetType()).ToHashSet();
            var destComponents = destNode.GetComponents<Component>().Select(c => c.GetType()).ToHashSet();
            float componentSimilarity = srcComponents.Intersect(destComponents).Count() / 
                                      (float)srcComponents.Union(destComponents).Count();
            score += componentSimilarity * 0.4f;
            
            // 位置匹配
            var srcRect = srcNode.GetComponent<RectTransform>();
            var destRect = destNode.GetComponent<RectTransform>();
            if (srcRect != null && destRect != null)
            {
                float positionSimilarity = 1f - Vector3.Distance(srcRect.position, destRect.position) / 100f;
                score += Mathf.Max(0, positionSimilarity) * 0.3f;
            }
            
            return score;
        }
        
        /// <summary>
        /// 缓存FileID
        /// </summary>
        private static void CacheFileID(GameObject prefab, Dictionary<long, GameObject> cache)
        {
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                GameObject child = prefab.transform.GetChild(i).gameObject;
                long fileID = FileIDHelper.GetFileID(child);
                cache[fileID] = child;
                CacheFileID(child, cache);
            }
        }
        
        /// <summary>
        /// 缓存身份组件
        /// </summary>
        private static void CacheIdentity(GameObject prefab, Dictionary<long, GameObject> cache)
        {
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                GameObject child = prefab.transform.GetChild(i).gameObject;
                var identity = child.GetComponent<NodeIdentityComponent>();
                if (identity != null)
                {
                    cache[identity.NodeFileID] = child;
                }
                CacheIdentity(child, cache);
            }
        }
        
        /// <summary>
        /// 获取所有节点
        /// </summary>
        private static List<GameObject> GetAllNodes(GameObject root)
        {
            var nodes = new List<GameObject>();
            GetAllNodesRecursive(root, nodes);
            return nodes;
        }
        
        /// <summary>
        /// 递归获取所有节点
        /// </summary>
        private static void GetAllNodesRecursive(GameObject obj, List<GameObject> nodes)
        {
            nodes.Add(obj);
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GetAllNodesRecursive(obj.transform.GetChild(i).gameObject, nodes);
            }
        }
        
        /// <summary>
        /// 为预制体添加身份组件
        /// </summary>
        /// <param name="prefab">目标预制体</param>
        public static void AddIdentityComponents(GameObject prefab)
        {
            var allNodes = GetAllNodes(prefab);
            int addedCount = 0;
            
            foreach (var node in allNodes)
            {
                if (node.GetComponent<NodeIdentityComponent>() == null)
                {
                    var identity = node.AddComponent<NodeIdentityComponent>();
                    identity.InitializeIdentity();
                    addedCount++;
                }
            }
            
            Debug.Log($"为预制体 {prefab.name} 添加了 {addedCount} 个身份组件");
        }
        
        /// <summary>
        /// 更新预制体中所有身份组件
        /// </summary>
        /// <param name="prefab">目标预制体</param>
        public static void UpdateIdentityComponents(GameObject prefab)
        {
            var allNodes = GetAllNodes(prefab);
            int updatedCount = 0;
            
            foreach (var node in allNodes)
            {
                var identity = node.GetComponent<NodeIdentityComponent>();
                if (identity != null)
                {
                    identity.UpdateIdentity();
                    updatedCount++;
                }
            }
            
            Debug.Log($"更新了预制体 {prefab.name} 中的 {updatedCount} 个身份组件");
        }
    }
} 