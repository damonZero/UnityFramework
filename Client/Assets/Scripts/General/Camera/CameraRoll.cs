using UnityEngine;

namespace General
{
    /// <summary>把滚动中心喂给全局 Shader（视差效果）。对应参考项目 General/Camera/CameraRoll.cs。</summary>
    [ExecuteAlways]
    public class CameraRoll : MonoBehaviour
    {
        [SerializeField] private Transform _trCenter;

        private int _centerPropertyId;

        private void Awake()
        {
            _centerPropertyId = Shader.PropertyToID("_RollWorldCenter");
        }

        private void Update()
        {
            if (!_trCenter) return;
            Shader.SetGlobalVector(_centerPropertyId, _trCenter.position);
        }
    }
}
