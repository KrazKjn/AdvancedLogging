using AdvancedLogging.Constants;
using AdvancedLogging.Utilities;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Linq;

namespace AdvancedLogging.Loggers
{
    /// <summary>
    /// Log4NetLogger class that extends CommonLogger and implements ILog.
    /// </summary>
    public class Log4NetLogger : CommonLogger, log4net.ILog
    {
        #region Private fields
        private log4net.ILog _logger;
        private log4net.Core.Level _logLevel = log4net.Core.Level.Info;
        #endregion

        /// <summary>
        /// Constructor that initializes the logger with a name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public Log4NetLogger(string name) : base()
        {
            _logger = LogManager.GetLogger(name);
            LoggingUtils.Logger = this;
        }

        /// <summary>
        /// Constructor that initializes the logger with a type.
        /// </summary>
        /// <param name="type">The type of the logger.</param>
        public Log4NetLogger(Type type) : base()
        {
            _logger = LogManager.GetLogger(type);
            LoggingUtils.Logger = this;
        }

        #region Implementation of ILoggerWrapper

        /// <summary>
        /// Gets or sets the logging level.
        /// </summary>
        public override SharedLevel Level
        {
            get
            {
                if (_logLevel == null)
                    _logLevel = log4net.Core.Level.Info;
                return LogLevelShared;
            }
            set
            {
                _logLevel = new Level(value.Value, value.Name);
                ToggleLoggingPrivate(_logLevel);
            }
        }
        #endregion

        /// <summary>
        /// Gets the underlying logger.
        /// </summary>
        public log4net.ILog Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }
        public override bool IsRemoting => Logger is RemoteSyslogAppender;
        public override bool IsLoggingToConsole => HasAppender<ConsoleAppender>(_logger);
        public override bool IsLoggingToDebugWindow => HasAppender<DebugAppender>(_logger);

        ILogger ILoggerWrapper.Logger => _logger.Logger;

        #region Implementation of ILog

        #region Implementation of Debug
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Debug(object message)
        {
#if DEBUG
            var rootAppender = ((Hierarchy)LogManager.GetRepository()).Root.Appenders.OfType<FileAppender>().FirstOrDefault();
            if (rootAppender != null && string.IsNullOrEmpty(rootAppender.File))
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(rootAppender.File));
            }
#endif
#if __IOS__
            Log.Debug(string.Format("{0}", message));
#endif
            _logger?.Debug(message);
        }

        /// <summary>
        /// Logs a debug message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Debug(object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Log.Debug(string.Format("{0}", message));
            else
                Log.Debug(exception, string.Format("{0}", message));
