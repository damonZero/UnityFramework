using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Core.Timeline
{
    /// <summary>过场切镜片段：淡黑屏切换两个相机。对应参考项目 Framework/External/DefaultPlayables/CameraSwitch/CameraSwitchClip.cs。</summary>
    [Serializable]
    public class CameraSwitchClip : PlayableAsset, ITimelineClipAsset
    {
        public CameraSwitchBehaviour template = new CameraSwitchBehaviour();
        public ExposedReference<Camera> origin;
        public ExposedReference<Camera> dest;

        private TimelineClip _clip;
        public TimelineClip clip
        {
            get => _clip;
            set => _clip = value;
        }

        public ClipCaps clipCaps => ClipCaps.Extrapolation | ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            template.origin = origin.Resolve(graph.GetResolver());
            template.dest = dest.Resolve(graph.GetResolver());
            template.clip = clip;
            template.target = owner;

            return ScriptPlayable<CameraSwitchBehaviour>.Create(graph, template);
        }
    }
}
