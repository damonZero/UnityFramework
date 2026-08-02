namespace Framework.Log
{
    public interface IGameLogSink
    {
        void Write(in GameLogEntry entry);
    }
}
