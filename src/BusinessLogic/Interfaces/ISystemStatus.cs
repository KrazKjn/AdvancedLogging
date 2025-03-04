using AdvancedLogging.Models.SystemStatus;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for managing system status and operations.
    /// </summary>
    public interface ISystemStatus
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        string ClientName { get; set; }

        /// <summary>
        /// Retrieves the tenant name.
        /// </summary>
        /// <returns>The tenant name.</returns>
        string GetTenantName();

        /// <summary>
        /// Retrieves the service controller for the current service.
        /// </summary>
        /// <returns>The service controller.</returns>
        ServiceController GetMyServiceController();

        /// <summary>
        /// Retrieves the name of the current service.
        /// </summary>
        /// <returns>The service name.</returns>
        string GetMyServiceName();

        /// <summary>
        /// Retrieves the name of the client.
        /// </summary>
        /// <returns>The client name.</returns>
        string GetClientName();

        /// <summary>
        /// Retrieves database information.
        /// </summary>
        /// <param name="ServiceName">The name of the service.</param>
        /// <param name="ClientName">The name of the client.</param>
        /// <param name="ServiceAssembly">The service assembly.</param>
        /// <param name="ConnectionString">The connection string.</param>
        /// <param name="DatabaseState">The state of the database.</param>
        /// <param name="OutDatabase">The output database information.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool GetDatabaseInfo(string ServiceName, string ClientName, Assembly ServiceAssembly, string ConnectionString, DatabaseInfo.DbState DatabaseState, out DatabaseInfo OutDatabase);

        /// <summary>
        /// Processes a custom service command.
        /// </summary>
        /// <param name="command">The command to process.</param>
        /// <param name="SqlConnections">The SQL connections.</param>
        /// <param name="SqlDatabaseTypes">The SQL database types.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool ProcessCustomServiceCommand(int command, string[] SqlConnections, List<DatabaseInfo.DbType> SqlDatabaseTypes, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Initializes registry constants.
        /// </summary>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool InitializeRegistryConstants(Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Retrieves registry constants.
        /// </summary>
        /// <param name="dblMinRefreshTime">The minimum refresh time.</param>
        /// <param name="dblTimeout">The timeout value.</param>
        /// <param name="bForce">Whether to force the operation.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool GetRegistryConstants(out double dblMinRefreshTime, out double dblTimeout, out bool bForce, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Determines whether a request can be sent.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if the request can be sent, otherwise false.</returns>
        bool CanSendRequest(string strServiceName, string strClientName, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Determines whether writing is allowed.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if writing is allowed, otherwise false.</returns>
        bool CanWrite(string strServiceName, string strClientName, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Sends a request.
        /// </summary>
        /// <param name="scThis">The current service controller.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool SendRequest(ICurrentServiceController scThis, string strClientName, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Sets the status.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="clientDBs">The client databases.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool SetStatus(string strServiceName, string strClientName, List<DatabaseInfo> clientDBs, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Clears the status.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool ClearStatus(string strServiceName, string strClientName, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Retrieves the status.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="strDatabase">The database name.</param>
        /// <param name="strConnectionString">The connection string.</param>
        /// <param name="strVersionInfoDB">The version information of the database.</param>
        /// <param name="strStatus">The status.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool GetStatus(string strServiceName, string strClientName, out string strDatabase, out string strConnectionString, out string strVersionInfoDB, out string strStatus, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Retrieves the status.
        /// </summary>
        /// <param name="strServiceName">The service name.</param>
        /// <param name="strClientName">The client name.</param>
        /// <param name="dbiDatabases">The list of database information.</param>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool GetStatus(string strServiceName, string strClientName, out List<DatabaseInfo> dbiDatabases, Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Verifies registry access.
        /// </summary>
        /// <param name="CodeTest">The code test mode.</param>
        /// <returns>True if successful, otherwise false.</returns>
        bool VerifyRegistryAccess(Constants.SystemStatus.CodeTest CodeTest = Constants.SystemStatus.CodeTest.Off);

        /// <summary>
        /// Compares two versions.
        /// </summary>
        /// <param name="Version1">The first version.</param>
        /// <param name="Version2">The second version.</param>
        /// <returns>An integer indicating the comparison result.</returns>
        int CompareVersion(string Version1, string Version2);

        /// <summary>
        /// Masks the connection string.
        /// </summary>
        /// <param name="strConnectionString">The connection string.</param>
        /// <returns>The masked connection string.</returns>
        string MaskConnectionString(string strConnectionString);
    }
}