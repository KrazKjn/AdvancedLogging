using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using System;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Factory class for creating instances of ISecurityHelper.
    /// </summary>
    public class SecurityHelperFactory : ISecurityHelperFactory
    {
        private readonly ISecurityHelperDataAccess dal;
        private readonly ICommonLogger _logger;
        private readonly ILoggingContext _loggingContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHelperFactory"/> class.
        /// </summary>
        /// <param name="dalInterface">The data access layer interface for security helper.</param>
        public SecurityHelperFactory(ICommonLogger logger, ILoggingContext loggingContext, ISecurityHelperDataAccess dalInterface)
        {
            _logger = logger;
            _loggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { dalInterface }))
            {
                try
                {
                    dal = dalInterface;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dalInterface }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates an instance of ISecurityHelper using the specified user name.
        /// </summary>
        /// <param name="userName">The user name for which to create the security helper.</param>
        /// <returns>An instance of ISecurityHelper.</returns>
        public ISecurityHelper CreateSecurityHelper(string userName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { userName }))
            {
                try
                {
                    return new SecurityHelper(_logger, _loggingContext, userName, dal);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { userName }, null, true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates an instance of ISecurityHelper using the specified primary ID.
        /// </summary>
        /// <param name="secPrimaryId">The primary ID for which to create the security helper.</param>
        /// <returns>An instance of ISecurityHelper.</returns>
        public ISecurityHelper CreateSecurityHelper(Int64 secPrimaryId)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { secPrimaryId }))
            {
                try
                {
                    return new SecurityHelper(_logger, _loggingContext, secPrimaryId, dal);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { secPrimaryId }, null, true, exOuter);
                    throw;
                }
            }
        }
    }
}
