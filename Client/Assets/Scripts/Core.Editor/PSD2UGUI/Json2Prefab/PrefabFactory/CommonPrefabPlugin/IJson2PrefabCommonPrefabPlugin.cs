using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    public enum Json2PrefabEnum
    {
        None,

        //宽高
        Width,
        Height,
        WidthHeight,

        //位置
        X,
        Y,
        XY,
    }

    public interface IJson2PrefabCommonPrefabPlugin
    {
        //公共组件名字(多个近似公共组件可以用同一套处理逻辑)
        IEnumerable<string> Names { get; }

        //使用自身的位置
        Json2PrefabEnum UseSelfPosition { get; }

        //使用自身的宽高
        Json2PrefabEnum UseSelfSize { get; }

        //是否会阻断当前节点的子节点生成
        bool IsInterruption { get; }

        //是否自动复用内嵌的公共组件实例
        bool AutoReuseEmbedded { get; }

        //定制化检查
        void CustomizedCheck(PsdNodeBase node, GameObject instance);
    }
}