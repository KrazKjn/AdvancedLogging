using System;

namespace AdvancedLogging.Models
{
    /// <summary>
    /// Represents the version information of the database.
    /// </summary>
    public class DatabaseVersionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseVersionInfo"/> class.
        /// </summary>
        public DatabaseVersionInfo()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseVersionInfo"/> class.
        /// </summary>
        /// <param name="databaseVersion">The version information retrieved from the DB.</param>
        /// <param name="databaseBuild">The build number for the database.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseVersion"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="databaseBuild"/> is less than zero.</exception>
        public DatabaseVersionInfo(string databaseVersion, int databaseBuild)
        {
            if (string.IsNullOrEmpty(databaseVersion))
            {
                throw new ArgumentNullException(nameof(databaseVersion), "Database version cannot be null or empty.");
            }

            if (databaseBuild < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(databaseBuild), "Database build number cannot be less than zero.");
            }

            DataBaseVersion = databaseVersion;
            DataBaseBuild = databaseBuild;
        }

        /// <summary>
        /// Gets or sets the version information retrieved from the DB.
        /// </summary>
        public string DataBaseVersion { get; set; }

        /// <summary>
        /// Gets or sets the build number for the database.
        /// </summary>
        public int DataBaseBuild { get; set; }
    }
}