#endif
            if (exception == null)
                _logger?.Debug(message);
            else
                _logger?.Debug(message, exception);
        }

        /// <summary>
        /// Logs a formatted debug message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void DebugFormat(string format, params object[] args)
        {
#if __IOS__
            Log.Debug(string.Format(format, args));
#endif
            _logger?.DebugFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted debug message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void DebugFormat(string format, object arg0)
        {
#if __IOS__
            Log.Debug(format, arg0);
#endif
            _logger?.DebugFormat(format, arg0);
        }

        /// <summary>
        /// Logs a formatted debug message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void DebugFormat(string format, object arg0, object arg1)
        {
#if __IOS__
            Log.Debug(format, arg0, arg1);
#endif
            _logger?.DebugFormat(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted debug message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        public override void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
#if __IOS__
            Log.Debug(format, arg0, arg1, arg2);
#endif
            _logger?.DebugFormat(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted debug message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
#if __IOS__
            Log.Debug(new SystemStringFormat(provider, format, args).ToString());
#endif
            _logger?.DebugFormat(provider, format, args);
        }
        #endregion Implementation of Debug

        #region Implementation of Base Functions
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Error(object message)
        {
#if __IOS__
            Log.Error(string.Format("{0}", message));
#endif
            _logger?.Error(message);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Error(object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Log.Error(string.Format("{0}", message));
            else
                Log.Error(exception, string.Format("{0}", message));
#endif
            if (exception == null)
                _logger?.Error(message);
            else
                _logger?.Error(message, exception);
        }

        /// <summary>
        /// Logs a formatted error message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void ErrorFormat(string format, params object[] args)
        {
#if __IOS__
            Log.Error(string.Format(format, args));
#endif
            _logger?.ErrorFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted error message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void ErrorFormat(string format, object arg0)
        {
#if __IOS__
            Log.Error(string.Format(format, arg0));
#endif
            _logger?.ErrorFormat(format, arg0);
        }

        /// <summary>
        /// Logs a formatted error message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void ErrorFormat(string format, object arg0, object arg1)
        {
#if __IOS__
            Log.Error(string.Format(format, arg0, arg1));
#endif
            _logger?.ErrorFormat(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted error message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        public override void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
#if __IOS__
            Log.Error(string.Format(format, arg0, arg1, arg2));
#endif
            _logger?.ErrorFormat(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted error message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
#if __IOS__
            Log.Error(new SystemStringFormat(provider, format, args).ToString());
#endif
            _logger?.ErrorFormat(provider, format, args);
        }

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Fatal(object message)
        {
#if __IOS__
            Log.Fatal(string.Format("{0}", message));
#endif
            _logger?.Fatal(message);
        }

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Fatal(object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Log.Fatal(string.Format("{0}", message));
            else
                Log.Fatal(exception, string.Format("{0}", message));
#endif
            if (exception == null)
                _logger?.Fatal(message);
            else
                _logger?.Fatal(message, exception);
        }

        /// <summary>
        /// Logs a formatted fatal message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void FatalFormat(string format, params object[] args)
        {
#if __IOS__
            Log.Fatal(string.Format(format, args));
#endif
            _logger?.FatalFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted fatal message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void FatalFormat(string format, object arg0)
        {
#if __IOS__
            Log.Fatal(string.Format(format, arg0));
#endif
            _logger?.FatalFormat(format, arg0);
        }

        /// <summary>
        /// Logs a formatted fatal message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void FatalFormat(string format, object arg0, object arg1)
        {
#if __IOS__
            Log.Fatal(string.Format(format, arg0, arg1));
#endif
            _logger?.FatalFormat(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted fatal message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        public override void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
#if __IOS__
            Log.Fatal(string.Format(format, arg0, arg1, arg2));
#endif
            _logger?.FatalFormat(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted fatal message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
#if __IOS__
            Log.Fatal(new SystemStringFormat(provider, format, args).ToString());
#endif
            _logger?.FatalFormat(provider, format, args);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Info(object message)
        {
#if DEBUG
            var rootAppender = ((Hierarchy)LogManager.GetRepository()).Root.Appenders.OfType<FileAppender>().FirstOrDefault();
            if (rootAppender != null && string.IsNullOrEmpty(rootAppender.File))
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(rootAppender.File));
            }
#endif
#if __IOS__
            Log.Information(string.Format("{0}", message));
#endif
            _logger?.Info(message);
        }

        /// <summary>
        /// Logs an informational message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Info(object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Log.Information(string.Format("{0}", message));
            else
                Log.Information(exception, string.Format("{0}", message));
#endif
            if (exception == null)
                _logger?.Info(message);
            else
                _logger?.Info(message, exception);
        }

        /// <summary>
        /// Logs a formatted informational message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void InfoFormat(string format, params object[] args)
        {
#if __IOS__
            Log.Information(string.Format(format, args));
#endif
            _logger?.InfoFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted informational message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void InfoFormat(string format, object arg0)
        {
#if __IOS__
            Log.Information(string.Format(format, arg0));
#endif
            _logger?.InfoFormat(format, arg0);
        }

        /// <summary>
        /// Logs a formatted informational message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void InfoFormat(string format, object arg0, object arg1)
        {
#if __IOS__
            Log.Information(string.Format(format, arg0, arg1));
#endif
            _logger?.InfoFormat(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted informational message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format
        public override void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
#if __IOS__
            Log.Information(string.Format(format, arg0, arg1, arg2));
#endif
            _logger?.InfoFormat(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted informational message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
#if __IOS__
            Log.Information(new SystemStringFormat(provider, format, args).ToString());
#endif
            _logger?.InfoFormat(provider, format, args);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Warn(object message)
        {
#if __IOS__
            Log.Warning(string.Format("{0}", message));
#endif
            _logger?.Warn(message);
        }

        /// <summary>
        /// Logs a warning message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Warn(object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Log.Warning(string.Format("{0}", message));
            else
                Log.Warning(exception, string.Format("{0}", message));
#endif
            if (exception == null)
                _logger?.Warn(message);
            else
                _logger?.Warn(message, exception);
        }

        /// <summary>
        /// Logs a formatted warning message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void WarnFormat(string format, params object[] args)
        {
#if __IOS__
            Log.Warning(string.Format(format, args));
#endif
            _logger?.WarnFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted warning message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void WarnFormat(string format, object arg0)
        {
#if __IOS__
            Log.Warning(string.Format(format, arg0));
#endif
            _logger?.WarnFormat(format, arg0);
        }

        /// <summary>
        /// Logs a formatted warning message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void WarnFormat(string format, object arg0, object arg1)
        {
#if __IOS__
            Log.Warning(string.Format(format, arg0, arg1));
#endif
            _logger?.WarnFormat(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted warning message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        public override void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
#if __IOS__
            Log.Warning(string.Format(format, arg0, arg1, arg2));
#endif
            _logger?.WarnFormat(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted warning message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
#if __IOS__
            Log.Warning(new SystemStringFormat(provider, format, args).ToString());
#endif
            _logger?.WarnFormat(provider, format, args);
        }
        #endregion Implementation of Base Functions

        #endregion Implementation of ILog

        /// <summary>
        /// Toggle all appenders to this specified level 
        /// </summary>
        /// <param name="level"></param>
        public override void ToggleLogging(SharedLevel level)
        {
            log4net.Core.Level levelLog4Net = new Level(level.Value, level.Name);
            ToggleLoggingPrivate(levelLog4Net);
        }

        private void ToggleLoggingPrivate(log4net.Core.Level level)
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
                    ErrorFormat("Error Setting ThreshHold on {0}.  Error: {1}", appender.Name, ex.ToString());
                }
            }
            //Configure the root logger.
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
            rootLogger.Level = level;
        }

        /// <summary>
        /// Toggle all appenders to this specified level
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public override SharedLevel ToggleLogging(string logLevel)
        {
            var level = ToggleLoggingPrivate(logLevel);
            return new SharedLevel(level.Value, level.Name);
        }
        private log4net.Core.Level ToggleLoggingPrivate(string logLevel)
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
            ToggleLoggingPrivate(level);

            return level;
        }

        /// <summary>
        /// Gets the current logging level.
        /// </summary>
        /// <returns>The current logging level as a SharedLevel.</returns>
        public override SharedLevel LoggingLevel()
        {
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            return new SharedLevel(h.Root.Level.Value, h.Root.Level.Name);
        }

        /// <summary>
        /// Sets the log file for the specified appender.
        /// </summary>
        /// <param name="fileName">The name of the log file.</param>
        public override void SetLogFile(string fileName)
        {
            FileInfo fiNew = new FileInfo(fileName);
            if (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.ToLower().EndsWith(".log"))
                    fiNew = new FileInfo(fileName);
                else
                    fiNew = new FileInfo(fileName + ".log");
            }

            log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
            {
                try
                {
                    if (appender.Name.ToLower() == "logfileappender")
                    {
                        if (appender is log4net.Appender.RollingFileAppender fa)
                        {
                            FileInfo fiOld = new FileInfo(fa.File);

                            if (string.IsNullOrEmpty(fa.DatePattern))
                            {
                                if (!fiOld.Name.Replace(fiOld.Extension, "").ToLower().StartsWith(fiNew.Name.Replace(fiNew.Extension, "").ToLower()))
                                {
                                    fa.File = fileName;
                                    fa.ActivateOptions();
                                }
                            }
                            else
                            {
                                string strDatePattern = DateTime.Now.ToString(fa.DatePattern);
                                string strExpectedFileName = fileName + strDatePattern;
                                fiNew = new FileInfo(strExpectedFileName);

                                if (fiOld.Name.ToLower() != fiNew.Name.ToLower())
                                {
                                    fa.File = fileName;
                                    fa.ActivateOptions();
                                }
                            }
                        }
                        else if (appender is log4net.Appender.FileAppender fa2)
                        {
                            FileInfo fiOld = new FileInfo(fa2.File);

                            if (!fiOld.Name.Replace(fiOld.Extension, "").ToLower().StartsWith(fiNew.Name.Replace(fiNew.Extension, "").ToLower()))
                            {
                                fa2.File = fileName;
                                fa2.ActivateOptions();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorFormat("Error Setting File on {0}.  Error: {1}", appender.Name, ex.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the log file for the specified appender.
        /// </summary>
        /// <returns>The log file name.</returns>
        public override string GetLogFile()
        {
            log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
            {
                try
                {
                    if (appender.Name.ToLower() == "logfileappender")
                    {
                        if (appender is log4net.Appender.RollingFileAppender fa)
                        {
                            return fa.File;
                        }
                        else if (appender is log4net.Appender.FileAppender fa2)
                        {
                            return fa2.File;
                        }
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    ErrorFormat("Error Setting File on {0}.  Error: {1}", appender.Name, ex.ToString());
                }
            }
            return "";
        }

        /// <summary>
        /// Replaces the current log file with a new log file.
        /// </summary>
        /// <param name="currentFileName">The current log file name.</param>
        /// <param name="fileName">The new log file name.</param>
        /// <returns>True if the log file was replaced, otherwise false.</returns>
        public override bool ReplaceLogFile(string currentFileName, string fileName)
        {
            bool bReplaced = false;
            FileInfo fiCurrent = null;
            if (!string.IsNullOrEmpty(currentFileName))
            {
                if (currentFileName.ToLower().EndsWith(".log"))
                    fiCurrent = new FileInfo(currentFileName);
                else
                    fiCurrent = new FileInfo(currentFileName + ".log");
            }

            log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
            {
                try
                {
                    if (appender is log4net.Appender.RollingFileAppender fa)
                    {
                        if (fiCurrent == null)
                        {
                            fa.File = fileName;
                            fa.ActivateOptions();
                            bReplaced = true;
                            break;
                        }
                        else
                        {
                            FileInfo fiOld = new FileInfo(fa.File);

                            if (string.IsNullOrEmpty(fa.DatePattern))
                            {
                                if (fiOld.Name.Replace(fiOld.Extension, "").ToLower().StartsWith(fiCurrent.Name.Replace(fiCurrent.Extension, "").ToLower()))
                                {
                                    fa.File = fileName;
                                    fa.ActivateOptions();
                                    bReplaced = true;
                                    break;
                                }
                            }
                            else
                            {
                                string strDatePattern = DateTime.Now.ToString(fa.DatePattern);
                                string strExpectedFileName = fiCurrent.Name.Replace(fiCurrent.Extension, "").ToLower() + strDatePattern;

                                if (fiOld.Name.ToLower() == strExpectedFileName.ToLower())
                                {
                                    fa.File = fileName;
                                    fa.ActivateOptions();
                                    bReplaced = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (appender is log4net.Appender.FileAppender fa2)
                    {
                        FileInfo fiOld = new FileInfo(fa2.File);

                        if (fiCurrent == null)
                        {
                            fa2.File = fileName;
                            fa2.ActivateOptions();
                            bReplaced = true;
                            break;
                        }
                        else if (fiOld.Name.ToLower() == fiCurrent.Name.ToLower())
                        {
                            fa2.File = fileName;
                            fa2.ActivateOptions();
                            bReplaced = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorFormat("Error Setting File on {0}.  Error: {1}", appender.Name, ex.ToString());
                }
            }
            return bReplaced;
        }
        private static bool HasAppender<T>(ILog logger) where T : IAppender
        {
            var hierarchy = (Hierarchy)logger.Logger.Repository;
            var rootAppender = hierarchy.Root.Appenders;
            foreach (var appender in rootAppender)
            {
                if (appender is T)
                {
                    return true;
                }
            }
            return false;
        }
    }
}