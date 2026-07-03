using NUnit.Framework;
using UnityEngine;

namespace Framework.TestKit.Assertions
{
    public static class AssertEx
    {
        public static void IsDestroyed(Object value, string message = null)
        {
            Assert.That(value, Is.Null, message ?? "Expected Unity object to be destroyed.");
        }

        public static void IsNotDestroyed(Object value, string message = null)
        {
            Assert.That(value, Is.Not.Null, message ?? "Expected Unity object to exist.");
        }

        public static void AreApproximatelyEqual(float expected, float actual, float tolerance = 0.0001f, string message = null)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance), message);
        }
    }
}
