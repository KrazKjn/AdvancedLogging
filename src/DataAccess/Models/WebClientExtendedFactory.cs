using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using System;
using System.Reflection;

namespace AdvancedLogging.Models
{
    /// <summary>
    /// System web client factory.
    /// </summary>
    public class WebClientExtendedFactory : IWebClientExtendedFactory
    {
        #region IWebClientFactory implementation

        /// <summary>
        /// Creates a new instance of <see cref="IWebClientExtended"/>.
        /// </summary>
        /// <returns>A new <see cref="IWebClientExtended"/> instance.</returns>
        public IWebClientExtended Create()
        {
            // AutoLogFunction logs the entry and exit of the method, including any exceptions.
            using (var vAutoLogFunction = new AutoLogFunction(MethodBase.GetCurrentMethod()))
            {
                try
                {
                    return new WebClientExtended();
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
