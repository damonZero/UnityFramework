namespace KJ.Core
{
    /// <summary>
    /// 资源句柄，封装加载的资源引用。
    /// 使用 using 语句或手动 Dispose 管理生命周期。
    /// </summary>
    public class AssetHandle
    {
        /// <summary>
        /// 资源路径。
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 是否已加载。
        /// </summary>
        public bool IsValid { get; private set; }

        internal UnityEngine.Object RawAsset { get; private set; }

        internal AssetHandle(string path, UnityEngine.Object asset)
        {
            Path = path;
            RawAsset = asset;
            IsValid = asset != null;
        }

        /// <summary>
        /// 获取类型化的资源。
        /// </summary>
        public T Get<T>() where T : UnityEngine.Object
        {
            return RawAsset as T;
        }

        /// <summary>
        /// 释放资源引用。
        /// </summary>
        public void Release()
        {
            RawAsset = null;
            IsValid = false;
        }
    }
}
