namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents the version information of a service.
    /// </summary>
    public class ServiceVersion
    {
        /// <summary>
        /// Gets or sets the unique identifier for the service version.
        /// </summary>
        public int ServiceVersionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the supported version of the service.
        /// </summary>
        public string SupportedVersion { get; set; }

        /// <summary>
        /// Gets or sets the settings files associated with the service version.
        /// </summary>
        public SettingsFile[] SettingsFiles { get; set; }
    }
}