using Microsoft.Win32;
using System.Security.AccessControl;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for interacting with Windows Registry keys.
    /// </summary>
    public interface IRegistryKey
    {
        /// <summary>
        /// Creates a new subkey or opens an existing subkey.
        /// </summary>
        /// <param name="name">The name or path of the subkey to create or open.</param>
        /// <param name="permissionCheck">The access control security for the subkey.</param>
        /// <returns>An instance of <see cref="IRegistryKey"/> representing the subkey.</returns>
        IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck);

        /// <summary>
        /// Creates a new subkey or opens an existing subkey with specified security.
        /// </summary>
        /// <param name="name">The name or path of the subkey to create or open.</param>
        /// <param name="permissionCheck">The access control security for the subkey.</param>
        /// <param name="registrySecurity">The security settings for the subkey.</param>
        /// <returns>An instance of <see cref="IRegistryKey"/> representing the subkey.</returns>
        IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity registrySecurity);

        /// <summary>
        /// Opens a subkey with the specified access control security.
        /// </summary>
        /// <param name="name">The name or path of the subkey to open.</param>
        /// <param name="permissionCheck">The access control security for the subkey.</param>
        /// <returns>An instance of <see cref="IRegistryKey"/> representing the subkey.</returns>
        IRegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck);

        /// <summary>
        /// Sets the value of a name/value pair in the registry key.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(string name, string value);

        /// <summary>
        /// Sets the value of a name/value pair in the registry key.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(string name, double value);

        /// <summary>
        /// Sets the value of a name/value pair in the registry key with a specified registry data type.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="registryValueKind">The registry data type to use when storing the data.</param>
        void SetValue(string name, double value, RegistryValueKind registryValueKind);

        /// <summary>
        /// Gets the value associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <param name="value">The default value to return if the name does not exist.</param>
        /// <returns>The value associated with the specified name, or the default value if the name is not found.</returns>
        string GetValue(string name, string value);

        /// <summary>
        /// Gets the value associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <param name="value">The default value to return if the name does not exist.</param>
        /// <returns>The value associated with the specified name, or the default value if the name is not found.</returns>
        double GetValue(string name, double value);

        /// <summary>
        /// Deletes the specified value from the registry key.
        /// </summary>
        /// <param name="name">The name of the value to delete.</param>
        /// <param name="throwOnMissingValue">Indicates whether to throw an exception if the value does not exist.</param>
        void DeleteValue(string name, bool throwOnMissingValue);

        /// <summary>
        /// Retrieves the access control security for the registry key.
        /// </summary>
        /// <returns>A <see cref="RegistrySecurity"/> object that describes the access control security for the registry key.</returns>
        RegistrySecurity GetAccessControl();

        /// <summary>
        /// Closes the registry key and flushes it to disk if its contents have been modified.
        /// </summary>
        void Close();
    }
}