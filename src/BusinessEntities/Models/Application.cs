namespace AdvancedLogging.Models
{
    /// <summary>
    /// Represents an application with a name, active status, and an optional description.
    /// </summary>
    public class Application
    {
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the application. This can be null or empty.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        /// <param name="applicationName">The name of the application.</param>
        /// <param name="isActive">A value indicating whether the application is active.</param>
        /// <param name="description">An optional description of the application.</param>
        public Application(string applicationName, bool isActive, string description = null)
        {
            ApplicationName = applicationName;
            IsActive = isActive;
            Description = description;
        }
    }
}