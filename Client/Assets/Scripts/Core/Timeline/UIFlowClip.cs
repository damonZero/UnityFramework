using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Core.Timeline
{
    /// <summary>UI 跟随片段。对应参考项目 Core/TimeLine/UIFlow/UIFlowClip.cs。</summary>
    public class UIFlowClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("UI相机")] public ExposedReference<Camera> uiCamera;
        [Header("UI的变化")] public ExposedReference<RectTransform> uiTransform;
        [Header("偏移量")] public Vector3 offset;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<UIFlowBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.uiTransform = uiTransform.Resolve(graph.GetResolver());
            behaviour.offset = offset;
            behaviour.uiCamera = uiCamera.Resolve(graph.GetResolver());
            return playable;
        }
    }
}
