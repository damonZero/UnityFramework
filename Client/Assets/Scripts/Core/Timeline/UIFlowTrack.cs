using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Core.Timeline
{
    /// <summary>UI 跟随轨道（绑定要跟随的世界 Transform）。对应参考项目 Core/TimeLine/UIFlow/UIFlowTrack.cs。</summary>
    [TrackClipType(typeof(UIFlowClip)), TrackBindingType(typeof(Transform))]
    public class UIFlowTrack : PlayableTrack
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<UIFlowBehaviour>.Create(graph, inputCount);
        }
    }
}
