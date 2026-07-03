namespace General
{
    /// <summary>
    /// Business lifecycle object. Project code should model business domains with Model, not System.
    /// </summary>
    public interface IModel
    {
        int Priority { get; }
        void Load();
        void Unload();
    }
}
