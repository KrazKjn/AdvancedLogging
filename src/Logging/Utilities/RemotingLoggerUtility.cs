using AdvancedLogging.Extensions;
using AdvancedLogging.Loggers;
using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Models;
using log4net;
using log4net.Appender;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AdvancedLogging.Utilities
{
    public class RemotingLoggerUtility : LoggerUtility
    {
        private readonly int m_intMinutesAfterMidnight = 60;
        private readonly bool m_bAllowAutoCleanUp = true;
        private readonly bool m_bAutoCleanUpLogFiles = true;
        private bool m_bRemotingLogging = false;
        private System.Timers.Timer m_tmrDateChange = null;
        private readonly LoggerConfig m_LoggerInitialization = null;

        /// <summary>
        /// LoggerUtility instance
        /// </summary>
        /// <param name="_logger">Logger where should write</param>
        /// <param name="_directoryManager"> The log directory.</param>
        public RemotingLoggerUtility(ICommonLogger _logger, IDirectoryManager _directoryManager = null, ICommonLogger _loggerLocalRunFile = null, ICommonLogger _loggerLocalErrorFile = null) : base(_logger)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _logger, _directoryManager, _loggerLocalRunFile, _loggerLocalErrorFile }))
            {
                try
                {
                    m_LoggerInitialization = new LoggerConfig();
                    MyLogger = _logger;
                    if (_directoryManager == null)
                        MyDirectoryManager = new DirectoryManager();
                    else
                        MyDirectoryManager = _directoryManager;
                    if (_loggerLocalRunFile != null)
                        MyLoggerLocalRunFile = _loggerLocalRunFile;
                    if (_loggerLocalErrorFile != null)
                        MyLoggerLocalErrorFile = _loggerLocalErrorFile;
                    InitializeLogMonitor();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _logger, _directoryManager, _loggerLocalRunFile, _loggerLocalErrorFile }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// LoggerUtility instance
        /// </summary>
        /// <param name="_loggerConfig">Logger where should write</param>
        public RemotingLoggerUtility(LoggerConfig _loggerConfig, string logName = "Remoting", bool bAllowAutoCleanUp = true) : base(new Log4NetLogger(logName))
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _loggerConfig, bAllowAutoCleanUp }))
            {
                try
                {
                    m_bAllowAutoCleanUp = bAllowAutoCleanUp;
                    m_LoggerInitialization = _loggerConfig;
                    MyLogger = (ICommonLogger)_loggerConfig.MyLogger;
                    if (MyDirectoryManager == null)
                        MyDirectoryManager = new DirectoryManager();
                    else
                        MyDirectoryManager = MyDirectoryManager;
                    if (_loggerConfig.MyLoggerLocalRunFile != null)
                        MyLoggerLocalRunFile = (ICommonLogger)_loggerConfig.MyLoggerLocalRunFile;
                    if (_loggerConfig.MyLoggerLocalErrorFile != null)
                        MyLoggerLocalErrorFile = (ICommonLogger)_loggerConfig.MyLoggerLocalErrorFile;
                    InitializeLogMonitor();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _loggerConfig, bAllowAutoCleanUp }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Enable or Disable Auto Clean up of rolling appender log files honoring the MaxSizeRollBackups parameter.
        /// </summary>
        public override bool AutoCleanUpLogFiles
        {
            get { return base.AutoCleanUpLogFiles; }
            set
            {
                if (m_bAllowAutoCleanUp)
                {
                    using (ThreadContext.Stacks["NDC"].Push("Main"))
                    {
                        if (m_bRemotingLogging)
                        {
                            StackFrame sf = new StackFrame(0);

                            ThreadContext.Properties["procnamefull"] = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "." + sf.GetMethod().Name;
                            ThreadContext.Properties["procname"] = sf.GetMethod().Name;
                        }
                        base.AutoCleanUpLogFiles = value;
                    }
                }
                else
                    base.AutoCleanUpLogFiles = false;
            }
        }
        /// <summary>
        /// Get or set the minutes after midnight when the auto clean up is executed.
        /// </summary>
        public override int MinutesAfterMidnight
        {
            get { return base.MinutesAfterMidnight; }
            set
            {
                using (ThreadContext.Stacks["NDC"].Push("Main"))
                {
                    if (m_bRemotingLogging)
                    {
                        StackFrame sf = new StackFrame(0);

                        ThreadContext.Properties["procnamefull"] = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "." + sf.GetMethod().Name;
                        ThreadContext.Properties["procname"] = sf.GetMethod().Name;
                    }
                    base.MinutesAfterMidnight = value;
                }
            }
        }
        /// <summary>
        /// Get or set the days interval when the auto clean up is executed
        /// </summary>
        public override int DaysInterval
        {
            get { return base.DaysInterval; }
            set
            {
                using (ThreadContext.Stacks["NDC"].Push("Main"))
                {
                    if (m_bRemotingLogging)
                    {
                        StackFrame sf = new StackFrame(0);

                        ThreadContext.Properties["procnamefull"] = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + "." + sf.GetMethod().Name;
                        ThreadContext.Properties["procname"] = sf.GetMethod().Name;
                    }
                    base.DaysInterval = value;
                }
            }
        }
        /// <summary>
        /// Get or set the Logger referenced in the LoggerUtility functions.
        /// </summary>
        public override ICommonLogger MyLogger
        {
            get { return base.MyLogger; }
            set
            {
                base.MyLogger = value;
                m_bRemotingLogging = m_LoggerInitialization.MyLogger.IsRemoting;
            }
        }
        /// <summary>
        /// Get or set the Logger referenced in the LoggerUtility functions.
        /// </summary>
        public ICommonLogger MyLoggerLocalErrorFile
        {
            get { return (ICommonLogger)m_LoggerInitialization.MyLoggerLocalErrorFile; }
            set { m_LoggerInitialization.MyLoggerLocalErrorFile = value; }
        }

        /// <summary>
        /// Get or set the Logger referenced in the LoggerUtility functions.
        /// </summary>
        public ICommonLogger MyLoggerLocalRunFile
        {
            get { return (ICommonLogger)m_LoggerInitialization.MyLoggerLocalRunFile; }
            set { m_LoggerInitialization.MyLoggerLocalRunFile = value; }
        }

        #region - Methods -
        /// <summary>
        /// Startup a timer and register a callback
        /// </summary>
        public override void InitializeLogMonitor()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    if (m_LoggerInitialization != null)
                    {
                        log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
                        log4net.GlobalContext.Properties["pname"] = m_LoggerInitialization.ProcessName;
                        log4net.GlobalContext.Properties["instancename"] = m_LoggerInitialization.ClientName;
                        SetDateChangeTimer(false);
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
        /// Event handler for the daily log files clean up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TmrDateChange_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sender, e }))
            {
                try
                {
                    if (!m_tmrDateChange.Enabled)
                        return;
                    m_tmrDateChange.Enabled = false;
                    m_tmrDateChange.Enabled = false;
                    //CleanUp Logs based on config settings
                    vAutoLogFunction.WriteLog("Cleaning Logs ...");
                    CleanUp();
                    SetDateChangeTimer();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sender, e }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Set all the values to enabled the Timer
        /// </summary>
        private void SetDateChangeTimer(bool bLogItems = true)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { bLogItems }))
            {
                try
                {
                    if (m_tmrDateChange == null)
                    {
                        m_tmrDateChange = new System.Timers.Timer();
                        m_tmrDateChange.Elapsed += TmrDateChange_Elapsed;
                    }
                    if (m_tmrDateChange.Enabled)
                    {
                        m_tmrDateChange.Enabled = false;
                    }
                    DateTime dtNext = DateTime.Now.Date.AddDays(DaysInterval);
                    dtNext = dtNext.AddMinutes(m_intMinutesAfterMidnight);
                    m_tmrDateChange.Interval = dtNext.Subtract(DateTime.Now).TotalMilliseconds;
                    m_tmrDateChange.Enabled = m_bAllowAutoCleanUp && m_bAutoCleanUpLogFiles;
                    if (bLogItems)
                    {
                        if (m_tmrDateChange.Enabled)
                        {
                            vAutoLogFunction.WriteLog("AutoCleanUp scheduled at " + dtNext.ToShortDateString() + " " + dtNext.ToShortTimeString());
                        }
                        else
                        {
                            vAutoLogFunction.WriteLog("AutoCleanUp is NOT scheduled!");
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { bLogItems }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Cleans up. Auto configures the cleanup based on the log4net configuration
        /// http://logging.apache.org/log4net/release/sdk/html/T_log4net_Appender_RollingFileAppender.htm
        /// </summary>
        public override void CleanUp()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                string directory = string.Empty;
                string filePrefix = string.Empty;
                int intMaxSizeRollBackups = 365;

                try
                {
                    var repo = LogManager.GetAllRepositories().FirstOrDefault() ?? throw new NotSupportedException("Log4Net has not been configured yet.");
                    foreach (var vRepo in LogManager.GetAllRepositories())
                    {
                        if (vRepo != null)
                        {
                            vAutoLogFunction.WriteDebug("Processing repo: " + vRepo.Name + " ...");
                            foreach (var app in vRepo.GetAppenders().Where(x => x.GetType() == typeof(RollingFileAppender)))
                            {
                                if (app != null)
                                {
                                    var appender = app as RollingFileAppender;
                                    vAutoLogFunction.WriteDebug("Processing appender: " + app.Name + " ...");
                                    directory = Path.GetDirectoryName(appender.File);
                                    filePrefix = Path.GetFileName(appender.File);
                                    filePrefix = filePrefix.Substring(0, filePrefix.Substring(0, filePrefix.LastIndexOf('.')).LastIndexOf('.'));
                                    intMaxSizeRollBackups = appender.MaxSizeRollBackups;

                                    vAutoLogFunction.WriteDebug("appender.MaxSizeRollBackups: " + appender.MaxSizeRollBackups);
                                    vAutoLogFunction.WriteDebug("appender.AppendToFile: " + appender.AppendToFile);
                                    vAutoLogFunction.WriteDebug("appender.DatePattern: " + appender.DatePattern);
                                    vAutoLogFunction.WriteDebug("appender.RollingStyle: " + appender.RollingStyle);

                                    //do Cleanup only if certain configuration is set
                                    if (!String.IsNullOrEmpty(appender.DatePattern) &&
                                        appender.RollingStyle == RollingFileAppender.RollingMode.Date)
                                    {
                                        var date = DateTime.Now.AddDays(-intMaxSizeRollBackups);
                                        vAutoLogFunction.WriteDebug("CleanUp Started.");
                                        vAutoLogFunction.WriteDebug("directory: " + directory);
                                        vAutoLogFunction.WriteDebug("filePrefix: " + filePrefix);
                                        vAutoLogFunction.WriteDebug("date: " + date);
                                        CleanUp(directory, filePrefix, date);
                                    }
                                    else
                                    {
                                        vAutoLogFunction.WriteDebug("CleanUp Skipped.");
                                        vAutoLogFunction.WriteDebug("directory: " + directory);
                                        vAutoLogFunction.WriteDebug("filePrefix: " + filePrefix);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    vAutoLogFunction.WriteError("Error: " + ex.Message);
                    throw new ApplicationException("Log CleanUp is not valid!" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Cleans up.
        /// </summary>
        /// <param name="logDirectory">The log directory.</param>
        /// <param name="logPrefix">The log prefix. Example: logfile dont include the file extension.</param>
        /// <param name="date">Anything prior will not be kept.</param>
        public override void CleanUp(string logDirectory, string logPrefix, DateTime date)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logDirectory, logPrefix, date }))
            {
                try
                {
                    if (string.IsNullOrEmpty(logDirectory))
                        throw new ArgumentException("logDirectory is missing");

                    if (string.IsNullOrEmpty(logPrefix))
                        throw new ArgumentException("logPrefix is missing");

                    var dirInfo = MyDirectoryManager.GetDirectoryInfo(logDirectory);
                    if (!dirInfo.Exists)
                    {
                        vAutoLogFunction.WriteError("Directory doesn't exist: " + dirInfo.FullName);
                        return;
                    }

                    var fileInfos = MyDirectoryManager.GetDirFiles(dirInfo, "{0}*.*".Sub(logPrefix));
                    if (fileInfos.Length == 0)
                    {
                        vAutoLogFunction.WriteDebug("No Files found");
                        return;
                    }

                    foreach (var file in fileInfos)
                    {
                        if (file.CreationTime.Date <= date.Date)
                        {
                            try
                            {
                                vAutoLogFunction.WriteDebug("Deleting: " + file.FullName + " ...");
                                file.Delete();
                                vAutoLogFunction.WriteLog("Deleted: " + file.FullName);
                            }
                            catch (Exception ex)
                            {
                                vAutoLogFunction.WriteLog("Error deleting: " + file.FullName);
                                vAutoLogFunction.WriteError(ex.Message);
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebug("Skipping: " + file.FullName + " (Not old enough yet)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddToErrorFile("Log CleanUp is not valid! => " + " " + ex.Message + "\n" + ex.StackTrace);
                    vAutoLogFunction.WriteErrorFormat("Log CleanUp is not valid! => {0} \n {1}", ex.Message, ex.StackTrace);
                    throw new ApplicationException("Log CleanUp is not valid!" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Log a message object with the Info level in ErrorFile.
        /// </summary>
        /// <param name="contents"></param>
        public void AddToErrorFile(string contents)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { contents }))
            {
                try
                {
                    MyLoggerLocalErrorFile?.Info(contents);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { contents }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        /// <summary>
        /// Log a message object with the Info level in LoggerRunFile.
        /// </summary>
        /// <param name="contents"></param>
        public void AddToLoggerRunFile(string contents)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { contents }))
            {
                try
                {
                    MyLoggerLocalRunFile?.Info(contents);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { contents }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        ///// <summary>
        ///// Get the log File Path
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        public string GetLogFileName(string name)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name }))
            {
                try
                {
                    String file = "";
                    var repo = LogManager.GetAllRepositories().FirstOrDefault() ?? throw new NotSupportedException("Log4Net has not been configured yet.");
                    foreach (var app in repo.GetAppenders().Where(x => x.Name == name))
                    {
                        if (app != null)
                        {
                            var appender = app as RollingFileAppender;

                            String directory = Path.GetDirectoryName(appender.File);
                            String filePrefix = Path.GetFileName(appender.File);
                            file = Path.Combine(directory, filePrefix);
                        }
                    }
                    return repo != null ? file : string.Empty;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete the local Log Files
        /// </summary>
        public void DeleteLogFile(string name)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name }))
            {
                try
                {
                    string filePathFile = GetLogFileName(name);
                    if (File.Exists(filePathFile))
                    {
                        File.Delete(filePathFile);
                    }
                }
                catch (Exception ex)
                {
                    AddToErrorFile("DeleteLogFile => " + " " + ex.Message + "\n" + ex.StackTrace);
                    MyLogger.ErrorFormat("DeleteLogFile => {0} \n {1}", ex.Message, ex.StackTrace);
                }
            }
        }
        #endregion
    }
}