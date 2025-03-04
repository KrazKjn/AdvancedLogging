namespace AdvancedLogging.Interfaces
{
    using AdvancedLogging.Models;
    using System.Data.SqlClient;

    public interface IVersionInfo
    {
        DatabaseVersionInfo GetDatabaseVersion(SqlConnectionStringBuilder ConnectionString);
        DatabaseVersionInfo GetDatabaseVersion(string dbConnectionString);
    }
}
