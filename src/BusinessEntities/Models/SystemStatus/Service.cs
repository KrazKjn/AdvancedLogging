namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents a service in the system.
    /// </summary>
    public class Service
    {
        /// <summary>
        /// Specifies the type of the service.
        /// </summary>
        public enum ServiceTypeClass
        {
            /// <summary>
            /// Represents a web service.
            /// </summary>
            WebService,
            /// <summary>
            /// Represents a web application.
            /// </summary>
            WebApplication,
            /// <summary>
            /// Represents a system service.
            /// </summary>
            SystemService
        }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the client associated with the service.
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        public ServiceTypeClass ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the system service details if the service is a system service.
        /// </summary>
        public SystemService SystemService { get; set; }

        /// <summary>
        /// Gets or sets the web service details if the service is a web service.
        /// </summary>
        public WebService WebService { get; set; }
    }
}