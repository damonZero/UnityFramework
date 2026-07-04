using System;

namespace Framework.Log
{
    /// <summary>
    /// Reserved for LOG-TOOLS: marks a const string as a module tree root that an
    /// editor panel or build pipeline can scan into <see cref="GameLogConfig"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GameLogTreeAttribute : Attribute
    {
        public GameLogTreeAttribute(string rootPath, string filePattern)
        {
            RootPath = rootPath ?? string.Empty;
            FilePattern = filePattern ?? string.Empty;
        }

        public string RootPath { get; }
        public string FilePattern { get; }
    }
}
