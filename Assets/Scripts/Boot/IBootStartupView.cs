namespace Boot
{
    public interface IBootStartupView
    {
        void SetStatus(string message);
        void SetProgress(float progress);
        void SetRepairVisible(bool visible);
    }
}
