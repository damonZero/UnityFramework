using UnityEngine;

namespace KJ.Boot
{
    /// <summary>
    /// 游戏启动入口。
    /// 只保留最小启动壳，具体依赖图由 GameLifetimeScope 管理。
    /// </summary>
    [RequireComponent(typeof(GameLifetimeScope))]
    public class Entry : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Entry] 游戏启动");
        }
    }
}
