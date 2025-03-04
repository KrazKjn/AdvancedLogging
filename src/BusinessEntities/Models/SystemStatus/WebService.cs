using System.Collections.Generic;

namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents a web service in the system.
    /// </summary>
    public class WebService
    {
        /// <summary>
        /// Specifies the type of the component.
        /// </summary>
        public enum ComponentTypeClass
        {
            /// <summary>
            /// Represents a web service.
            /// </summary>
            WebService,
            /// <summary>
            /// Represents a web application.
            /// </summary>
            WebApplication
        }

        /// <summary>
        /// Gets or sets the unique identifier for the web service.
        /// </summary>
        public int WebServiceId { get; set; }

        /// <summary>
        /// Gets or sets the name of the web service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the web service.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Gets or sets the type of the component.
        /// </summary>
        public ComponentTypeClass ItemType { get; set; }

        /// <summary>
        /// Gets or sets the client instance associated with the web service.
        /// </summary>
        public Client ClientInstance { get; set; }

        /// <summary>
        /// Gets or sets the version information of the web service.
        /// </summary>
        public string VersionInfo { get; set; }

        /// <summary>
        /// Gets or sets the list of databases associated with the web service.
        /// </summary>
        public List<DatabaseInfo> DataBases { get; set; }
    }
}
