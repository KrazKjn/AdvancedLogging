using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using System;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Factory class for creating instances of ISecurityHelper.
    /// </summary>
    public class SecurityHelperFactory : ISecurityHelperFactory
    {
        private readonly ISecurityHelperDataAccess dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHelperFactory"/> class.
        /// </summary>
        /// <param name="dalInterface">The data access layer interface for security helper.</param>
        public SecurityHelperFactory(ISecurityHelperDataAccess dalInterface)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dalInterface }))
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
            using (var vAutoLogFunction = new AutoLogFunction(new { userName }))
            {
                try
                {
                    return new SecurityHelper(userName, dal);
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
            using (var vAutoLogFunction = new AutoLogFunction(new { secPrimaryId }))
            {
                try
                {
                    return new SecurityHelper(secPrimaryId, dal);
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
