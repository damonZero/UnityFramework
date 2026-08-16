using System;

namespace Framework.ViewCache
{
    /// <summary>
    /// 策略接口，当从Cache中获取资源，放入资源由策略管理资源的Destroy
    /// </summary>
    public interface IStrategy<KeyT>
    {
        //缓存容量
        int Capacity { get; set; }

        // 获取缓存资源前
        void BeforeTake(KeyT key);

        //获取缓存资源后
        void AfterTake(KeyT key);

        //放入缓存后
        void AfterPut(KeyT key);

        //更新策略
        void Update(float elapsed);

        //初始化
        void Init();

        // 触发缓存清理操作
        Action<KeyT> Eviction { get; set; }

        //删除缓存
        void Destroy(KeyT key);

        //清理资源
        void Clear();
    }
}
