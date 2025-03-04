using System.Collections.Generic;

namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents a settings file with its ID, name, and connection string paths.
    /// </summary>
    public class SettingsFile
    {
        /// <summary>
        /// Gets or sets the unique identifier for the settings file.
        /// </summary>
        public int FileId { get; set; }

        /// <summary>
        /// Gets or sets the name of the settings file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the paths to the connection strings.
        /// </summary>
        public List<string> ConnectionStringPath { get; set; }
    }
}