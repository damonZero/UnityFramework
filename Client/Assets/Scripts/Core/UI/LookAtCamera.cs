using UnityEngine;

namespace Core.UI
{
    /// <summary>公告板：让物体始终面向相机。对应参考项目 Core/UI/Util/LookAtCamera.cs。</summary>
    public class LookAtCamera : MonoBehaviour
    {
        public Transform toCamera;

        private void Update()
        {
            transform.LookAt(toCamera);
        }
    }
}
