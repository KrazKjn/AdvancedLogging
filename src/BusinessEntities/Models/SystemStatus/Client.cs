namespace AdvancedLogging.Models.SystemStatus
{
    /// <summary>
    /// Represents a client in the system.
    /// </summary>
    public class Client
    {
        /// <summary>
        /// Gets or sets the unique identifier for the client.
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tenant name associated with the client.
        /// </summary>
        public string TenantName { get; set; }
    }
}