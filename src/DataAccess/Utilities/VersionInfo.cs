using System;
using System.Data.SqlClient;
using AdvancedLogging.Models;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;

namespace AdvancedLogging.Utilities
{
    public class VersionInfo : IVersionInfo
    {
        public const string DatabaseVersionProcedure = "dbo.GetDatabaseVersion";
        public const string VersionColumn = "Version";
        public const string BuildColumn = "Build";


        private readonly ISqlHelper _sqlHelper;
        public VersionInfo()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    _sqlHelper = new SqlHelper();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Constructor to use for DI when running unit tests.
        /// </summary>
        public VersionInfo(ISqlHelper helper)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { helper }))
            {
                try
                {
                    _sqlHelper = helper;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { helper }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Retrieves the version details for the Database.
        /// </summary>
        /// <returns>Version Info object</returns>
        public DatabaseVersionInfo GetDatabaseVersion(string connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString }))
            {
                try
                {
                    return GetDatabaseVersion(new SqlConnectionStringBuilder(connectionString));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public DatabaseVersionInfo GetDatabaseVersion(SqlConnectionStringBuilder connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString }))
            {
                try
                {
                    DatabaseVersionInfo versionInfo = new DatabaseVersionInfo();

                    using (var connection = _sqlHelper.OpenNewConnection(connectionString.ConnectionString))
                    {
                        using (var reader = _sqlHelper.ExecuteReader(connection, DatabaseVersionProcedure))
                        {
                            if (reader.Read())
                            {
                                versionInfo.DataBaseVersion = reader.GetString(VersionColumn);
                                versionInfo.DataBaseBuild = reader.GetInt(BuildColumn);
                            }
                        }
                    }

                    return versionInfo;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
