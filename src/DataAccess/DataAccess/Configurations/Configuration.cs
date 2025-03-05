using AdvancedLogging.Extensions;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AdvancedLogging.DataAccess.Configurations
{
    /// <summary>
    /// Standard class to talk to a Configuration Server.
    /// </summary>
    public class Configuration : IConfigurationServer
    {
        protected IWebClientExtended client;
        protected Dictionary<string, Models.ConfigurationParameter> receivedConfiguration;

        /// <summary>
        /// Create a Configuration that talks to the Configuration Server at the specified URL.
        /// </summary>
        /// <param name="serverBaseUrl">The base URL of the configuration server.</param>
        /// <param name="webClientFactory">Factory to create instances of IWebClient.</param>
        public Configuration(string serverBaseUrl, IWebClientExtendedFactory webClientFactory)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { serverBaseUrl, webClientFactory }))
            {
                try
                {
                    client = webClientFactory.Create();

                    client.BaseAddress = serverBaseUrl;
                    client.Headers.Add("Accept", "application/json");
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { serverBaseUrl, webClientFactory }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get the configuration for the specified application for the specified client.
        /// </summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <returns>
        /// A dictionary containing configuration parameters for the specified client and application.
        /// If the configuration cannot be retrieved, an empty dictionary is returned.
        /// </returns>
        public Dictionary<string, Models.ConfigurationParameter> GetConfigurationParametersWithDefaults(string clientName, string applicationName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { clientName, applicationName }))
            {
                try
                {
                    Dictionary<string, Models.ConfigurationParameter> results = new Dictionary<string, Models.ConfigurationParameter>();

                    try
                    {
                        string url = String.Format("/api/Configuration?clientName={0}&applicationName={1}",
                                                 clientName, applicationName);
                        string path = Uri.EscapeUriString(client.BaseAddress + url);
                        string jsonResponse = client.DownloadString(path, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                        JObject jsonObj = JObject.Parse(jsonResponse);

                        results = jsonObj.ToObject<Dictionary<string, Models.ConfigurationParameter>>();
                    }
                    catch (Exception)
                    {
                        // Logging is likely not initialized at this point -- so we can't log.
                        // This is an "expected" error as we transition to using the Configuration Server -- so I am ignoring it.
                    }
                    return results;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { clientName, applicationName }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
