using System.Collections.Generic;
using Framework.Log;
using UnityEngine;

namespace Framework.UIEffectExtensions
{
    /// <summary>
    /// UI 上的模型位置分布器：把多个 UIModel 分散到不同的远距离坐标，避免模型重叠/相互影响。
    /// 对应参考项目 Package/UIEffectExtension/Runtime/UIModelImage/UIModelLocMgr.cs。
    /// </summary>
    public static class UIModelLocMgr
    {
        private const float INTERVAL = 500f; // UIModel 之间的间隔
        private const int PER_COUNT = 4; // 每维上的数量
        private const int PER_COUNT2 = PER_COUNT * PER_COUNT; // 每维数量的平方
        private const int PER_COUNT3 = PER_COUNT2 * PER_COUNT; // 每维数量的立方
        private const float OFFSET = (PER_COUNT - 1) / 2f; // 平移至以原点为中心的偏移

        // 可变 static（非 readonly）：软重启时 StaticReset 置 null，经 Init 惰性重建，避免残留位置索引泄漏。
        private static Stack<int> _emptyPos;
        private static Dictionary<int, Vector3> _posDict;

        // 初始化位置池
        private static void Init()
        {
            _emptyPos ??= new Stack<int>();
            _posDict ??= new Dictionary<int, Vector3>();
            if (_posDict.Count > 0) return;

            // 通过三维方式分布，避免坐标过大导致精度丢失
            for (var i = 0; i < PER_COUNT3; ++i)
            {
                var y = i / PER_COUNT2;
                var rest = i - y * PER_COUNT2;
                var z = rest / PER_COUNT;
                var x = rest - z * PER_COUNT;
                var pos = (new Vector3(x, y, z) - new Vector3(OFFSET, OFFSET, OFFSET)) * INTERVAL;
                _posDict[i] = pos;
                _emptyPos.Push(i);
            }
        }

        // 清理缓存
        public static void Clean()
        {
            _emptyPos?.Clear();
            _posDict?.Clear();
        }

        // 分配一个空闲位置
        public static void GetEmptyPos(out int index, out Vector3 pos)
        {
            Init();
            // 如果 64 个位置都被用完，说明有泄漏
            if (_emptyPos.Count == 0)
            {
                index = -1;
                // 随机个位置兜底
                pos = new Vector3(
                    Random.Range(-INTERVAL, INTERVAL),
                    Random.Range(-INTERVAL, INTERVAL),
                    Random.Range(-INTERVAL, INTERVAL));
                GameLog.Error($"UIModel pos use over! count={_posDict.Count}", module: nameof(UIModelLocMgr));
                return;
            }

            index = _emptyPos.Pop();
            pos = _posDict[index];
        }

        // 回收用完的坐标再分配
        public static void RecyclePos(int index)
        {
            if (index == -1) return;
            _emptyPos.Push(index);
        }
    }
}
