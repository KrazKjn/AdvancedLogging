using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using System;

[assembly: CLSCompliant(true)]
namespace AdvancedLogging.DataAccess.Configurations
{
    /// <summary>
    /// Class to implement default factory for a Configuration Server
    /// </summary>
    public class ConfigurationFactory : IConfigurationServerFactory
    {
        /// <summary>
        /// Create a new instance of a configuration server for the specified URL
        /// </summary>
        /// <param name="url">The URL of the configuration server.</param>
        /// <returns>An instance of a configuration server for the specified URL.</returns>
        public IConfigurationServer Create(string url)
        {
            // AutoLogFunction is used for logging the function execution details
            using (var vAutoLogFunction = new AutoLogFunction(new { url }))
            {
                try
                {
                    // Configuration is a class that represents the configuration server
                    // SystemWebClientFactory is a class that creates web client instances
                    return new Configuration(url, new WebClientExtendedFactory());
                }
                catch (Exception exOuter)
                {
                    // LogFunction logs the details of the function execution and any exceptions
                    vAutoLogFunction.LogFunction(new { url }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
