using System;

namespace Framework.Log
{
    [Serializable]
    public sealed class GameLogModuleRuleData
    {
        public string Module;
        public GameLogLevel MinimumLevel = GameLogLevel.None;

        public GameLogModuleRule ToRule()
        {
            return new GameLogModuleRule(Module, MinimumLevel);
        }
    }
}
