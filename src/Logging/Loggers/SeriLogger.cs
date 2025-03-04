using AdvancedLogging.Constants;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Reflection;

namespace AdvancedLogging.Loggers
{
    /// <summary>
    /// Logger class implementing CommonLogger using Serilog.
    /// </summary>
    public class SeriLogger : CommonLogger
    {
        private ILogger _logger;
        private SharedLevel _logLevel = SharedLevel.Info;
        private string _logFileName = string.Empty;
        private static readonly LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        /// <summary>
        /// Initializes a new instance of the SeriLogger class.
        /// </summary>
        public SeriLogger()
        {
            _logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();
        }

        public override bool IsRemoting => IsRemoteLoggingConfigured(_logger);
        public override bool IsLoggingToConsole => IsLoggingToSink(_logger, "ConsoleSink");
        public override bool IsLoggingToDebugWindow => IsLoggingToSink(_logger, "DebugSink");

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Debug(object message)
        {
            _logger?.Debug("{Message}", message);
        }

        /// <summary>
        /// Logs a debug message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Debug(object message, Exception exception)
        {
            _logger?.Debug(exception, "{Message}", message);
        }

        /// <summary>
        /// Logs a formatted debug message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void DebugFormat(string format, params object[] args)
        {
            _logger?.Debug(format, args);
        }

        /// <summary>
        /// Logs a formatted debug message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void DebugFormat(string format, object arg0)
        {
            _logger?.Debug(format, arg0);
        }

