using System;
using System.Collections.Generic;

namespace Framework.Log
{
    public sealed class GameLogProfile
    {
        private static readonly char[] ModuleSeparators = { '.', '/', '\\' };

        private readonly Dictionary<string, GameLogLevel> _moduleMinimumLevels = new(StringComparer.Ordinal);

        public GameLogProfile(GameLogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public GameLogLevel MinimumLevel { get; private set; }
        public GameLogEnvironment Environment { get; private set; }

        public static GameLogProfile Development()
        {
            return FromEnvironment(GameLogEnvironment.Development);
        }

        public static GameLogProfile Formal()
        {
            return FromEnvironment(GameLogEnvironment.Formal);
        }

        public static GameLogProfile Silent()
        {
            return FromEnvironment(GameLogEnvironment.Silent);
        }

        public static GameLogProfile FromEnvironment(GameLogEnvironment environment)
        {
            return new GameLogProfile(GetDefaultMinimumLevel(environment))
            {
                Environment = environment
            };
        }

        public void ApplyEnvironment(GameLogEnvironment environment)
        {
            Environment = environment;
            MinimumLevel = GetDefaultMinimumLevel(environment);
        }

        public void SetMinimumLevel(GameLogLevel level)
        {
            MinimumLevel = level;
        }

        public void SetModuleMinimumLevel(string module, GameLogLevel level)
        {
            if (string.IsNullOrWhiteSpace(module))
                return;

            _moduleMinimumLevels[module] = level;
        }

        public void ApplyModuleRules(IEnumerable<GameLogModuleRule> rules)
        {
            if (rules == null)
                return;

            foreach (var rule in rules)
            {
                SetModuleMinimumLevel(rule.Module, rule.MinimumLevel);
            }
        }

        public void SetModuleEnabled(string module, bool enabled)
        {
            if (enabled)
            {
                ClearModuleOverride(module);
                return;
            }

            SetModuleMinimumLevel(module, GameLogLevel.None);
        }

        public void ClearModuleOverride(string module)
        {
            if (string.IsNullOrWhiteSpace(module))
                return;

            _moduleMinimumLevels.Remove(module);
        }

        public bool IsEnabled(string module, GameLogLevel level)
        {
            var minimumLevel = ResolveMinimumLevel(module);
            return level >= minimumLevel && level < GameLogLevel.None;
        }

        private GameLogLevel ResolveMinimumLevel(string module)
        {
            if (string.IsNullOrEmpty(module))
                return MinimumLevel;

            var cursor = module;
            while (true)
            {
                if (_moduleMinimumLevels.TryGetValue(cursor, out var level))
                    return level;

                var index = cursor.LastIndexOfAny(ModuleSeparators);
                if (index < 0)
                    return MinimumLevel;

                cursor = cursor[..index];
            }
        }

        private static GameLogLevel GetDefaultMinimumLevel(GameLogEnvironment environment)
        {
            return environment switch
            {
                GameLogEnvironment.Trace => GameLogLevel.Trace,
                GameLogEnvironment.Development => GameLogLevel.Debug,
                GameLogEnvironment.Qa => GameLogLevel.Information,
                GameLogEnvironment.FormalMonitoring => GameLogLevel.Warning,
                GameLogEnvironment.Formal => GameLogLevel.Error,
                _ => GameLogLevel.None
            };
        }
    }
}
