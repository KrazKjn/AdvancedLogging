using System.Collections.Generic;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for reading a configuration from the Configuration Server
    /// </summary>
    public interface IConfigurationServer
    {
        Dictionary<string, Models.ConfigurationParameter> GetConfigurationParametersWithDefaults(string clientName, string applicationName);
    }
}
