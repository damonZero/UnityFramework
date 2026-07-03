namespace Framework.Pool
{
    public interface IPool<T>
        where T : class
    {
        T Rent();

        void Return(T item);

        int IdleCount { get; }
    }
}
