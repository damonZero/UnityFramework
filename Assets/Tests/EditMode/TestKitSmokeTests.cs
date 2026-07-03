using System;
using Framework.TestKit.Fakes;
using Framework.TestKit.Probes;
using Framework.TestKit.Time;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class TestKitSmokeTests
    {
        [Test]
        public void CallProbeAssertSequenceAcceptsMatchingCalls()
        {
            var probe = new CallProbe();

            probe.Record("init");
            probe.Record("shutdown");

            probe.AssertSequence("init", "shutdown");
        }

        [Test]
        public void RecordingAssetSystemRecordsOnlySuccessfulLoads()
        {
            var system = new RecordingAssetSystem();
            var asset = ScriptableObject.CreateInstance<TestAsset>();

            try
            {
                system.RegisterAsset("hero", asset);

                Assert.Throws<InvalidCastException>(() =>
                    system.LoadAssetAsync<OtherAsset>("hero").GetAwaiter().GetResult());

                Assert.That(system.LoadedPaths, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ManualClockAdvanceUpdatesTimeAndDelta()
        {
            var clock = new ManualClock();
            var sink = new RecordingEventSink<float>();
            clock.Advanced += sink.Record;

            clock.Advance(0.25f);

            Assert.That(clock.Time, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(clock.DeltaTime, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(sink.AssertSingle(), Is.EqualTo(0.25f).Within(0.0001f));
        }

        private sealed class TestAsset : ScriptableObject
        {
        }

        private sealed class OtherAsset : ScriptableObject
        {
        }
    }
}
