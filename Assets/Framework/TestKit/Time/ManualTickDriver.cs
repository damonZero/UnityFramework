using System;

namespace Framework.TestKit.Time
{
    public sealed class ManualTickDriver
    {
        public event Action<float> Tick;
        public event Action<float> LateTick;
        public event Action<float> FixedTick;

        public void Step(float deltaTime)
        {
            Tick?.Invoke(deltaTime);
        }

        public void StepLate(float deltaTime)
        {
            LateTick?.Invoke(deltaTime);
        }

        public void StepFixed(float fixedDeltaTime)
        {
            FixedTick?.Invoke(fixedDeltaTime);
        }
    }
}
