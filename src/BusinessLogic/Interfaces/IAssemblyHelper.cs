namespace AdvancedLogging.Interfaces
{
    using System.Reflection;
    /// <summary>
    /// Provides methods to assist with assembly-related operations.
    /// </summary>
    public interface IAssemblyHelper
    {
        /// <summary>
        /// Gets the formatted version string of the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the version from.</param>
        /// <returns>A formatted version string of the assembly.</returns>
        string GetFormattedVersion(Assembly assembly);
    }
}
