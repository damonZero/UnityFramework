using UnityEngine;
using UnityEngine.Playables;

namespace Core.Timeline
{
    /// <summary>UI 跟随行为：把世界物体投影到屏幕，把 UI 元素钉在该点。对应参考项目 Core/TimeLine/UIFlow/UIFlowBehaviour.cs。</summary>
    public class UIFlowBehaviour : PlayableBehaviour
    {
        [Header("需要跟随对象的UI变换")] public RectTransform uiTransform;
        [Header("偏移量")] public Vector3 offset;
        [Header("UI相机")] public Camera uiCamera;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var goTrans = playerData as Transform;
            if (goTrans == null || uiCamera == null || uiTransform == null) return;

            Vector3 pos = uiCamera.WorldToScreenPoint(goTrans.position);
            uiTransform.position = pos + offset;
        }
    }
}
