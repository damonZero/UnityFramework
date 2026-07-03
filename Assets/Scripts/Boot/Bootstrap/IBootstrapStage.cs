namespace Boot
{
    public interface IBootstrapStage
    {
        int Priority { get; }
        string StageName { get; }
        void Configure(BootstrapContext context);
    }
}
