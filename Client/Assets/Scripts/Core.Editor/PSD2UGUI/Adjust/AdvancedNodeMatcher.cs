using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 高级节点匹配器
    /// 提供多种精确的节点查找方法
    /// </summary>
    public class AdvancedNodeMatcher
    {
        /// <summary>
        /// 节点特征信息
        /// </summary>
        public class NodeSignature
        {
            public long FileID;
            public string Name;
            public string Path;
            public int HierarchyLevel;
            public Vector3 Position;
            public Vector2 Size;
            public string[] ComponentTypes;
            public string[] SpriteNames;
            public string[] TextContents;
            public Color[] Colors;
            public string[] Tags;
            
            public NodeSignature(GameObject node)
            {
                FileID = FileIDHelper.GetFileID(node);
                Name = node.name;
                Path = GetNodePath(node);
                HierarchyLevel = GetHierarchyLevel(node);
                
                var rectTransform = node.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Position = rectTransform.position;
                    Size = rectTransform.rect.size;
                }
                
                ComponentTypes = node.GetComponents<Component>()
                    .Select(c => c.GetType().Name)
                    .ToArray();
                
                CollectSpriteInfo(node);
                CollectTextInfo(node);
                CollectColorInfo(node);
                CollectTagInfo(node);
            }
            
            private string GetNodePath(GameObject node)
            {
                string path = node.name;
                Transform current = node.transform.parent;
                
                while (current != null)
                {
                    path = current.name + "/" + path;
                    current = current.parent;
                }
                
                return path;
            }
            
            private int GetHierarchyLevel(GameObject node)
            {
                int level = 0;
                Transform current = node.transform.parent;
                
                while (current != null)
                {
                    level++;
                    current = current.parent;
                }
                
                return level;
            }
            
            private void CollectSpriteInfo(GameObject node)
            {
                var sprites = new List<string>();
                
                // 收集Image组件的Sprite
                var images = node.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    if (image.sprite != null)
                    {
                        sprites.Add(image.sprite.name);
                    }
                }
                
                // 收集RawImage组件的Texture
                var rawImages = node.GetComponentsInChildren<RawImage>();
                foreach (var rawImage in rawImages)
                {
                    if (rawImage.texture != null)
                    {
                        sprites.Add(rawImage.texture.name);
                    }
                }
                
                SpriteNames = sprites.ToArray();
            }
            
            private void CollectTextInfo(GameObject node)
            {
                var texts = new List<string>();
                
                // 收集Text组件
                var textComponents = node.GetComponentsInChildren<Text>();
                foreach (var text in textComponents)
                {
                    if (!string.IsNullOrEmpty(text.text))
                    {
                        texts.Add(text.text);
                    }
                }
                
                // 收集TextMeshPro组件
                var tmpComponents = node.GetComponentsInChildren<TextMeshPro>();
                foreach (var tmp in tmpComponents)
                {
                    if (!string.IsNullOrEmpty(tmp.text))
                    {
                        texts.Add(tmp.text);
                    }
                }
                
                TextContents = texts.ToArray();
            }
            
            private void CollectColorInfo(GameObject node)
            {
                var colors = new List<Color>();
                
                // 收集Image组件的颜色
                var images = node.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    colors.Add(image.color);
                }
                
                // 收集Text组件的颜色
                var texts = node.GetComponentsInChildren<Text>();
                foreach (var text in texts)
                {
                    colors.Add(text.color);
                }
                
                Colors = colors.ToArray();
            }
            
            private void CollectTagInfo(GameObject node)
            {
                var tags = new List<string>();
                
                // 收集标签
                if (!string.IsNullOrEmpty(node.tag))
                {
                    tags.Add(node.tag);
                }
                
                // 收集Layer
                tags.Add($"Layer:{node.layer}");
                
                Tags = tags.ToArray();
            }
        }
        
        /// <summary>
        /// 匹配算法类型
        /// </summary>
        public enum MatchAlgorithm
        {
            ExactFileID,        // 精确FileID匹配
            SignatureBased,     // 基于签名的匹配
            ContentBased,       // 基于内容的匹配
            PositionBased,      // 基于位置的匹配
            Hybrid,            // 混合算法
            MachineLearning    // 机器学习算法
        }
        
        /// <summary>
        /// 高级匹配结果
        /// </summary>
        public class AdvancedMatchResult
        {
            public GameObject SourceNode;
            public GameObject DestNode;
            public float Confidence;
            public MatchAlgorithm Algorithm;
            public string[] MatchReasons;
            public Dictionary<string, float> FeatureScores;
            
            public AdvancedMatchResult(GameObject source, GameObject dest, float confidence, 
                MatchAlgorithm algorithm, string[] reasons, Dictionary<string, float> featureScores)
            {
                SourceNode = source;
                DestNode = dest;
                Confidence = confidence;
                Algorithm = algorithm;
                MatchReasons = reasons;
                FeatureScores = featureScores;
            }
        }
        
        /// <summary>
        /// 执行高级节点匹配
        /// </summary>
        public static Dictionary<GameObject, GameObject> AdvancedMatch(
            GameObject srcPrefab, 
            GameObject destPrefab, 
            MatchAlgorithm algorithm = MatchAlgorithm.Hybrid)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            Debug.Log($"开始高级匹配，算法: {algorithm}");
            
            switch (algorithm)
            {
                case MatchAlgorithm.ExactFileID:
                    result = MatchByExactFileID(srcPrefab, destPrefab);
                    break;
                case MatchAlgorithm.SignatureBased:
                    result = MatchBySignature(srcPrefab, destPrefab);
                    break;
                case MatchAlgorithm.ContentBased:
                    result = MatchByContent(srcPrefab, destPrefab);
                    break;
                case MatchAlgorithm.PositionBased:
                    result = MatchByPosition(srcPrefab, destPrefab);
                    break;
                case MatchAlgorithm.Hybrid:
                    result = MatchByHybrid(srcPrefab, destPrefab);
                    break;
                case MatchAlgorithm.MachineLearning:
                    result = MatchByMachineLearning(srcPrefab, destPrefab);
                    break;
            }
            
            Debug.Log($"高级匹配完成，找到 {result.Count} 个匹配");
            
            // 输出匹配详情
            foreach (var match in result)
            {
                Debug.Log($"匹配: {match.Key.name} -> {match.Value.name}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 精确FileID匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByExactFileID(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            var srcSignatures = CreateNodeSignatures(srcPrefab);
            var destSignatures = CreateNodeSignatures(destPrefab);
            
            foreach (var srcSig in srcSignatures)
            {
                var matchingDest = destSignatures.FirstOrDefault(d => d.Value.FileID == srcSig.Value.FileID);
                if (matchingDest.Value != null)
                {
                    result[srcSig.Key] = matchingDest.Key;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 基于内容的匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByContent(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            var srcSignatures = CreateNodeSignatures(srcPrefab);
            var destSignatures = CreateNodeSignatures(destPrefab);
            
            Debug.Log($"内容匹配：源节点数 {srcSignatures.Count}，目标节点数 {destSignatures.Count}");
            
            foreach (var srcSig in srcSignatures)
            {
                var bestMatch = FindBestContentMatch(srcSig.Key, srcSig.Value, destSignatures);
                if (bestMatch != null && bestMatch.Confidence > 0.7f)
                {
                    result[srcSig.Key] = bestMatch.DestNode;
                    Debug.Log($"内容匹配成功: {srcSig.Key.name} -> {bestMatch.DestNode.name} (置信度: {bestMatch.Confidence:F2})");
                }
                else
                {
                    Debug.LogWarning($"内容匹配失败: {srcSig.Key.name} (最佳置信度: {bestMatch?.Confidence:F2})");
                }
            }
            
            Debug.Log($"内容匹配完成，找到 {result.Count} 个匹配");
            return result;
        }
        
        /// <summary>
        /// 基于签名的匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchBySignature(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            var srcSignatures = CreateNodeSignatures(srcPrefab);
            var destSignatures = CreateNodeSignatures(destPrefab);
            
            var candidates = new List<AdvancedMatchResult>();
            
            foreach (var srcSig in srcSignatures)
            {
                foreach (var destSig in destSignatures)
                {
                    var matchResult = CalculateSignatureSimilarity(srcSig.Key, srcSig.Value, destSig.Key, destSig.Value);
                    if (matchResult.Confidence > 0.8f)
                    {
                        candidates.Add(matchResult);
                    }
                }
            }
            
            // 按置信度排序并分配
            var sortedCandidates = candidates.OrderByDescending(x => x.Confidence).ToList();
            var usedDestNodes = new HashSet<GameObject>();
            
            foreach (var candidate in sortedCandidates)
            {
                if (!result.ContainsKey(candidate.SourceNode) && !usedDestNodes.Contains(candidate.DestNode))
                {
                    result[candidate.SourceNode] = candidate.DestNode;
                    usedDestNodes.Add(candidate.DestNode);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 基于位置的匹配
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByPosition(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            var srcSignatures = CreateNodeSignatures(srcPrefab);
            var destSignatures = CreateNodeSignatures(destPrefab);
            
            foreach (var srcSig in srcSignatures)
            {
                var closestNode = FindClosestNode(srcSig.Value, destSignatures);
                if (closestNode != null)
                {
                    result[srcSig.Key] = closestNode;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 混合匹配算法
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByHybrid(GameObject srcPrefab, GameObject destPrefab)
        {
            var result = new Dictionary<GameObject, GameObject>();
            
            // 第一层：精确FileID匹配
            var exactMatches = MatchByExactFileID(srcPrefab, destPrefab);
            result = exactMatches;
            
            // 第二层：签名匹配
            var signatureMatches = MatchBySignature(srcPrefab, destPrefab);
            foreach (var match in signatureMatches)
            {
                if (!result.ContainsKey(match.Key))
                {
                    result[match.Key] = match.Value;
                }
            }
            
            // 第三层：内容匹配
            var contentMatches = MatchByContent(srcPrefab, destPrefab);
            foreach (var match in contentMatches)
            {
                if (!result.ContainsKey(match.Key))
                {
                    result[match.Key] = match.Value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 机器学习匹配算法（简化版）
        /// </summary>
        private static Dictionary<GameObject, GameObject> MatchByMachineLearning(GameObject srcPrefab, GameObject destPrefab)
        {
            // 这里可以实现更复杂的机器学习算法
            // 目前使用加权特征匹配作为简化版本
            return MatchBySignature(srcPrefab, destPrefab);
        }
        
        /// <summary>
        /// 创建节点签名
        /// </summary>
        private static Dictionary<GameObject, NodeSignature> CreateNodeSignatures(GameObject root)
        {
            var signatures = new Dictionary<GameObject, NodeSignature>();
            CreateNodeSignaturesRecursive(root, signatures);
            return signatures;
        }
        
        /// <summary>
        /// 递归创建节点签名
        /// </summary>
        private static void CreateNodeSignaturesRecursive(GameObject node, Dictionary<GameObject, NodeSignature> signatures)
        {
            signatures[node] = new NodeSignature(node);
            
            for (int i = 0; i < node.transform.childCount; i++)
            {
                CreateNodeSignaturesRecursive(node.transform.GetChild(i).gameObject, signatures);
            }
        }
        
        /// <summary>
        /// 计算签名相似度
        /// </summary>
        private static AdvancedMatchResult CalculateSignatureSimilarity(GameObject srcNode, NodeSignature src, GameObject destNode, NodeSignature dest)
        {
            var featureScores = new Dictionary<string, float>();
            var reasons = new List<string>();
            
            // FileID匹配
            float fileIDScore = src.FileID == dest.FileID ? 1.0f : 0.0f;
            featureScores["FileID"] = fileIDScore;
            if (fileIDScore > 0) reasons.Add("FileID匹配");
            
            // 名称匹配
            float nameScore = src.Name == dest.Name ? 1.0f : 0.0f;
            featureScores["Name"] = nameScore;
            if (nameScore > 0) reasons.Add("名称匹配");
            
            // 路径匹配
            float pathScore = CalculateStringSimilarity(src.Path, dest.Path);
            featureScores["Path"] = pathScore;
            if (pathScore > 0.8f) reasons.Add("路径相似");
            
            // 层级匹配
            float levelScore = src.HierarchyLevel == dest.HierarchyLevel ? 1.0f : 
                Mathf.Max(0, 1.0f - Mathf.Abs(src.HierarchyLevel - dest.HierarchyLevel) * 0.2f);
            featureScores["Hierarchy"] = levelScore;
            if (levelScore > 0.8f) reasons.Add("层级匹配");
            
            // 位置匹配
            float positionScore = CalculatePositionSimilarity(src.Position, dest.Position);
            featureScores["Position"] = positionScore;
            if (positionScore > 0.8f) reasons.Add("位置相似");
            
            // 大小匹配
            float sizeScore = CalculateSizeSimilarity(src.Size, dest.Size);
            featureScores["Size"] = sizeScore;
            if (sizeScore > 0.8f) reasons.Add("大小相似");
            
            // 组件匹配
            float componentScore = CalculateComponentSimilarity(src.ComponentTypes, dest.ComponentTypes);
            featureScores["Components"] = componentScore;
            if (componentScore > 0.8f) reasons.Add("组件相似");
            
            // 内容匹配
            float contentScore = CalculateContentSimilarity(src, dest);
            featureScores["Content"] = contentScore;
            if (contentScore > 0.8f) reasons.Add("内容相似");
            
            // 计算综合置信度
            float confidence = featureScores.Values.Average();
            
            return new AdvancedMatchResult(srcNode, destNode, confidence, MatchAlgorithm.SignatureBased, 
                reasons.ToArray(), featureScores);
        }
        
        /// <summary>
        /// 计算字符串相似度
        /// </summary>
        private static float CalculateStringSimilarity(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0f;
                
            if (str1 == str2) return 1f;
            
            // 使用编辑距离计算相似度
            int distance = CalculateLevenshteinDistance(str1, str2);
            int maxLength = Mathf.Max(str1.Length, str2.Length);
            
            return 1f - (float)distance / maxLength;
        }
        
        /// <summary>
        /// 计算编辑距离
        /// </summary>
        private static int CalculateLevenshteinDistance(string str1, string str2)
        {
            int[,] matrix = new int[str1.Length + 1, str2.Length + 1];
            
            for (int i = 0; i <= str1.Length; i++)
                matrix[i, 0] = i;
            
            for (int j = 0; j <= str2.Length; j++)
                matrix[0, j] = j;
            
            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    int cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                    matrix[i, j] = Mathf.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1,
                        matrix[i - 1, j - 1] + cost
                    );
                }
            }
            
            return matrix[str1.Length, str2.Length];
        }
        
        /// <summary>
        /// 计算位置相似度
        /// </summary>
        private static float CalculatePositionSimilarity(Vector3 pos1, Vector3 pos2)
        {
            float distance = Vector3.Distance(pos1, pos2);
            return Mathf.Max(0, 1f - distance / 100f);
        }
        
        /// <summary>
        /// 计算大小相似度
        /// </summary>
        private static float CalculateSizeSimilarity(Vector2 size1, Vector2 size2)
        {
            float area1 = size1.x * size1.y;
            float area2 = size2.x * size2.y;
            
            if (area1 == 0 && area2 == 0) return 1f;
            if (area1 == 0 || area2 == 0) return 0f;
            
            float ratio = Mathf.Min(area1, area2) / Mathf.Max(area1, area2);
            return ratio;
        }
        
        /// <summary>
        /// 计算组件相似度
        /// </summary>
        private static float CalculateComponentSimilarity(string[] components1, string[] components2)
        {
            var set1 = new HashSet<string>(components1);
            var set2 = new HashSet<string>(components2);
            
            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();
            
            return union > 0 ? (float)intersection / union : 0f;
        }
        
        /// <summary>
        /// 计算内容相似度
        /// </summary>
        private static float CalculateContentSimilarity(NodeSignature src, NodeSignature dest)
        {
            float spriteScore = CalculateArraySimilarity(src.SpriteNames, dest.SpriteNames);
            float textScore = CalculateArraySimilarity(src.TextContents, dest.TextContents);
            float colorScore = CalculateColorSimilarity(src.Colors, dest.Colors);
            
            return (spriteScore + textScore + colorScore) / 3f;
        }
        
        /// <summary>
        /// 计算数组相似度
        /// </summary>
        private static float CalculateArraySimilarity(string[] arr1, string[] arr2)
        {
            if (arr1.Length == 0 && arr2.Length == 0) return 1f;
            if (arr1.Length == 0 || arr2.Length == 0) return 0f;
            
            var set1 = new HashSet<string>(arr1);
            var set2 = new HashSet<string>(arr2);
            
            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();
            
            return union > 0 ? (float)intersection / union : 0f;
        }
        
        /// <summary>
        /// 计算颜色相似度
        /// </summary>
        private static float CalculateColorSimilarity(Color[] colors1, Color[] colors2)
        {
            if (colors1.Length == 0 && colors2.Length == 0) return 1f;
            if (colors1.Length == 0 || colors2.Length == 0) return 0f;
            
            float totalSimilarity = 0f;
            int comparisons = 0;
            
            foreach (var color1 in colors1)
            {
                foreach (var color2 in colors2)
                {
                    float similarity = 1f - Vector4.Distance(color1, color2);
                    totalSimilarity += Mathf.Max(0, similarity);
                    comparisons++;
                }
            }
            
            return comparisons > 0 ? totalSimilarity / comparisons : 0f;
        }
        
        /// <summary>
        /// 查找最佳内容匹配
        /// </summary>
        private static AdvancedMatchResult FindBestContentMatch(GameObject srcNode, NodeSignature src, Dictionary<GameObject, NodeSignature> destSignatures)
        {
            AdvancedMatchResult bestMatch = null;
            float bestScore = 0f;
            
            Debug.Log($"为节点 {srcNode.name} 查找最佳内容匹配，候选目标节点数: {destSignatures.Count}");
            
            foreach (var destSig in destSignatures)
            {
                var matchResult = CalculateSignatureSimilarity(srcNode, src, destSig.Key, destSig.Value);
                if (matchResult.Confidence > bestScore)
                {
                    bestScore = matchResult.Confidence;
                    bestMatch = matchResult;
                    Debug.Log($"发现更好的匹配: {destSig.Key.name} (置信度: {matchResult.Confidence:F2})");
                }
            }
            
            if (bestMatch != null)
            {
                Debug.Log($"最佳匹配: {srcNode.name} -> {bestMatch.DestNode.name} (置信度: {bestMatch.Confidence:F2})");
            }
            else
            {
                Debug.LogWarning($"未找到匹配: {srcNode.name}");
            }
            
            return bestMatch;
        }
        
        /// <summary>
        /// 查找最近节点
        /// </summary>
        private static GameObject FindClosestNode(NodeSignature src, Dictionary<GameObject, NodeSignature> destSignatures)
        {
            GameObject closestNode = null;
            float closestDistance = float.MaxValue;
            
            foreach (var destSig in destSignatures)
            {
                float distance = Vector3.Distance(src.Position, destSig.Value.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = destSig.Key;
                }
            }
            
            return closestNode;
        }
    }
} 