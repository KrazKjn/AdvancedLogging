using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Data.SqlClient;
using System.Reflection;

namespace AdvancedLogging.BusinessLogic.Interfaces
{
    /// <summary>
    /// Manages version information for software and database.
    /// </summary>
    public class VersionInfoManager : IVersionInfoManager
    {
        public const string NotApplicable = "N/A";

        private readonly IVersionInfo _versionDataAccess;
        private readonly IAssemblyHelper _assemblyHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionInfoManager"/> class.
        /// </summary>
        public VersionInfoManager()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    _versionDataAccess = new VersionInfo();
                    _assemblyHelper = new AssemblyHelper();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionInfoManager"/> class for dependency injection.
        /// </summary>
        /// <param name="infoDal">The data access layer for version information.</param>
        /// <param name="assemblyHelper">The helper for assembly operations.</param>
        public VersionInfoManager(IVersionInfo infoDal, IAssemblyHelper assemblyHelper)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { infoDal, assemblyHelper }))
            {
                try
                {
                    _versionDataAccess = infoDal;
                    _assemblyHelper = assemblyHelper;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { infoDal, assemblyHelper }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the version details for the specified assembly and database connection string.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <param name="dbConnectionString">The database connection string.</param>
        /// <returns>The version details.</returns>
        public VersionDetails GetVersionDetails(Assembly assembly, string dbConnectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assembly, dbConnectionString }))
            {
                try
                {
                    if (string.IsNullOrEmpty(dbConnectionString))
                        return GetVersionDetails(assembly);
                    else
                        return GetVersionDetails(assembly, new SqlConnectionStringBuilder(dbConnectionString));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assembly, dbConnectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the version details for the specified assembly and SQL connection string builder.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <param name="connectionString">The SQL connection string builder.</param>
        /// <returns>The version details.</returns>
        public VersionDetails GetVersionDetails(Assembly assembly, SqlConnectionStringBuilder connectionString = null)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assembly, connectionString }))
            {
                try
                {
                    var versionDetails = GetVersionDetails(assembly);

                    if (connectionString != null)
                    {
                        var versionInfo = _versionDataAccess.GetDatabaseVersion(connectionString);
                        versionDetails.Database = $"{versionInfo.DataBaseVersion}.{versionInfo.DataBaseBuild}";
                    }

                    return versionDetails;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assembly, connectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the version details for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get version details for.</param>
        /// <returns>The version details.</returns>
        public VersionDetails GetVersionDetails(Assembly assembly)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assembly }))
            {
                try
                {
                    var versionDetails = new VersionDetails();
                    if (assembly == null)
                    {
                        return versionDetails;
                    }

                    versionDetails.Software = _assemblyHelper.GetFormattedVersion(assembly);
                    versionDetails.Database = NotApplicable;

                    return versionDetails;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assembly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
