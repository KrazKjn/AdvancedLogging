using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;

namespace AdvancedLogging.BusinessLogic
{
    /// <summary>
    /// Provides methods to control and retrieve information about a Windows service.
    /// </summary>
    public class CurrentServiceController : ICurrentServiceController
    {
        private readonly ServiceController _serviceController;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentServiceController"/> class.
        /// </summary>
        public CurrentServiceController() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentServiceController"/> class with the specified service controller.
        /// </summary>
        /// <param name="serviceController">The service controller to use.</param>
        public CurrentServiceController(ServiceController serviceController)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { serviceController }))
            {
                try
                {
                    _serviceController = serviceController;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { serviceController }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string ServiceName
        {
            get => _serviceController.ServiceName;
            set => _serviceController.ServiceName = value;
        }

        /// <summary>
        /// Gets the display name of the service.
        /// </summary>
        public string DisplayName
        {
            get => _serviceController.DisplayName;
            set => _serviceController.DisplayName = value;
        }

        /// <summary>
        /// Gets or sets the start type of the service.
        /// </summary>
        public ServiceStartMode StartType
        {
            get
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        return (ServiceStartMode)openedKey.GetValue("Start", 0);
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
            set
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        openedKey.SetValue("Start", value, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the status of the service.
        /// </summary>
        public ServiceControllerStatus Status => _serviceController.Status;

        /// <summary>
        /// Gets or sets the executable name of the service.
        /// </summary>
        public string ExecutableName
        {
            get
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        return (string)openedKey.GetValue("ImagePath", "");
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
            set
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        openedKey.SetValue("ImagePath", value);
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets or sets the logon account of the service.
        /// </summary>
        public string LogOnAccount
        {
            get
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        return (string)openedKey.GetValue("ObjectName", "");
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
            set
            {
                try
                {
                    using (var rkService = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                    using (var openedKey = rkService.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        openedKey.SetValue("ObjectName", value);
                    }
                }
                catch (Exception ex)
                {
                    if (ApplicationSettings.LogToDebugWindow)
                        Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets all current services.
        /// </summary>
        /// <returns>An array of <see cref="ICurrentServiceController"/> representing the current services.</returns>
        public ICurrentServiceController[] GetCurrentServices()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    var serviceControllers = new List<ICurrentServiceController>();
                    foreach (var currentController in ServiceController.GetServices())
                    {
                        serviceControllers.Add(new CurrentServiceController(currentController));
                    }
                    return serviceControllers.ToArray();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a command on the service.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void ExecuteCommand(int command)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { command }))
            {
                try
                {
                    _serviceController.ExecuteCommand(command);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { command }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }

}