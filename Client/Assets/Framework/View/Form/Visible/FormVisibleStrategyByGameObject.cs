// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    public sealed class FormVisibleGameObject : IVisibleStrategy
    {
        public static FormVisibleGameObject Shared { get; } = new ();

        /// <summary>
        /// 构造函数设置为private，不允许new，直接使用Shared实例即可
        /// </summary>
        private FormVisibleGameObject()
        {
        }

        public void SetVisible(ViewBase view, bool visible)
        {
            if (view is BaseForm form)
            {
                form.gameObject.SetActive(visible);
            }
            else
            {
                Log.Error($"{view} is not a {nameof(BaseForm)}");
            }
        }
    }
}
