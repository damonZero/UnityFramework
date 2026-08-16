//**************************************************************************************
//Create By wensx on 2020/07/03.
//
//@Description 透传处理，提供了透传所需的参数，暂时没有提供辅助方法
//**************************************************************************************

using System;

namespace Framework.Touch
{
    [Serializable]
    public class PassHandler
    {
        public readonly BaseTrigger.BaseTriggerParam baseParam = new BaseTrigger.BaseTriggerParam();
        public readonly PassTrigger.PassTriggerParam passParam = new PassTrigger.PassTriggerParam();
    }
}
