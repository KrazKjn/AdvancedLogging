using System;
using AdvancedLogging.Models;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for accessing security helper data.
    /// </summary>
    public interface ISecurityHelperDataAccess
    {
        /// <summary>
        /// Gets security information based on the user name.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <returns>The security information.</returns>
        SecurityHelperInfo GetSecurityInfo(string userName);

        /// <summary>
        /// Gets security information based on the primary ID.
        /// </summary>
        /// <param name="secPrimaryId">The primary ID.</param>
        /// <returns>The security information.</returns>
        SecurityHelperInfo GetSecurityInfo(Int64 secPrimaryId);

        /// <summary>
        /// Sets the security information with a new encrypted password.
        /// </summary>
        /// <param name="securityInfo">The security information.</param>
        /// <param name="newEncryptedPassword">The new encrypted password.</param>
        /// <returns>The updated security information.</returns>
        SecurityHelperInfo SetSecurityInfo(SecurityHelperInfo securityInfo, string newEncryptedPassword);
    }
}
