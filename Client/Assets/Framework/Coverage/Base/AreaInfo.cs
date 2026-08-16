using System;
using UnityEngine;

namespace Framework.Coverage
{

    /// <summary>
    /// 显示/遮挡区域信息
    /// </summary>
    [Serializable]
    public class AreaInfo
    {
        [SerializeField]
        public RectTransform anchorTrans;
        [SerializeField]
        public int offsetUp;
        [SerializeField]
        public int offsetDown;
        [SerializeField]
        public int offsetLeft;
        [SerializeField]
        public int offsetRight;

        public AreaInfo(RectTransform trans)
        {
            anchorTrans = trans;
        }
    }
}
