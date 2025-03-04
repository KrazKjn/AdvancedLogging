using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using Microsoft.Win32;
using System;
using System.Security.AccessControl;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Represents a wrapper around the Windows RegistryKey with additional logging functionality.
    /// </summary>
    public class CurrentRegistryKey : IRegistryKey, IDisposable
    {
        private readonly RegistryKey _registryKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentRegistryKey"/> class.
        /// </summary>
        public CurrentRegistryKey() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentRegistryKey"/> class with the specified registry key.
        /// </summary>
        /// <param name="registryKey">The registry key to wrap.</param>
        public CurrentRegistryKey(RegistryKey registryKey)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { registryKey }))
            {
                try
                {
                    _registryKey = registryKey ?? throw new ArgumentNullException(nameof(registryKey));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { registryKey }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a new subkey or opens an existing subkey.
        /// </summary>
        /// <param name="name">The name or path of the subkey to create or open.</param>
        /// <param name="permissionCheck">The access control to apply to the subkey.</param>
        /// <returns>A <see cref="IRegistryKey"/> representing the subkey.</returns>
        public IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck }))
            {
                try
                {
                    var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(name, permissionCheck);
                    return new CurrentRegistryKey(registryKey);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a new subkey or opens an existing subkey with specified security.
        /// </summary>
        /// <param name="name">The name or path of the subkey to create or open.</param>
        /// <param name="permissionCheck">The access control to apply to the subkey.</param>
        /// <param name="registrySecurity">The security to apply to the subkey.</param>
        /// <returns>A <see cref="IRegistryKey"/> representing the subkey.</returns>
        public IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity registrySecurity)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck, registrySecurity }))
            {
                try
                {
                    var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(name, permissionCheck, registrySecurity);
                    return new CurrentRegistryKey(registryKey);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck, registrySecurity }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Opens a subkey with the specified access control.
        /// </summary>
        /// <param name="name">The name or path of the subkey to open.</param>
        /// <param name="permissionCheck">The access control to apply to the subkey.</param>
        /// <returns>A <see cref="IRegistryKey"/> representing the subkey.</returns>
        public IRegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck }))
            {
                try
                {
                    var tempRegistryKey = Registry.LocalMachine.OpenSubKey(name, permissionCheck);
                    return new CurrentRegistryKey(tempRegistryKey);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets the specified value in the registry key.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(string name, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    _registryKey.SetValue(name, value);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets the specified value in the registry key.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(string name, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    _registryKey.SetValue(name, value);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets the specified value in the registry key with the specified value kind.
        /// </summary>
        /// <param name="name">The name of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="registryValueKind">The registry data type to use when storing the data.</param>
        public void SetValue(string name, double value, RegistryValueKind registryValueKind)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value, registryValueKind }))
            {
                try
                {
                    _registryKey.SetValue(name, value, registryValueKind);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value, registryValueKind }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the specified value from the registry key.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <param name="value">The default value to return if the value does not exist.</param>
        /// <returns>The value associated with the specified name, or the default value if the name is not found.</returns>
        public string GetValue(string name, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    return (string)_registryKey.GetValue(name, value);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the specified value from the registry key.
        /// </summary>
        /// <param name="name">The name of the value to retrieve.</param>
        /// <param name="value">The default value to return if the value does not exist.</param>
        /// <returns>The value associated with the specified name, or the default value if the name is not found.</returns>
        public double GetValue(string name, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    double DoubleValue = value;
                    var ObjectValue = _registryKey.GetValue(name, value);

                    if (ObjectValue is Int32)
                    {
                        DoubleValue = Convert.ToDouble(ObjectValue);
                    }
                    else if (ObjectValue is string strValue)
                    {
                        if (!Double.TryParse(strValue, out DoubleValue))
                        {
                            DoubleValue = value;
                        }
                    }
                    return DoubleValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Deletes the specified value from the registry key.
        /// </summary>
        /// <param name="name">The name of the value to delete.</param>
        /// <param name="throwOnMissingValue">true to throw an exception if the value does not exist; otherwise, false.</param>
        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, throwOnMissingValue }))
            {
                try
                {
                    _registryKey.DeleteValue(name, throwOnMissingValue);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, throwOnMissingValue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the access control security for the registry key.
        /// </summary>
        /// <returns>A <see cref="RegistrySecurity"/> object that describes the access control permissions on the registry key.</returns>
        public RegistrySecurity GetAccessControl()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    return _registryKey.GetAccessControl();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Closes the registry key.
        /// </summary>
        public void Close()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    _registryKey?.Close();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="CurrentRegistryKey"/>.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}