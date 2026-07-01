using UnityEngine;

namespace Boot
{
    /// <summary>
    /// 游戏启动入口。
    /// 只保留最小启动壳，具体依赖图由上层 LifetimeScope 管理。
    /// </summary>
    public class Entry : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Entry] 游戏启动");
        }
    }
}
