using System;

namespace Framework.TestKit.Time
{
    public sealed class ManualClock
    {
        private bool _isAdvancing;

        public float Time { get; private set; }
        public float DeltaTime { get; private set; }

        public event Action<float> Advanced;

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            if (_isAdvancing)
                throw new InvalidOperationException("ManualClock.Advance cannot be called recursively from an Advanced callback.");

            _isAdvancing = true;
            try
            {
                DeltaTime = deltaTime;
                Time += deltaTime;
                Advanced?.Invoke(deltaTime);
            }
            finally
            {
                _isAdvancing = false;
            }
        }

        public void Reset()
        {
            Time = 0f;
            DeltaTime = 0f;
        }
    }
}
