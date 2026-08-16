using System;
using System.Collections.Generic;

namespace Framework.View.Editor
{
    public interface IAutoBindingRegister
    {
        /// <summary>
        /// 变量绑定类型映射：前缀字符串 -> 绑定类型
        /// </summary>
        Dictionary<string, Type> PrefixTypeDict { get; }
    }
}
