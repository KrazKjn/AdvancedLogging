using System.ServiceProcess;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for controlling and managing Windows services.
    /// </summary>
    public interface ICurrentServiceController
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the service.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the start type of the service.
        /// </summary>
        ServiceStartMode StartType { get; set; }

        /// <summary>
        /// Gets the current status of the service.
        /// </summary>
        ServiceControllerStatus Status { get; }

        /// <summary>
        /// Gets or sets the name of the executable associated with the service.
        /// </summary>
        string ExecutableName { get; set; }

        /// <summary>
        /// Gets or sets the account under which the service runs.
        /// </summary>
        string LogOnAccount { get; set; }

        /// <summary>
        /// Retrieves the current services managed by this controller.
        /// </summary>
        /// <returns>An array of <see cref="ICurrentServiceController"/> representing the current services.</returns>
        ICurrentServiceController[] GetCurrentServices();

        /// <summary>
        /// Executes a command on the service.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        void ExecuteCommand(int command);
    }
}
