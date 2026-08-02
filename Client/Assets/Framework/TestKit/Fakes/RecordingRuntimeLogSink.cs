using System.Collections.Generic;
using Framework.Log;

namespace Framework.TestKit.Fakes
{
    public sealed class RecordingRuntimeLogSink : IGameLogSink
    {
        private readonly List<GameLogEntry> _entries = new();

        public IReadOnlyList<GameLogEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Write(in GameLogEntry entry)
        {
            _entries.Add(entry);
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
