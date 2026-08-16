namespace Framework.Timer
{
    /// <summary>
    /// 计时器句柄 — 值类型，零额外堆分配。
    /// 内部持有节点引用 + 版本号，节点被池化复用后旧句柄自动失效，不会误操作新计时器。
    /// </summary>
    public readonly struct TimerHandle
    {
        private readonly TimerNode _node;
        private readonly int _version;

        internal TimerHandle(TimerNode node, int version)
        {
            _node = node;
            _version = version;
        }

        /// <summary>
        /// 句柄是否仍然有效（计时器尚未完成/取消，且节点未被池化复用）。
        /// </summary>
        public bool IsValid => _node != null && _node.IsActive && _node.Version == _version;

        /// <summary>
        /// 暂停该计时器（保留剩余时间）。
        /// </summary>
        public void Pause()
        {
            if (IsValid)
                _node.IsPaused = true;
        }

        /// <summary>
        /// 恢复该计时器。
        /// </summary>
        public void Resume()
        {
            if (IsValid)
                _node.IsPaused = false;
        }

        /// <summary>
        /// 取消该计时器（节点在下次 <see cref="TimerManager.Tick"/> 时回收）。
        /// </summary>
        public void Cancel()
        {
            if (IsValid)
                _node.IsActive = false;
        }
    }
}
