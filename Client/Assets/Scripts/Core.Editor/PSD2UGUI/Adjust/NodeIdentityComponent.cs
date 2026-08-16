using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 节点身份标识组件
    /// 用于保存节点的层级结构和FileID信息，便于智能匹配
    /// </summary>
    [AddComponentMenu("UI/节点身份标识")]
    public class NodeIdentityComponent : MonoBehaviour
    {
        [Header("节点身份信息")]
        [SerializeField] private long nodeFileID;
        [SerializeField] private string nodePath;
        [SerializeField] private int hierarchyLevel;
        [SerializeField] private string parentPath;
        
        [Header("匹配权重")]
        [SerializeField] private float fileIDWeight = 1.0f;
        [SerializeField] private float pathWeight = 0.8f;
        [SerializeField] private float levelWeight = 0.6f;
        
        /// <summary>
        /// 节点FileID
        /// </summary>
        public long NodeFileID => nodeFileID;
        
        /// <summary>
        /// 节点路径
        /// </summary>
        public string NodePath => nodePath;
        
        /// <summary>
        /// 层级深度
        /// </summary>
        public int HierarchyLevel => hierarchyLevel;
        
        /// <summary>
        /// 父节点路径
        /// </summary>
        public string ParentPath => parentPath;
        
        /// <summary>
        /// 初始化节点身份信息
        /// </summary>
        public void InitializeIdentity()
        {
            nodeFileID = FileIDHelper.GetFileID(gameObject);
            nodePath = GetNodePath();
            hierarchyLevel = GetHierarchyLevel();
            parentPath = GetParentPath();
        }
        
        /// <summary>
        /// 获取节点完整路径
        /// </summary>
        /// <returns></returns>
        private string GetNodePath()
        {
            string path = gameObject.name;
            Transform current = transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// 获取层级深度
        /// </summary>
        /// <returns></returns>
        private int GetHierarchyLevel()
        {
            int level = 0;
            Transform current = transform.parent;
            
            while (current != null)
            {
                level++;
                current = current.parent;
            }
            
            return level;
        }
        
        /// <summary>
        /// 获取父节点路径
        /// </summary>
        /// <returns></returns>
        private string GetParentPath()
        {
            if (transform.parent == null) return "";
            
            string path = transform.parent.name;
            Transform current = transform.parent.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// 计算与目标节点的匹配度
        /// </summary>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        public float CalculateMatchScore(NodeIdentityComponent targetNode)
        {
            if (targetNode == null) return 0f;
            
            float score = 0f;
            
            // FileID匹配（最高权重）
            if (nodeFileID == targetNode.nodeFileID)
            {
                score += fileIDWeight;
            }
            
            // 路径匹配
            if (nodePath == targetNode.nodePath)
            {
                score += pathWeight;
            }
            else
            {
                // 路径相似度计算
                float pathSimilarity = CalculatePathSimilarity(nodePath, targetNode.nodePath);
                score += pathSimilarity * pathWeight;
            }
            
            // 层级匹配
            if (hierarchyLevel == targetNode.hierarchyLevel)
            {
                score += levelWeight;
            }
            else
            {
                // 层级差异惩罚
                int levelDiff = Mathf.Abs(hierarchyLevel - targetNode.hierarchyLevel);
                float levelPenalty = Mathf.Max(0, levelWeight - levelDiff * 0.1f);
                score += levelPenalty;
            }
            
            // 父节点路径匹配
            if (parentPath == targetNode.parentPath)
            {
                score += 0.4f;
            }
            
            return score;
        }
        
        /// <summary>
        /// 计算路径相似度
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        private float CalculatePathSimilarity(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
                return 0f;
                
            string[] parts1 = path1.Split('/');
            string[] parts2 = path2.Split('/');
            
            int minLength = Mathf.Min(parts1.Length, parts2.Length);
            int maxLength = Mathf.Max(parts1.Length, parts2.Length);
            
            if (minLength == 0) return 0f;
            
            int matchCount = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (parts1[i] == parts2[i])
                {
                    matchCount++;
                }
            }
            
            return (float)matchCount / maxLength;
        }
        
        /// <summary>
        /// 获取节点信息字符串
        /// </summary>
        /// <returns></returns>
        public string GetNodeInfo()
        {
            return $"FileID: {nodeFileID}, Path: {nodePath}, Level: {hierarchyLevel}";
        }
        
        /// <summary>
        /// 在编辑器中显示节点信息
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            // 在编辑器中自动初始化
            if (nodeFileID == 0)
            {
                InitializeIdentity();
            }
        }
        
        /// <summary>
        /// 重置节点身份信息
        /// </summary>
        [ContextMenu("重置节点身份信息")]
        public void ResetIdentity()
        {
            InitializeIdentity();
        }
        
        /// <summary>
        /// 更新节点身份信息
        /// </summary>
        [ContextMenu("更新节点身份信息")]
        public void UpdateIdentity()
        {
            InitializeIdentity();
        }
    }
} 