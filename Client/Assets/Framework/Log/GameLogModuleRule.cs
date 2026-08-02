namespace Framework.Log
{
    public readonly struct GameLogModuleRule
    {
        public GameLogModuleRule(string module, GameLogLevel minimumLevel)
        {
            Module = module ?? string.Empty;
            MinimumLevel = minimumLevel;
        }

        public string Module { get; }
        public GameLogLevel MinimumLevel { get; }
    }
}
