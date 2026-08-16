using System;
using UnityEngine.Serialization;
namespace Framework.View.Navigation.Editor
{
    [Serializable]
    public class NavigationOperateRecordData
    {
        //操作时间
        public DateTime operateTime;

        //操作帧
        public int frame;

        //操作类型
        public NavigationStateType operateType;

        //操作数据
        public object operateData;

        //操作对象类型
        public Type operateObjType;

        //操作对象名
        public string operateObjName;

        //操作对象
        public NavigationBehaviour operateObj;

        //操作C#堆栈
        public string operateCSharpStack;

    }
}