        /// <summary>
        /// Logs a formatted debug message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void DebugFormat(string format, object arg0, object arg1)
        {
            _logger?.Debug(format, arg0, arg1);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Error(object message)
        {
            _logger?.Error("{Message}", message);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Error(object message, Exception exception)
        {
            _logger?.Error(exception, "{Message}", message);
        }

        /// <summary>
        /// Logs a formatted error message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void ErrorFormat(string format, params object[] args)
        {
            _logger?.Error(format, args);
        }

        /// <summary>
        /// Logs a formatted error message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void ErrorFormat(string format, object arg0)
        {
            _logger?.Error(format, arg0);
        }

        /// <summary>
        /// Logs a formatted error message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void ErrorFormat(string format, object arg0, object arg1)
        {
            _logger?.Error(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Fatal(object message)
        {
            _logger?.Fatal("{Message}", message);
        }

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Fatal(object message, Exception exception)
        {
            _logger?.Fatal(exception, "{Message}", message);
        }

        /// <summary>
        /// Logs a formatted fatal message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void FatalFormat(string format, params object[] args)
        {
            _logger?.Fatal(format, args);
        }

        /// <summary>
        /// Logs a formatted fatal message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void FatalFormat(string format, object arg0)
        {
            _logger?.Fatal(format, arg0);
        }

        /// <summary>
        /// Logs a formatted fatal message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void FatalFormat(string format, object arg0, object arg1)
        {
            _logger?.Fatal(format, arg0, arg1);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Info(object message)
        {
            _logger?.Information("{Message}", message);
        }

        /// <summary>
        /// Logs an informational message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Info(object message, Exception exception)
        {
            _logger?.Information(exception, "{Message}", message);
        }

        /// <summary>
        /// Logs a formatted informational message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void InfoFormat(string format, params object[] args)
        {
            _logger?.Information(format, args);
        }

        /// <summary>
        /// Logs a formatted informational message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void InfoFormat(string format, object arg0)
        {
            _logger?.Information(format, arg0);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public override void Warn(object message)
        {
            _logger?.Warning("{Message}", message);
        }

        /// <summary>
        /// Logs a warning message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public override void Warn(object message, Exception exception)
        {
            _logger?.Warning(exception, "{Message}", message);
        }

        /// <summary>
        /// Logs a formatted warning message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        public override void WarnFormat(string format, object arg0)
        {
            _logger?.Warning(format, arg0);
        }

        /// <summary>
        /// Toggles the logging level at runtime.
        /// </summary>
        /// <param name="level">The logging level to set.</param>
        public override void ToggleLogging(SharedLevel level)
        {
            _logLevel = level;
            // Change the logging level at runtime
            if (level.Equals(SharedLevel.Debug))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Debug;
            }
            else if (level.Equals(SharedLevel.Info))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Information;
            }
            else if (level.Equals(SharedLevel.Warn))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Warning;
            }
            else if (level.Equals(SharedLevel.Error))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Error;
            }
            else if (level.Equals(SharedLevel.Fatal))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Fatal;
            }
            else
            {
                levelSwitch.MinimumLevel = LogEventLevel.Information;
            }
        }

        /// <summary>
        /// Gets the current logging level.
        /// </summary>
        /// <returns>The current logging level.</returns>
        public override SharedLevel LoggingLevel()
        {
            return _logLevel;
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
            _logger?.Debug(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted debug message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger?.Debug(string.Format(provider, format, args));
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
            _logger?.Error(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted error message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger?.Error(string.Format(provider, format, args));
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
            _logger?.Fatal(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted fatal message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger?.Fatal(string.Format(provider, format, args));
        }

        /// <summary>
        /// Logs a formatted informational message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void InfoFormat(string format, object arg0, object arg1)
        {
            _logger?.Information(format, arg0, arg1);
        }

        /// <summary>
        /// Logs a formatted informational message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        public override void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger?.Information(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted informational message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger?.Information(string.Format(provider, format, args));
        }

        /// <summary>
        /// Logs a formatted warning message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void WarnFormat(string format, params object[] args)
        {
            _logger?.Warning(format, args);
        }

        /// <summary>
        /// Logs a formatted warning message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        public override void WarnFormat(string format, object arg0, object arg1)
        {
            _logger?.Warning(format, arg0, arg1);
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
            _logger?.Warning(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Logs a formatted warning message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        public override void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger?.Warning(string.Format(provider, format, args));
        }

        /// <summary>
        /// Toggles the logging level based on a string input.
        /// </summary>
        /// <param name="logLevel">The logging level as a string.</param>
        /// <returns>The logging level as a SharedLevel.</returns>
        public override SharedLevel ToggleLogging(string logLevel)
        {
            _logLevel = (SharedLevel)Enum.Parse(typeof(SharedLevel), logLevel, true);
            return _logLevel;
        }

        /// <summary>
        /// Sets the log file for the logger.
        /// </summary>
        /// <param name="fileName">The name of the log file.</param>
        public override void SetLogFile(string fileName)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File(
                    path: fileName,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
            _logFileName = fileName;
        }

        /// <summary>
        /// Gets the current log file name.
        /// </summary>
        /// <returns>The current log file name.</returns>
        public override string GetLogFile()
        {
            return _logFileName;
        }

        /// <summary>
        /// Replaces the current log file with a new log file.
        /// </summary>
        /// <param name="currentFileName">The current log file name.</param>
        /// <param name="fileName">The new log file name.</param>
        /// <returns>True if the log file was replaced, otherwise false.</returns>
        public override bool ReplaceLogFile(string currentFileName, string fileName)
        {
            try
            {
                SetLogFile(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRemoteLoggingConfigured(ILogger logger)
        {
            // Check if the logger configuration includes any remote sinks
            // This is a simplified example and may need to be adapted based on your specific configuration
            if (logger is Serilog.Core.Logger loggerConfiguration)
            {
                // Use reflection to check for remote sinks
                var sinksField = typeof(Serilog.Core.Logger).GetField("_sink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sinksField != null)
                {
                    var sink = sinksField.GetValue(loggerConfiguration);
                    if (sink != null && (sink.GetType().Name.Contains("SeqSink") || sink.GetType().Name.Contains("ElasticsearchSink")))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsLoggingToSink(ILogger logger, string sinkName)
        {
            var loggerType = logger.GetType();
            var sinksField = loggerType.GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sinksField != null)
            {
                var sinks = (ILogEventSink[])sinksField.GetValue(Log.Logger);
                sinkName = sinkName.ToLower();
                foreach (var sink in sinks)
                {
                    if (sink.GetType().Name.ToLower().Contains(sinkName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
