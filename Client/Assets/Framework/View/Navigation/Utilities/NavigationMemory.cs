//**************************************************************************************
//Create By Liangc on 2023/11/15
//导航系统内存管理
//@Description 对内存进行管理，在各种情况下保证内存的合理使用
//**************************************************************************************

using System.Text;
using UnityEngine;
using System.Collections.Generic;
namespace Framework.View.Navigation
{
    public class NavigationMemory
    {
        //最低内存
        private const int MIN_LIMIT_MEMORY = 100;

        //当前内存
        private int _curLimitMemory = 200;

        //内存限制
        public int LimitMemory
        {
            get => _curLimitMemory;
            set => _curLimitMemory = value < MIN_LIMIT_MEMORY ? MIN_LIMIT_MEMORY : value;
        }

        //清理超过限制的全屏组
        public void ClearGroupMemory(NavigateContainer skipGroup, NavigateContainer clearGroup)
        {
            int curMemory = GroupMemory(clearGroup);
            int curMemoryTmp = curMemory;
            if (curMemoryTmp <= LimitMemory) return;
            //收集需要清理的全屏组,和全屏组之间的非全屏组
            List<NavigateContainer> clearGroups = NavigationFactory.GetContainerList();
            NavigateContainer skipParent = skipGroup.Parent;

            bool betweenFullScreen = false;
            foreach (var group in clearGroup.ForeachContainers(TraversalOrder.Forward))
            {
                if (group == skipGroup || !group.IsStateValid(NavigationStateType.Clear))
                    continue;
                if (skipParent.RelationshipChild && group == skipParent)
                    continue;

                //以全屏组为单位,收集需要清理的全屏组和其后所有非全屏组
                if (group.IsFullScreen())
                {
                    if (curMemoryTmp <= LimitMemory)
                        break;
                    if (group.IsUnlocked(NavigationStateType.Clear))
                    {
                        clearGroups.Add(group);
                        curMemoryTmp -= group.Memory;
                        betweenFullScreen = true;
                    }
                    else
                        betweenFullScreen = false;
                }
                else
                {
                    if (betweenFullScreen && group.IsUnlocked(NavigationStateType.Clear))
                    {
                        clearGroups.Add(group);
                        curMemoryTmp -= group.Memory;
                    }
                }
            }


            PrintLog(clearGroups, curMemoryTmp, curMemory - curMemoryTmp);
            //清理所有收集到的组
            foreach (var group in clearGroups)
            {
                if (group.CanChangeTo(NavigationStateType.Clear))
                    group.Clear();
            }

            NavigationFactory.ReleaseContainerList(clearGroups);
        }

        //导航容器内存
        private int GroupMemory(NavigateContainer group)
        {
            int memory = 0;
            foreach (var g in group.ForeachContainers(TraversalOrder.Forward))
            {
                memory += g.Memory;
            }

            return memory;
        }

        //打印日志
        private void PrintLog(List<NavigateContainer> clearGroups, int curMemory, int clearMemory)
        {
            if (clearGroups.Count == 0) return;
            StringBuilder tostring = new StringBuilder();
            tostring.Append($"清理内存:{clearMemory}," +
                            $"当前内存:{curMemory},清理数量:{clearGroups.Count}----{Time.frameCount}\n");
            foreach (var clearGroup in clearGroups)
            {
                tostring.Append($"    清理导航容器:{clearGroup.Name},清理类型:{clearGroup.Cache.ClearType}");
            }
        }

        public override string ToString()
        {
            if (_curLimitMemory <= 100)
                return "低内存 100M";
            if (_curLimitMemory <= 150)
                return "中内存 150M";
            return "高内存 200M";
        }
    }
}
