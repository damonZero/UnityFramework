//**************************************************************************************
//Create By wensx on 2020/07/03.
//
//@Description 禁用接口，禁用之后会直接透传事件
//**************************************************************************************

namespace Framework.Touch
{
    public interface IDisable
    {
        // 设置禁用状态
        void SetDisable(bool isDisable);
        // 获取禁用状态
        bool IsDisable();
    }
}
