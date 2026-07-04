using System;

namespace Framework.Log
{
    /// <summary>
    /// Reserved for LOG-TOOLS: marks a const string as an explicit named log
    /// switch with a default minimum level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GameLogSwitchAttribute : Attribute
    {
        public GameLogSwitchAttribute(GameLogLevel minimumLevel = GameLogLevel.Debug)
        {
            MinimumLevel = minimumLevel;
        }

        public GameLogLevel MinimumLevel { get; }
    }
}
