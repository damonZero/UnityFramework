using System;
using UnityEngine;

namespace Framework.UIEffectExtensions
{
    /// <summary>
    /// 相机渲染 3D 模型到 RawImage 的相机组件。
    /// 在相机 Cull 之前将渲染对象搬到较远位置渲染，避免相互影响，渲染完成后还原位置。
    /// 对应参考项目 Package/UIEffectExtension/Runtime/UIModelImage/UIModelCam.cs。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class UIModelCam : MonoBehaviour
    {
        [HideInInspector] public GameObject lightObject; // 灯光容器

        public static event Action<Camera> OnCreateCamera;

        private void Awake()
        {
            OnCreateCamera?.Invoke(GetComponent<Camera>());
        }
    }
}
