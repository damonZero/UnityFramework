using System.Collections.Generic;

namespace Core.Systems
{
    public interface ICoreStartupStatus
    {
        bool IsStarted { get; }
        bool HasInitFailures { get; }
        IReadOnlyList<string> FailedSystemNames { get; }
    }
}
