namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents information about a database.
    /// </summary>
    public class DatabaseInfo
    {
        /// <summary>
        /// Specifies the type of the database.
        /// TODO: Add your own Applications here
        /// </summary>
        public enum DbType
        {
            Application01,
            Application02,
            Application03
        }

        /// <summary>
        /// Specifies the state of the database.
        /// </summary>
        public enum DbState
        {
            InActive,
            Active
        }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the database.
        /// </summary>
        public DbType DatabaseType { get; set; }

        /// <summary>
        /// Gets or sets the state of the database.
        /// </summary>
        public DbState DatabaseState { get; set; }

        /// <summary>
        /// Gets or sets the connection string for the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the version information of the database.
        /// </summary>
        public string VersionInfo { get; set; }
    }
}
