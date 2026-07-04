namespace Framework.Log
{
    /// <summary>
    /// Applies logging configuration created by build scripts or the future
    /// LOG-TOOLS editor panel. Configure from startup/main thread unless a
    /// runtime hot-switch implementation adds synchronization.
    /// </summary>
    public static class GameLogSwitches
    {
        public static GameLogConfig CurrentConfig { get; private set; }

        public static void Configure(GameLogConfig config)
        {
            CurrentConfig = config;
            GameLog.ApplyProfile(config != null ? config.CreateProfile() : GameLogProfile.Silent());
        }

        public static void ResetToEnvironment(GameLogEnvironment environment)
        {
            Configure(new GameLogConfig { Environment = environment });
        }
    }
}
