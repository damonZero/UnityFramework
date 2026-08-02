namespace Framework.Pool
{
    public readonly struct PoolStatistics
    {
        public PoolStatistics(int idleCount, int createdCount, int rentCount, int returnCount, int maxIdle)
        {
            IdleCount = idleCount;
            CreatedCount = createdCount;
            RentCount = rentCount;
            ReturnCount = returnCount;
            MaxIdle = maxIdle;
        }

        public int IdleCount { get; }
        public int CreatedCount { get; }
        public int RentCount { get; }
        public int ReturnCount { get; }
        public int MaxIdle { get; }
    }
}
