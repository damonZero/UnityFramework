using Core.UI;
using Core.ViewSystem;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class ViewFrameworkTests
    {
        [Test]
        public void FormLifecycleEvent_PhaseConstructor_SetsPhaseAndForm()
        {
            var evt = new FormLifecycleEvent(FormLifecyclePhase.PostOpen, null);

            Assert.AreEqual(FormLifecyclePhase.PostOpen, evt.Phase);
            Assert.IsNull(evt.Form);
            Assert.AreEqual(0, evt.OldLayer);
            Assert.AreEqual(0, evt.NewLayer);
        }

        [Test]
        public void FormLifecycleEvent_LayerChangedConstructor_SetsLayers()
        {
            var evt = new FormLifecycleEvent(null, 3, 7);

            Assert.AreEqual(FormLifecyclePhase.LayerChanged, evt.Phase);
            Assert.AreEqual(3, evt.OldLayer);
            Assert.AreEqual(7, evt.NewLayer);
        }

        [Test]
        public void ScreenHelper_HasExpectedDesignResolution()
        {
            Assert.AreEqual(750, ScreenHelper.StandardWidth);
            Assert.AreEqual(1624, ScreenHelper.StandardHeight);
            Assert.AreEqual(750f / 1624f, ScreenHelper.StandardAspect);
        }
    }
}
