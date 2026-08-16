using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public class Psd2UguiStatisticsJson
    {
        //psd名称
        public string psdName;

        //公共组件数量
        public int count;

        public string nameOrIp;

        //公共组件名字
        public readonly HashSet<string> prefabs = new HashSet<string>();
    }


    public static class Psd2UguiStatistics
    {
        private static Psd2UguiStatisticsJson _statistics = new Psd2UguiStatisticsJson();

        public static void Reset(string path)
        {
            _statistics.psdName = Path.GetFileNameWithoutExtension(path);
            _statistics.count = 0;
            _statistics.prefabs.Clear();
        }

        public static void UseCommonPrefab(string prefabName)
        {
            _statistics.prefabs.Add(prefabName);
            _statistics.count = _statistics.prefabs.Count;
        }

        /// <summary>
        /// 导出统计上报。原 P33 通过飞书 webhook 上报，移植后暂只输出到日志。
        /// </summary>
        public static void Statistics()
        {
            Debug.Log($"[PSD2UGUI] 导出统计: psd={_statistics.psdName}, 公共组件数={_statistics.count}");
        }
    }
}
