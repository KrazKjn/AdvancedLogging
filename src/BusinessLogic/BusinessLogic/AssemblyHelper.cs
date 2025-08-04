using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using System;
using System.Reflection;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Provides helper methods for working with assemblies.
    /// </summary>
    public class AssemblyHelper : IAssemblyHelper
    {
        private readonly ICommonLogger _logger;
        private readonly ILoggingContext _loggingContext;

        public AssemblyHelper(ICommonLogger logger, ILoggingContext loggingContext)
        {
            _logger = logger;
            _loggingContext = loggingContext;
        }

        /// <summary>
        /// Gets the formatted version string of the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the version from.</param>
        /// <returns>A string representing the version in the format "Major.Minor.Build".</returns>
        public string GetFormattedVersion(Assembly assembly)
        {
            // Using AutoLogFunction to automatically log the function execution
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assembly }))
            {
                try
                {
                    // Get the version information from the assembly
                    var versionInfo = assembly.GetName().Version;
                    // Return the formatted version string
                    return $"{versionInfo.Major}.{versionInfo.Minor}.{versionInfo.Build}";
                }
                catch (Exception exOuter)
                {
                    // Log the exception and rethrow it
                    vAutoLogFunction.LogFunction(new { assembly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
