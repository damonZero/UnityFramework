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

                Assert.That(system.RequestedPaths, Is.EqualTo(new[] { "hero" }));
                Assert.That(system.LoadedPaths, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void RecordingAssetSystemTracksMissingRequestsWithoutSuccessfulLoads()
        {
            var system = new RecordingAssetSystem();

            var result = system.LoadAssetAsync<TestAsset>("missing").GetAwaiter().GetResult();

            Assert.IsNull(result);
            Assert.That(system.RequestedPaths, Is.EqualTo(new[] { "missing" }));
            Assert.That(system.LoadedPaths, Is.Empty);
        }

        [Test]
        public void RecordingAssetSystemTracksReleaseAndUnloadUnused()
        {
            var system = new RecordingAssetSystem();

            system.Release<TestAsset>("hero");
            system.Release("shared");
            system.UnloadUnused();

            Assert.That(system.ReleasedPaths, Is.EqualTo(new[] { "hero", "shared" }));
            Assert.AreEqual(1, system.UnloadUnusedCount);

            system.ClearRecords();

            Assert.That(system.ReleasedPaths, Is.Empty);
            Assert.AreEqual(0, system.UnloadUnusedCount);
        }

        [Test]
        public void RecordingPublisherCapturesPublishedEvents()
        {
            var publisher = new RecordingPublisher<TestEvent>();
            var evt = new TestEvent(7);

            publisher.Publish(evt);

            Assert.AreEqual(1, publisher.Count);
            Assert.AreEqual(evt, publisher.AssertSingle());

            publisher.Clear();

            publisher.AssertEmpty();
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

        private readonly struct TestEvent
        {
            public TestEvent(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }
    }
}
