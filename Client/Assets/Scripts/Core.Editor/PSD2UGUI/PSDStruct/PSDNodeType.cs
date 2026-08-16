//*****************************************************************************
//Created By Liangc on 2019/6/3
//PSD文件信息类型枚举
//@Description 根据节点类型的不同,分发不同的创建和修复接口
//*****************************************************************************

namespace Package.PSD2UGUI
{
    /// <summary>
    /// PSD信息枚举
    /// </summary>
    public enum PsdNodeType
    {
        Error = 0,//错误
        CommonNode = 1,//节点(PSD中文件夹层)
        OverNode = 2,//结束节点(PSD中文件夹的结束层)
        ButtonNode = 3,//按钮节点(PSD中以"btn"命名的文件夹)
        Image = 4,//图片(PSD中的图片图层)
        Text = 5,//文本(PSD中的文本图层)
    }

}
