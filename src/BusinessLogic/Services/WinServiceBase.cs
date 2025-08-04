using AdvancedLogging.Enumerations;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using AdvancedLogging.Models.SystemStatus;
using AdvancedLogging.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.ServiceProcess;

namespace AdvancedLogging.BLL.Services
{
    /// <summary>
    /// Base class for Windows services providing logging and system status functionalities.
    /// </summary>
    public class WinServiceBase : ServiceBase
    {
        /// <summary>
        /// Gets or sets the system status.
        /// </summary>
        public ISystemStatus SystemStatus { get; set; }

        /// <summary>
        /// Gets or sets the common logger.
        /// </summary>
        public ICommonLogger Log { get; set; }

        /// <summary>
        /// Gets or sets the logging context.
        /// </summary>
        public ILoggingContext LoggingContext { get; set; }

        /// <summary>
        /// Gets or sets the connection strings.
        /// </summary>
        public string[] ConnectionStrings { get; set; }

        public List<DatabaseInfo.DbType> DatabaseTypes { get; set; }

        private ILoggerUtility m_loggerUtility;

        /// <summary>
        /// Gets or sets the logger utility.
        /// </summary>
        public ILoggerUtility LoggerUtility
        {
            get
            {
                if (m_loggerUtility == null)
                {
                    m_loggerUtility = new LoggerUtility(Log);
                }
                return m_loggerUtility;
            }
            set
            {
                if (m_loggerUtility != null)
                {
                    m_loggerUtility.MyLogger?.Info("Resetting Existing Logger");
                    m_loggerUtility = null;
                }
                m_loggerUtility = value;
                m_loggerUtility.MyLogger?.Info("Logger Assigned by Code");
            }
        }

        /// <summary>
        /// Gets or sets the service assembly.
        /// </summary>
        public Assembly Service { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public System.Configuration.Configuration Config { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WinServiceBase"/> class.
        /// </summary>
        public WinServiceBase(ICommonLogger logger, ILoggingContext loggingContext)
        {
            Log = logger;
            LoggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { }))
            {
                try
                {
                    PreInitialize();
                    Initialize();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WinServiceBase"/> class with specified parameters.
        /// </summary>
        public WinServiceBase(ISystemStatus systemStatus, ICommonLogger log, ILoggingContext loggingContext, string[] connectionStrings, List<DatabaseInfo.DbType> databaseTypes)
        {
            Log = log;
            LoggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { systemStatus, log, connectionStrings, databaseTypes }))
            {
                try
                {
                    PreInitialize();
                    SystemStatus = systemStatus;
                    ConnectionStrings = connectionStrings;
                    DatabaseTypes = databaseTypes;
                    Initialize();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { systemStatus, log, connectionStrings, databaseTypes }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WinServiceBase"/> class with specified parameters.
        /// </summary>
        public WinServiceBase(ISystemStatus systemStatus, ICommonLogger log, ILoggingContext loggingContext, ILoggerUtility loggerUtility, string[] connectionStrings, List<DatabaseInfo.DbType> databaseTypes)
        {
            Log = log;
            LoggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { systemStatus, log, loggerUtility, connectionStrings, databaseTypes }))
            {
                try
                {
                    PreInitialize();
                    SystemStatus = systemStatus;
                    LoggerUtility = loggerUtility;
                    ConnectionStrings = connectionStrings;
                    DatabaseTypes = databaseTypes;
                    Initialize();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { systemStatus, log, loggerUtility, connectionStrings, databaseTypes }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a custom command in a testable manner.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void OnCustomCommandTestable(int command)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { command }))
            {
                try
                {
                    OnCustomCommand(command);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { command }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Handles custom commands.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        protected override void OnCustomCommand(int command)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { command }))
            {
                try
                {
                    if (string.IsNullOrEmpty(SystemStatus.ServiceName) || string.IsNullOrEmpty(SystemStatus.ClientName))
                    {
                        if (string.IsNullOrEmpty(SystemStatus.ServiceName))
                        {
                            vAutoLogFunction.WriteError("No Active Service!");
                        }
                        if (string.IsNullOrEmpty(SystemStatus.ClientName))
                        {
                            vAutoLogFunction.WriteError("No Active Client!");
                        }
                    }
                    else
                    {
                        vAutoLogFunction.WriteLogFormat(Constants.SystemStatus.LogFormat.General, "Processing Custom Command: " + SystemStatus.ClientName + " - " + SystemStatus.ServiceName);
                        SystemStatus.ProcessCustomServiceCommand(command, ConnectionStrings, DatabaseTypes);
                    }
                }
                catch (Exception ex)
                {
                    vAutoLogFunction.WriteErrorFormat(Constants.SystemStatus.LogFormat.Error, ex.Message, ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Pre-initializes the service.
        /// </summary>
        private void PreInitialize()
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { }))
            {
                try
                {
                    SecurityProtocol.AvailableSecurityProtocols = SecurityProtocolTypeCustom.SystemDefault;
                    SecurityProtocol.EnableAllTlsSupport();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes the service.
        /// </summary>
        private void Initialize()
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { }))
            {
                try
                {
                    Service = Assembly.GetEntryAssembly();
                    if (Service != null)
                    {
                        Config = BusinessLogic.Configuration.OpenConfigurationFile(Log, LoggingContext, Service.Location);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes auto logging cleanup.
        /// </summary>
        /// <param name="bCleanLogs">Indicates whether to clean logs.</param>
        /// <returns>True if auto cleanup of log files is enabled, otherwise false.</returns>
        public bool InitializeAutoLoggingCleanUp(bool bCleanLogs = true)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, LoggingContext, new { bCleanLogs }))
            {
                try
                {
                    // Clean up logs based on config settings
                    try
                    {
                        m_loggerUtility = new LoggerUtility(Log, new DirectoryManager())
                        {
                            MinutesAfterMidnight = BusinessLogic.Configuration.GetConfigurationIntValue(Config, Log, LoggingContext, "MinutesAfterMidnight", 120),
                            DaysInterval = BusinessLogic.Configuration.GetConfigurationIntValue(Config, Log, LoggingContext, "DaysInterval", 1),
                            AutoCleanUpLogFiles = bool.Parse(BusinessLogic.Configuration.GetConfigurationStringValue(Config, Log, LoggingContext, "AutoCleanUpLogFiles", "true"))
                        };

                        if (bCleanLogs)
                        {
                            m_loggerUtility.CleanUp();
                        }
                        return m_loggerUtility.AutoCleanUpLogFiles;
                    }
                    catch (Exception ex)
                    {
                        Log?.ErrorFormat("Invalid parameter: " + ex.Message);
                    }
                    return false;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { bCleanLogs }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
