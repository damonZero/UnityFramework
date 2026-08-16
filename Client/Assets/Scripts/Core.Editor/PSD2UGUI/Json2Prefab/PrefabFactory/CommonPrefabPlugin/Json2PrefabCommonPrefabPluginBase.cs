using System.Collections.Generic;
using UnityEngine;

namespace Package.PSD2UGUI
{
    /// <summary>
    /// 公共组件插件基类
    /// 子类只需继承本类, 必须覆写 Names / CustomizedCheck, 其余成员提供默认实现可按需覆写
    /// </summary>
    public abstract class Json2PrefabCommonPrefabPluginBase : IJson2PrefabCommonPrefabPlugin
    {
        /// <summary>
        /// 公共组件名字(多个近似公共组件可以用同一套处理逻辑), 子类必须提供
        /// </summary>
        public abstract IEnumerable<string> Names { get; }

        /// <summary>
        /// 使用自身的位置
        /// </summary>
        public virtual Json2PrefabEnum UseSelfPosition => Json2PrefabEnum.None;

        /// <summary>
        /// 使用自身的宽高
        /// </summary>
        public virtual Json2PrefabEnum UseSelfSize => Json2PrefabEnum.None;

        /// <summary>
        /// 是否中断解析流程,不继续解析当前PSD节点的子节点.如果是false会生成一个不引用公共预制体的普通UGUI节点
        /// </summary>
        public virtual bool IsInterruption => true;

        /// <summary>
        /// 是否默认复用内嵌的公共组件实例
        /// 外层大预制体(Z)内嵌子公共组件(A/B/C)时, 直接把PSD子节点数据套用到预制体内已有的实例上,
        /// 复用其定制化解析, 不重新实例化. 递归处理嵌套实例(如A内又嵌C).
        /// 插件若自行管理子节点生成(列表/页签等), 可覆写为 false 避免叠加.
        /// </summary>
        public virtual bool AutoReuseEmbedded => true;

        /// <summary>
        /// 定制化检查, 子类必须实现
        /// </summary>
        public abstract void CustomizedCheck(PsdNodeBase node, GameObject instance);
    }
}
