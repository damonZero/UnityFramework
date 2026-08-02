using System;
using System.Collections.Generic;

namespace Framework.Log
{
    [Serializable]
    public sealed class GameLogConfig
    {
        public GameLogEnvironment Environment = GameLogEnvironment.Development;
        public List<GameLogModuleRuleData> ModuleRules = new();

        public GameLogProfile CreateProfile()
        {
            var profile = GameLogProfile.FromEnvironment(Environment);
            foreach (var rule in ModuleRules)
            {
                profile.SetModuleMinimumLevel(rule.Module, rule.MinimumLevel);
            }

            return profile;
        }
    }
}
