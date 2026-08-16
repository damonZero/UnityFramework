//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航容器缓存类
//@Description 负责将导航容器在清理时将Loader对象缓存起来，还原时恢复，让导航系统做的无限跳转的效果
//**************************************************************************************

using System;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Framework.View.Navigation
{
    public class NavigatorCached
    {
        //缓存的加载器
        private List<NavigationLoader> LoadersToRecover { get; set; }

        //清理类型
        public NavigationClearType ClearType { get; set; } = NavigationClearType.EntranceRecover;

        //当前清理状态
        public NavigationClearType CurState { get; internal set; } = NavigationClearType.Complete;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool Empty => LoadersToRecover == null || LoadersToRecover.Count == 0;

        /// <summary>
        /// 加载器是否被缓存
        /// </summary>
        /// <param name="loader"></param>
        /// <returns></returns>
        internal bool IsLoaderCached(NavigationLoader loader)
        {
            return LoadersToRecover != null && LoadersToRecover.Contains(loader);
        }

        /// <summary>
        /// 清理一批界面/场景
        /// </summary>
        /// <param name="loaders">加载器列表</param>
        /// <param name="data">任意类型参数</param>
        internal void Clear(List<NavigationLoader> loaders, object data = null)
        {
            // 拷贝到临时列表，避免清理过程中输入列表变化
            var tempList = NavigationFactory.GetLoaderList();
            foreach (var loader in loaders)
            {
                tempList.Add(loader);
            }

            var loadersToRecover = NavigationFactory.GetLoaderList();
            LoadersToRecover = loadersToRecover;

            // 如果单个界面/场景Close的过程中又打开新界面，要不要处理呢？ => 先不处理
            foreach (var loader in tempList)
            {
                object saveData;
                switch (ClearType)
                {
                    case NavigationClearType.EntranceRecover:
                        saveData = loader.Save();
                        if (saveData != null)
                        {
                            loader.OpenShowData = saveData;
                            loadersToRecover.Add(loader);
                        }
                        else if (loader.Entrance)
                        {
                            loadersToRecover.Add(loader);
                        }

                        break;

                    case NavigationClearType.AllRecover:
                        saveData = loader.Save();
                        if (saveData != null)
                        {
                            loader.OpenShowData = saveData;
                        }

                        loadersToRecover.Add(loader);
                        break;

                    case NavigationClearType.ClearMemory:
                        loader.Clear();
                        break;
                }
            }

            //分两次遍历,避免在Close时加载器,因为业务层代码调用关闭而重置加载器状态
            if (ClearType != NavigationClearType.ClearMemory)
            {
                foreach (var loader in tempList)
                {
                    if (loader.Name != null)
                        loader.Close();
                }
            }

            CurState = ClearType;
            NavigationFactory.ReleaseLoaderList(tempList);
        }

        /// <summary>
        /// 还原缓存的界面/场景
        /// </summary>
        /// <param name="open"></param>
        internal async UniTask Recover(Func<NavigationLoader, UniTask<NavigationLoader>> open)
        {
            var loaders = LoadersToRecover;
            if (loaders == null || loaders.Count == 0)
            {
                CurState = CurState >= NavigationClearType.AllRecover ? NavigationClearType.Complete : CurState;
                return;
            }

            bool transition = false;
            foreach (var loader in loaders)
            {
                loader.BeforeSetRecover(); //还原导航容器时,模拟已打开的情况,以便OnShow中的isOpen参数保持一致
                var retLoader = await open(loader);

                if (loader != retLoader)
                {
                    NavigationFactory.Instance.Recycle(loader);
                }

                if (!(loader.Transition?.IsNoOp ?? true))
                {
                    transition = true;
                }
            }

            LoadersToRecover = null;
            NavigationFactory.ReleaseLoaderList(loaders);
            CurState = CurState >= NavigationClearType.AllRecover ? NavigationClearType.Complete : CurState;
        }

        /// <summary>
        /// 验证重复清理
        /// </summary>
        /// <returns>验证通过返回true</returns>
        internal bool VerifyRepeat()
        {
            //只能进行比当前状态更重度的清理,更轻度的清理无意义
            return ClearType > CurState;
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        /// <returns></returns>
        internal bool Visible()
        {
            return CurState <= NavigationClearType.ClearMemory;
        }

        /// <summary>
        /// 查找加载器
        /// </summary>
        /// <param name="targetName">加载器名</param>
        /// <returns></returns>
        internal NavigationLoader FindLoader(string targetName)
        {
            var loaders = LoadersToRecover;
            if (loaders == null) return default;
            foreach (var loader in LoadersToRecover)
            {
                if (loader.Name == targetName) return loader;
            }
            return null;
        }

        /// <summary>
        /// 移除加载器
        /// </summary>
        /// <param name="loader"></param>
        internal bool RemoveLoader(NavigationLoader loader)
        {
            return LoadersToRecover.Remove(loader);
        }

        /// <summary>
        /// 重置导航容器缓存
        /// </summary>
        internal void Reset()
        {
            ClearType = NavigationClearType.EntranceRecover;
            CurState = NavigationClearType.Complete;
            if (LoadersToRecover != null)
            {
                NavigationFactory.ReleaseLoaderList(LoadersToRecover);
                LoadersToRecover = null;
            }
        }

        /// <summary>
        /// 是否有入口
        /// </summary>
        /// <returns></returns>
        internal bool HasEntrance()
        {
            if (LoadersToRecover == null)
                return false;
            foreach (var loader in LoadersToRecover)
            {
                if (loader.Entrance)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取第一个加载器
        /// </summary>
        /// <returns></returns>
        internal NavigationLoader GetFirstLoader()
        {
            if (LoadersToRecover == null || LoadersToRecover.Count == 0)
                return null;
            return LoadersToRecover[0];
        }

        /// <summary>
        /// 获取最后一个加载器
        /// </summary>
        /// <returns></returns>
        internal NavigationLoader GetLastLoader()
        {
            if (LoadersToRecover == null || LoadersToRecover.Count == 0)
                return null;
            return LoadersToRecover[LoadersToRecover.Count - 1];
        }

        /// <summary>
        /// 遍历缓存的Loader
        /// </summary>
        /// <param name="order">遍历顺序</param>
        /// <returns>Loader迭代器</returns>
        public IEnumerable<NavigationLoader> Loaders(TraversalOrder order = TraversalOrder.Reverse)
        {
            if (LoadersToRecover == null || LoadersToRecover.Count == 0)
                yield break;

            if (order == TraversalOrder.Forward)
            {
                for (int i = 0; i < LoadersToRecover.Count; i++)
                {
                    if (LoadersToRecover == null || i >= LoadersToRecover.Count)
                        yield break;
                    yield return LoadersToRecover[i];
                }
            }
            else
            {
                for (int i = LoadersToRecover.Count - 1; i >= 0; i--)
                {
                    if (LoadersToRecover == null || i >= LoadersToRecover.Count)
                        yield break;
                    yield return LoadersToRecover[i];
                }
            }
        }

        /// <summary>
        /// 遍历Loader（兼容旧方法）
        /// </summary>
        [Obsolete("Use Loaders(TraversalOrder) instead")]
        public IEnumerable<NavigationLoader> ForeachLoader(bool forward = false)
        {
            return Loaders(forward ? TraversalOrder.Forward : TraversalOrder.Reverse);
        }

        public override string ToString()
        {
            var toString = new StringBuilder();
            foreach (var loader in LoadersToRecover)
            {
                toString.Append($"       [{loader.Name}]==>\n");
            }

            return toString.ToString();
        }
    }
}
