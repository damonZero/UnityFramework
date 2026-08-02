namespace Framework.Aop
{
    public interface IAopCollector
    {
        void Record(AopEvent spanEvent);
    }
}
