//**************************************************************************************
//Create By wensx on 2020/07/06.
//
//@Description 自定义点击接口，当原点击判定逻辑（释放时还在点击对象上）失败时，会调用该接口再
//             进行判定
//**************************************************************************************

using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public interface ICustomClick
    {
        bool CanTriggerClick(PointerEventData eventData);
    }
}
