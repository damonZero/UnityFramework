using UnityEngine;

namespace Core.Timeline
{
    /// <summary>淡黑屏（切镜过渡）。对应参考项目 Framework/External/DefaultPlayables/CameraSwitch/FadeScene.cs。</summary>
    public class FadeScene : MonoBehaviour
    {
        private Texture2D _blackTexture;
        private float _alpha;
        private float _fadeSpeed;
        private int _fadeDir;
        private bool _state;

        private void Start()
        {
            _blackTexture = new Texture2D(1, 1);
            _blackTexture.SetPixels(new[] { Color.black });
            _blackTexture.Apply();
        }

        private void OnGUI()
        {
            if (_state)
                _alpha += _fadeDir * _fadeSpeed * Time.deltaTime;

            _alpha = Mathf.Clamp01(_alpha);
            GUI.color = new Color(0f, 0f, 0f, _alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _blackTexture, ScaleMode.ScaleAndCrop, false);
        }

        public void BeginFade(float alpha, int dir = -1, float speed = 0.5f)
        {
            _state = true;
            _alpha = alpha;
            _fadeDir = dir;
            _fadeSpeed = speed;
        }

        public void Pause()
        {
            _state = false;
        }

        public void Resume()
        {
            _state = true;
        }

        public bool IsOver()
        {
            return (_fadeDir < 0 && _alpha <= 0) || (_fadeDir > 0 && _alpha >= 1);
        }
    }
}
