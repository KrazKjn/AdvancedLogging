using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using System;
using System.Reflection;

namespace AdvancedLogging.DataAccess
{
    /// <summary>
    /// System web client factory.
    /// </summary>
    public class SystemWebClientFactory : IWebClientFactory
    {
        #region IWebClientFactory implementation

        /// <summary>
        /// Creates a new instance of <see cref="IWebClient"/>.
        /// </summary>
        /// <returns>A new <see cref="IWebClient"/> instance.</returns>
        public IWebClient Create()
        {
            // AutoLogFunction logs the entry and exit of the method, including any exceptions.
            using (var vAutoLogFunction = new AutoLogFunction(MethodBase.GetCurrentMethod()))
            {
                try
                {
                    return new SystemWebClient();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
    }
}
