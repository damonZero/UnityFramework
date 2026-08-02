using System.Collections.Generic;
using NUnit.Framework;

namespace Framework.TestKit.Probes
{
    public sealed class CallProbe
    {
        private readonly List<string> _calls = new();

        public IReadOnlyList<string> Calls => _calls;
        public int Count => _calls.Count;

        public void Record(string call)
        {
            _calls.Add(call);
        }

        public void Clear()
        {
            _calls.Clear();
        }

        public void AssertSequence(params string[] expected)
        {
            expected ??= new string[0];
            if (_calls.Count != expected.Length)
            {
                Assert.Fail(
                    $"Call count mismatch. Expected {expected.Length}, got {_calls.Count}. " +
                    $"Expected: [{string.Join(", ", expected)}]. Actual: [{string.Join(", ", _calls)}].");
            }

            CollectionAssert.AreEqual(expected, _calls, $"Call sequence mismatch. Actual: [{string.Join(", ", _calls)}]");
        }
    }
}
