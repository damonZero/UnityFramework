using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Core.Timeline
{
    /// <summary>过场切镜轨道。对应参考项目 Framework/External/DefaultPlayables/CameraSwitch/CameraSwitchTrack.cs。</summary>
    [Serializable]
    [TrackClipType(typeof(CameraSwitchClip))]
    [TrackColor(0.53f, 0.0f, 0.08f)]
    public class CameraSwitchTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var myAsset = clip.asset as CameraSwitchClip;
                if (myAsset != null)
                    myAsset.clip = clip;
            }

            return ScriptPlayable<CameraSwitchBehaviour>.Create(graph, inputCount);
        }
    }

    /// <summary>切镜行为：淡黑屏时切换 origin/dest 相机的激活状态。对应参考项目 CameraSwitchBehaviour。</summary>
    [Serializable]
    public class CameraSwitchBehaviour : PlayableBehaviour
    {
        private GameObject _target;
        public GameObject target
        {
            get => _target;
            set => _target = value;
        }

        private Camera _origin;
        public Camera origin
        {
            get => _origin;
            set => _origin = value;
        }

        private Camera _dest;
        public Camera dest
        {
            get => _dest;
            set => _dest = value;
        }

        private TimelineClip _clip;
        public TimelineClip clip
        {
            get => _clip;
            set => _clip = value;
        }

        private FadeScene _fadeScene;
        private bool _appear;
        private bool _fade;
        private float _speed = 0.5f;

        public override void OnPlayableCreate(Playable playable)
        {
            base.OnPlayableCreate(playable);
            if (_target == null) return;

            _fadeScene = _target.GetComponent<FadeScene>();
            if (_fadeScene == null)
            {
                _fadeScene = _target.AddComponent<FadeScene>();
                _speed = 2f / (float)(clip.end - clip.start);
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
            if (_fadeScene != null)
            {
                UnityEngine.Object.DestroyImmediate(_fadeScene);
                _fadeScene = null;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);
            if (_fadeScene != null) _fadeScene.Pause();
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            if (_fadeScene == null) return;

            if (!_fade)
            {
                _fade = true;
                _fadeScene.BeginFade(0f, 1, _speed);
            }

            _fadeScene.Resume();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            if (!_appear && _fadeScene != null && _fadeScene.IsOver())
            {
                origin.gameObject.SetActive(false);
                dest.gameObject.SetActive(true);
                _fadeScene.BeginFade(1f, -1, _speed);
                _appear = true;
            }
        }
    }
}
