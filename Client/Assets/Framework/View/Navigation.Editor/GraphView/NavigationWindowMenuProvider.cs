using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
namespace Framework.View.Navigation.Editor
{
    public class NavigationWindowMenuProvider : ScriptableObject, ISearchWindowProvider
    {
        /// <summary>
        /// 当选中一个节点时的回调
        /// </summary>
        public Func<SearchTreeEntry, SearchWindowContext, bool> onSelectEntryCb;

        /// <summary>
        /// 创建菜单
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                //1级菜单
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };
            return entries;
        }

        /// <summary>
        /// 当选中一个节点时的回调
        /// </summary>
        /// <param name="searchTreeEntry"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (onSelectEntryCb == null) return false;
            return onSelectEntryCb.Invoke(searchTreeEntry, context);
        }
    }
}
