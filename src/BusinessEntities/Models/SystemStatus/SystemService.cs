using System.Collections.Generic;

namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents a system service with various properties such as service ID, name, status, and related databases.
    /// </summary>
    public class SystemService
    {
        /// <summary>
        /// Gets or sets the unique identifier for the service.
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of the service.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the executable path of the service.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the service executable.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets the logon account used by the service.
        /// </summary>
        public string LogOnAccount { get; set; }

        /// <summary>
        /// Gets or sets the client instance associated with the service.
        /// </summary>
        public Client ClientInstance { get; set; }

        /// <summary>
        /// Gets or sets the current status of the service.
        /// </summary>
        public System.ServiceProcess.ServiceControllerStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the start mode of the service.
        /// </summary>
        public System.ServiceProcess.ServiceStartMode StartMode { get; set; }

        /// <summary>
        /// Gets or sets the version information of the service.
        /// </summary>
        public string VersionInfo { get; set; }

        /// <summary>
        /// Gets or sets the list of databases associated with the service.
        /// </summary>
        public List<DatabaseInfo> DataBases { get; set; }
    }
}
