using AdvancedLogging.Models;
using System.Data.SqlClient;
using System.Reflection;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for managing version information.
    /// </summary>
    public interface IVersionInfoManager
    {
        /// <summary>
        /// Gets the version details for the specified assembly and database connection string builder.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <param name="ConnectionString">The database connection string builder.</param>
        /// <returns>The version details.</returns>
        VersionDetails GetVersionDetails(Assembly assembly, SqlConnectionStringBuilder ConnectionString);

        /// <summary>
        /// Gets the version details for the specified assembly and database connection string.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <param name="dbConnectionString">The database connection string.</param>
        /// <returns>The version details.</returns>
        VersionDetails GetVersionDetails(Assembly assembly, string dbConnectionString);

        /// <summary>
        /// Gets the version details for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <returns>The version details.</returns>
        VersionDetails GetVersionDetails(Assembly assembly);
    }
}
