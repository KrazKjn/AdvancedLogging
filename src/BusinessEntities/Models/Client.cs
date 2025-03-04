namespace AdvancedLogging.Models
{
    /// <summary>
    /// Represents a client with a name, active status, and an optional description.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets an optional description about the client. This can be null, empty, or missing.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="clientName">The name of the client.</param>
        /// <param name="isActive">A value indicating whether the client is active.</param>
        /// <param name="description">An optional description about the client.</param>
        public Client(string clientName, bool isActive, string description = null)
        {
            ClientName = clientName;
            IsActive = isActive;
            Description = description;
        }
    }
}