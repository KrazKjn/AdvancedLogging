namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for creating a Configuration Server Data Access Layer Object
    /// </summary>
    public interface IConfigurationServerFactory
    {
        IConfigurationServer Create(string url);
    }
}
