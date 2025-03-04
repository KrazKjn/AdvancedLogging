using AdvancedLogging.Extensions;
using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using log4net;
using log4net.Appender;
using log4net.Core;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace AdvancedLogging.Utilities
{

    /// <summary>
    /// LoggerUtility provides various methods for changing log4net
    /// logging levels and thresholds on the fly without having to restart your
    /// application.
    /// 
    /// So far, it supports 3 methods for changing the logging levels:
    /// 1.  setRootLoggingLevel -- overrides all other settings
    /// 2.  setRepositoryThreshold -- overrides all appender settings
    /// 3.  setAppenderThreshold -- allows individual appender thresholds to be set
    /// 
    /// LoggerUtility also provides:
    /// LogCleanUp  functionality for cleaning up log4netold files
    /// based on maxSizeRollBackups parameter
    /// Reason:
    /// According to log4net documentation when => rollingStyle value="Date"
    /// maxSizeRollBackups does not work (it does  for "Size" and partially for "Composit" rollingStyle , though)
    /// We need to CleanUp based on Date (older files)
    /// http://logging.apache.org/log4net/release/sdk/html/T_log4net_Appender_RollingFileAppender.htm
    /// 
    /// It also supplies methods for getting the logging hierarchy, appender names,
    /// and information about the current logging configuration.
    /// </summary>
    public class LoggerUtility : ILoggerUtility
    {
        private ICommonLogger logger;
        private IDirectoryManager directoryManager;
        private int m_intMinutesAfterMidnight = 60;
        private int m_intDaysInterval = 1;
        private bool m_bAutoCleanUpLogFiles = true;
        private System.Timers.Timer m_tmrDateChange = null;

        /// <summary>
        /// Constructor
        /// </summary>

        public LoggerUtility(ICommonLogger commonLogger)
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    this.logger = commonLogger;
                    directoryManager = new DirectoryManager();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        /// <summary>
        /// LoggerUtility instance
        /// </summary>
        /// <param name="_logger">Logger where should write</param>
        /// <param name="_directoryManager"> The log directory.</param>
        public LoggerUtility(ICommonLogger _logger, IDirectoryManager _directoryManager)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _logger, _directoryManager }))
            {
                try
                {
                    logger = _logger;
                    directoryManager = _directoryManager;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _logger, _directoryManager }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        /// <summary>
        /// Enable or Disable Auto Clean up of rolling appender log files honoring the MaxSizeRollBackups parameter.
        /// </summary>
        public virtual bool AutoCleanUpLogFiles
        {
            get { return m_bAutoCleanUpLogFiles; }
            set
            {
                using (var vAutoLogFunction = new AutoLogFunction())
                {
                    m_bAutoCleanUpLogFiles = value;
                    vAutoLogFunction.WriteDebug("AutoCleanUpLogFiles = " + m_bAutoCleanUpLogFiles.ToString());
                    SetDateChangeTimer();
                }
            }
        }
        /// <summary>
        /// Get or set the minutes after midnight when the auto clean up is executed.
        /// </summary>
        public virtual int MinutesAfterMidnight
        {
            get { return m_intMinutesAfterMidnight; }
            set
            {
                using (var vAutoLogFunction = new AutoLogFunction())
                {
                    m_intMinutesAfterMidnight = value;
                    vAutoLogFunction.WriteDebug("MinutesAfterMidnight = " + m_intMinutesAfterMidnight.ToString());
                    SetDateChangeTimer();
                }
            }
        }
        /// <summary>
        /// Get or set the days interval when the auto clean up is executed
        /// </summary>
        public virtual int DaysInterval
        {
            get { return m_intDaysInterval; }
            set
            {
                using (var vAutoLogFunction = new AutoLogFunction())
                {
                    m_intDaysInterval = value;
                    vAutoLogFunction.WriteDebug("DaysInterval = " + m_intDaysInterval.ToString());
                    SetDateChangeTimer();
                }
            }
        }

        /// <summary>
        /// Get or set the Logger referenced in the LoggerUtility functions.
        /// </summary>
        public virtual ICommonLogger MyLogger
        {
            get { return logger; }
            set { logger = value; }
        }

        /// <summary>
        /// Get or set the DirectoryManager referenced in the LoggerUtility functions.
        /// </summary>
        public virtual IDirectoryManager MyDirectoryManager
        {
            get { return directoryManager; }
            set { directoryManager = value; }
        }
        #region - Methods -
        /// <summary>
        /// Startup a timer and register a callback
        /// </summary>
        public virtual void InitializeLogMonitor()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    SetDateChangeTimer(false);
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
        private void TimerDateChange_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sender, e }))
            {
                try
                {
                    if (!m_tmrDateChange.Enabled)
                        return;
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
                        m_tmrDateChange.Elapsed += TimerDateChange_Elapsed;
                    }
                    if (m_tmrDateChange.Enabled)
                    {
                        m_tmrDateChange.Enabled = false;
                    }
                    DateTime dtNext = DateTime.Now.Date.AddDays(DaysInterval);
                    dtNext = dtNext.AddMinutes(m_intMinutesAfterMidnight);
                    m_tmrDateChange.Interval = dtNext.Subtract(DateTime.Now).TotalMilliseconds;
                    m_tmrDateChange.Enabled = m_bAutoCleanUpLogFiles;
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
        public virtual void CleanUp()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
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
                                var varAppenders = vRepo.GetAppenders().Where(x => x.GetType() == typeof(RollingFileAppender));
                                foreach (var app in varAppenders)
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
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Cleans up.
        /// </summary>
        /// <param name="logDirectory">The log directory.</param>
        /// <param name="logPrefix">The log prefix. Example: logfile dont include the file extension.</param>
        /// <param name="date">Anything prior will not be kept.</param>
        public virtual void CleanUp(string logDirectory, string logPrefix, DateTime date)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logDirectory, logPrefix, date }))
            {
                try
                {
                    if (string.IsNullOrEmpty(logDirectory))
                        throw new ArgumentException("logDirectory is missing");

                    if (string.IsNullOrEmpty(logPrefix))
                        throw new ArgumentException("logPrefix is missing");

                    var dirInfo = directoryManager.GetDirectoryInfo(logDirectory);
                    if (!dirInfo.Exists)
                    {
                        vAutoLogFunction.WriteError("Directory doesn't exist: " + dirInfo.FullName);
                        return;
                    }

                    var fileInfos = directoryManager.GetDirFiles(dirInfo, "{0}*.*".Sub(logPrefix));
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
                    throw new ApplicationException("Log CleanUp is not valid!" + ex.Message);
                }
            }
        }

        /// <summary>
        /// Removes password from Connection String
        /// <param name="stringToClean">Connection String</param>
        public virtual string StringRemovePassword(string stringToClean)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { stringToClean }))
            {
                try
                {
                    if (!string.IsNullOrEmpty(stringToClean) && !stringToClean.Trim().StartsWith("#{"))
                    {
                        DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                        string[] secureFields = { "Pwd", "Password" };

                        builder.ConnectionString = stringToClean;
                        foreach (string secureField in secureFields)
                        {
                            if (builder.ContainsKey(secureField))
                                builder.Remove(secureField);
                        }
                        return builder.ConnectionString;
                    }
                    return "";
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { stringToClean }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringToClean"></param>
        /// <returns></returns>
        public static string StringRemovePasswordStatic(string stringToClean)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { stringToClean }))
            {
                try
                {
                    if (!string.IsNullOrEmpty(stringToClean) && !stringToClean.Trim().StartsWith("#{"))
                    {
                        DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                        string[] secureFields = { "Pwd", "Password" };

                        builder.ConnectionString = stringToClean;
                        foreach (string field in secureFields)
                        {
                            if (builder.ContainsKey(field))
                                builder.Remove(field);
                        }
                        return builder.ConnectionString;
                    }
                    return "";
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { stringToClean }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Masks password in Connection String
        /// </summary>
        /// <param name="str">Connection String</param>
        /// <param name="maskvalue">Default ********</param>
        /// <returns></returns>
        public static string StringMaskPassword(string name, string str, string maskvalue = "********")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { str, maskvalue }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    if (!string.IsNullOrEmpty(str) && !str.Trim().StartsWith("#{"))
                    {
                        if (vAutoLogFunction.Logger.IsConnectionString(name, str))
                        {
                            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                            string[] secureFields = { "Pwd", "Password" };

                            builder.ConnectionString = str;
                            foreach (string secureField in secureFields)
                            {
                                if (builder.ContainsKey(secureField))
                                    builder[secureField] = maskvalue;
                            }
                            return builder.ConnectionString;
                        }
                        else
                        {
                            return maskvalue;
                        }
                    }
                    return "";
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { str, maskvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Implement the translation from a string to a log4net level enumeration.
        /// </summary>
        /// <param name="thesholdString">String representation of the log 4 net logging level: "OFF";"FATAL";"ERROR";"WARN";"INFO";"DEBUG";"ALL"</param>
        /// <returns>log4net level enumeration</returns>
        //public virtual SharedLevel GetThresholdFromString(string thesholdString)
        //{
        //    Level level = GetThresholdFromString(thesholdString);
        //    SharedLevel log4netLvl;
        //    throw new NotImplementedException();
        //}

        Level ILoggerUtility.GetThresholdFromString(string thesholdString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { thesholdString }))
            {
                try
                {
                    Level log4netLvl;

                    switch (thesholdString)
                    {
                        case "ALL":
                            log4netLvl = log4net.Core.Level.All;
                            break;
                        case "DEBUG":
                            log4netLvl = log4net.Core.Level.Debug;
                            break;
                        case "INFO":
                            log4netLvl = log4net.Core.Level.Info;
                            break;
                        case "WARN":
                            log4netLvl = log4net.Core.Level.Warn;
                            break;
                        case "ERROR":
                            log4netLvl = log4net.Core.Level.Error;
                            break;
                        case "FATAL":
                            log4netLvl = log4net.Core.Level.Fatal;
                            break;
                        case "OFF":
                            log4netLvl = log4net.Core.Level.Off;
                            break;
                        default:
                            log4netLvl = log4net.Core.Level.Info;
                            break;
                    }

                    return log4netLvl;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { thesholdString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Toggle all appenders to this specified level 
        /// </summary>
        /// <param name="level"></param>
        public void ToggleLogging(log4net.Core.Level level)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { level }))
            {
                try
                {
                    log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
                    foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
                    {
                        try
                        {
                            log4net.Appender.AppenderSkeleton vAppender = (log4net.Appender.AppenderSkeleton)appender;
                            vAppender.Threshold = level;
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("Error Setting ThreshHold on {0}.  Error: {1}", appender.Name, ex.ToString());
                        }
                    }
                    //Configure the root logger.
                    log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
                    log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
                    rootLogger.Level = level;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { level }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Toggle all appenders to this specified level 
        /// </summary>
        /// <param name="level"></param>
        public static void ToggleLoggingStatic(log4net.Core.Level level)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { level }))
            {
                try
                {
                    log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
                    foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
                    {
                        try
                        {
                            log4net.Appender.AppenderSkeleton vAppender = (log4net.Appender.AppenderSkeleton)appender;
                            vAppender.Threshold = level;
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("Error Setting ThreshHold on {0}.  Error: {1}", appender.Name, ex.ToString());
                        }
                    }
                    //Configure the root logger.
                    log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
                    log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
                    rootLogger.Level = level;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { level }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Toggle all appenders to this specified level
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public log4net.Core.Level ToggleLogging(string logLevel)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logLevel }))
            {
                try
                {
                    log4net.Core.Level level = null;

                    if (!string.IsNullOrEmpty(logLevel))
                    {
                        level = log4net.LogManager.GetRepository().LevelMap[logLevel.ToUpper()];
                    }
                    if (level == null)
                    {
                        level = log4net.Core.Level.Info;
                    }
                    ToggleLogging(level);

                    return level;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { logLevel }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Toggle all appenders to this specified level
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public static log4net.Core.Level ToggleLoggingStatic(string logLevel)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logLevel }))
            {
                try
                {
                    log4net.Core.Level level = null;

                    if (!string.IsNullOrEmpty(logLevel))
                    {
                        level = log4net.LogManager.GetRepository().LevelMap[logLevel.ToUpper()];
                    }
                    if (level == null)
                    {
                        level = log4net.Core.Level.Info;
                    }
                    ToggleLoggingStatic(level);

                    return level;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { logLevel }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
    }
}