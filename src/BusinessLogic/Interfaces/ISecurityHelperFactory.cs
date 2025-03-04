using System;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Factory interface for creating instances of <see cref="ISecurityHelper"/>.
    /// </summary>
    public interface ISecurityHelperFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="ISecurityHelper"/> using the specified user name.
        /// </summary>
        /// <param name="userName">The user name for which to create the security helper.</param>
        /// <returns>An instance of <see cref="ISecurityHelper"/>.</returns>
        ISecurityHelper CreateSecurityHelper(string userName);

        /// <summary>
        /// Creates an instance of <see cref="ISecurityHelper"/> using the specified security primary ID.
        /// </summary>
        /// <param name="secPrimaryId">The security primary ID for which to create the security helper.</param>
        /// <returns>An instance of <see cref="ISecurityHelper"/>.</returns>
        ISecurityHelper CreateSecurityHelper(Int64 secPrimaryId);
    }
}
