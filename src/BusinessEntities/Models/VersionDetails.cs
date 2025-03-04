namespace AdvancedLogging.Models
{
    /// <summary>
    /// Contains version details for software and database.
    /// </summary>
    public class VersionDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionDetails"/> class.
        /// </summary>
        public VersionDetails()
        {
            Software = string.Empty;
            Database = string.Empty;
        }

        /// <summary>
        /// Gets or sets the application version information.
        /// </summary>
        public string Software { get; set; }

        /// <summary>
        /// Gets or sets the database version information.
        /// </summary>
        public string Database { get; set; }
    }
}
