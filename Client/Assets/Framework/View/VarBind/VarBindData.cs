using System;
using System.Collections.Generic;
using Cysharp.Text;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Framework.View
{
    [Serializable]
    public class VarBindData
    {
        public const string VAR_LIST_SUFFIX = "List";

        // 单个对象或对象数组
        [SerializeField] private Object _singleObj;
        [SerializeField] private Object[] _multiObjs;
        [SerializeField] private string _name;
        [SerializeField] private string _typeStr;
        [SerializeField] private string _displayName; // 新增：用于显示的字段名，可修改
        [SerializeField] private List<string> _varyTextAliases = new List<string>(); // VaryText别名列表

        private Type _type;

        public VarBindData(string varName, Type varType, Object varObj)
        {
            Name = varName;
            OriginalName = varName;
            DisplayName = varName; // 初始化显示名称为变量名
            Type = varType;
            _singleObj = varObj;
        }

        /// <summary>
        /// 可能的时机问题，因此都要判断，最后才会进行List -> Array
        /// </summary>
        public bool IsMultiple => MultiObjs is { Count: > 0 } || _multiObjs is { Length: > 0 };

        /// <summary>
        /// 绑定变量显示的名字（只读，用于代码生成）
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// 用户可自定义的显示名称，默认与Name相同
        /// </summary>
        public string DisplayName
        {
            get => string.IsNullOrEmpty(_displayName) ? _name : _displayName;
            set => _displayName = value;
        }

        /// <summary>
        /// 原始的变量名，因为可能被修改（列表会加"List"）
        /// </summary>
        public string OriginalName { get; private set; }

        private List<Object> MultiObjs { get; set; }

        /// <summary>
        /// 绑定变量类型
        /// </summary>
        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                _typeStr = value == null ? string.Empty : value.FullName;
            }
        }

        /// <summary>
        /// VaryText别名列表（用于代码生成）
        /// </summary>
        public List<string> VaryTextAliases
        {
            get => _varyTextAliases;
            set => _varyTextAliases = value ?? new List<string>();
        }

        /// <summary>
        /// 获取绑定变量的类型字符串
        /// </summary>
        public string TypeStr => _typeStr;

        public void AddObject(Object obj)
        {
            if (MultiObjs == null)
            {
                // 从单个转为多个
                MultiObjs = new List<Object> { _singleObj, obj };
                _singleObj = null;
                Name += VAR_LIST_SUFFIX;
            }
            else
            {
                // 已为多个，直接添加
                if (!MultiObjs.Contains(obj))
                    MultiObjs.Add(obj);
            }
        }

        public Object BindObject()
        {
            return _singleObj;
        }

        public Object[] BindObjects()
        {
            return _multiObjs;
        }

        /// <summary>
        /// 最后的处理，List 转 数组，方便使用
        /// </summary>
        public void FinalHandle()
        {
            if (MultiObjs is { Count: > 0 })
            {
                _multiObjs = MultiObjs.ToArray();
            }
        }

        public override string ToString()
        {
            return ZString.Format("VarBindData[Name: {0}, Type: {1}, object: {2}]",
                Name,
                _typeStr,
                _singleObj != null ? _singleObj.ToString() : _multiObjs.ToString());
        }
    }
}
