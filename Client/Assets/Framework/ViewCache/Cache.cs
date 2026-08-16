using Cysharp.Threading.Tasks;

using Framework.Log;

namespace Framework.ViewCache
{
    /// <summary>
    /// 缓存类
    /// 通过指定策略类，完成对缓存容器中元素的管理
    /// 提供接口Take/Put接口完成对缓存获取或者卸载
    ///
    /// 策略类复杂缓存策略，比如什么时候缓存满了，需要卸载什么元素
    /// 缓存容器类负责缓存资源的创建以及管理，比如GameObject的话就是Initiate，缓存放到指定节点，Scene则是其他实现
    /// 详情参考文档： https://stl.woobest.com/wiki/pages/viewpage.action?pageId=114096923
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="KeyT">缓存key类型</typeparam>
    public class Cache<KeyT, T>
    {
        #region Field

        //container提供Cache的保存，管理，以及创建等接口
        private ICacheResContainer<KeyT, T> Container { get; set; }

        //策略是管理cache的核心，控制资源是否缓存
        private IStrategy<KeyT> _strategy;

        // 缓存容量
        private int _capacity;

        #endregion

        #region 创建函数相关

        internal Cache(int capacity, ICacheResContainer<KeyT, T> container, IStrategy<KeyT> strategy)
        {
            Container = container;
            InitStrategy(strategy);
            Capacity = capacity;
        }

        #endregion

        #region 接口函数

        /// <summary>
        /// 缓存容量
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                if (_capacity == value)
                    return;
                _capacity = value;
                _strategy.Capacity = value;
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _strategy.Clear();
            Container.Clear();
        }

        /// <summary>
        /// 放入缓存
        /// </summary>
        /// <param name="go"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public bool Put(T go, KeyT assetName)
        {
            GameLog.Debug($"{typeof(T)}加入缓存中 cache : {assetName}", module: CacheFactory.CACHER_LOG_DEBUG);
            if (!Container.Put(go, assetName))
            {
                Container.DestroyObj(go);
                return false;
            }

            _strategy.AfterPut(assetName);
            return true;
        }

        /// <summary>
        /// 获取缓存资源，如果不在则创建
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public T Take(KeyT assetName)
        {
            _strategy.BeforeTake(assetName);
            GameLog.Debug($"{typeof(T)}取出缓存{assetName}", module: CacheFactory.CACHER_LOG_DEBUG);
            var val = Container.Take(assetName);
            _strategy.AfterTake(assetName);
            return val;
        }

        /// <summary>
        /// 获取缓存资源,如果缓存中没有则直接创建
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public async UniTask<T> TakeAsync(KeyT assetName)
        {
            GameLog.Debug($"取出缓存{assetName}", module: CacheFactory.CACHER_LOG_DEBUG);
            _strategy.BeforeTake(assetName);

            var instance = await Container.TakeAsync(assetName);

            _strategy.AfterTake(assetName);

            return instance;
        }

        /// <summary>
        /// 获取缓存命中信息
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        public int GetCacheMiss(out int total)
        {
            total = 0;
            return _strategy is StrategyDecorate<KeyT> decorate ? decorate.GetCacheMiss(out total) : 0;
        }

        /// <summary>
        /// 更新缓存
        /// </summary>
        public void Update(float elapsed)
        {
            _strategy.Update(elapsed);
        }

        #endregion


        #region Private Funcitons

        //清理回调
        private void OnEviction(KeyT assetName)
        {
            // Log.Cache.Info($"Cache {typeof(T)} 缓存已满，清理{assetName}");
            Container.Destroy(assetName);
        }

        public void InitStrategy(IStrategy<KeyT> strategy)
        {
            _strategy = strategy;
            strategy.Eviction = OnEviction;
            strategy.Init();
        }


        public override string ToString()
        {
            return $"Cache {typeof(T)} Count-{Container.GetCount()} Container :\n [{Container}]";
        }
        #endregion
    }


    /// <summary>
    /// 缓存类，默认用string作为key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Cache<T> : Cache<string, T>
    {
        internal Cache(int capacity, ICacheResContainer<string, T> container, IStrategy<string> strategy)
            : base(capacity, container, strategy)
        {
        }
    }
}
