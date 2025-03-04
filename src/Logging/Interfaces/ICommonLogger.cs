using AdvancedLogging.Constants;
using AdvancedLogging.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for common logging functionality.
    /// </summary>
    public interface ICommonLogger
    {
        // Event Handlers
        #region Event Handlers
        /// <summary>
        /// Event triggered when the configuration file changes.
        /// </summary>
        event EventHandler<ConfigFileChangedEventArgs> ConfigFileChanged;

        /// <summary>
        /// Event triggered when a configuration setting changes.
        /// </summary>
        event EventHandler<ConfigSettingChangedEventArgs> ConfigSettingChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the debug levels.
        /// </summary>
        Dictionary<string, int> DebugLevels { get; set; }

        /// <summary>
        /// Gets or sets the monitored settings.
        /// </summary>
        ConcurrentDictionary<string, string> MonitoredSettings { get; set; }

        /// <summary>
        /// Gets or sets the password settings.
        /// </summary>
        ConcurrentDictionary<string, bool> IsPassword { get; set; }

        /// <summary>
        /// Gets or sets the auto log SQL threshold.
        /// </summary>
        double AutoLogSQLThreshold { set; get; }

        /// <summary>
        /// Gets or sets a value indicating whether monitoring is enabled.
        /// </summary>
        bool Monitoring { set; get; }

        /// <summary>
        /// Gets or sets the log file.
        /// </summary>
        string LogFile { set; get; }

        /// <summary>
        /// Gets or sets the configuration file.
        /// </summary>
        string ConfigFile { set; get; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        int LogLevel { set; get; }

        /// <summary>
        /// Gets or sets the logging level.
        /// </summary>
        SharedLevel Level { set; get; }

        /// <summary>
        /// Gets the contents of the configuration file.
        /// </summary>
        string ConfigFileContents { get; }

        /// <summary>
        /// Gets the configuration file as an XML document.
        /// </summary>
        XmlDocument ConfigFileXml { get; }

        /// <summary>
        /// Gets the configuration file as an XML document.
        /// </summary>
        XmlDocument ConfigFileXmlDocument { get; }
        #endregion Properties

        // Debug Methods
        #region Debug Methods
        #region Base Debug Methods
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Debug(object message);

        /// <summary>
        /// Logs a debug message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void Debug(object message, Exception exception);

        /// <summary>
        /// Logs a formatted debug message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void DebugFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted debug message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        void DebugFormat(string format, object arg0);

        /// <summary>
        /// Logs a formatted debug message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        void DebugFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted debug message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        void DebugFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted debug message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void DebugFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Base Debug Methods

        #region Debug Helpers
        /// <summary>
        /// Logs a debug message with a prefix and an exception.
        /// </summary>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void DebugPrefix(string logPreFix, object message, Exception exception);

        /// <summary>
        /// Logs a debug message with a prefix, function name, and an exception.
        /// </summary>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void DebugPrefix(string logPreFix, string functionName, object message, Exception exception);
        #endregion Debug Helpers

        #region Advanced Debug Methods with Level
        /// <summary>
        /// Logs a debug message with a specified level.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool Debug(Int32 level, object message);

        /// <summary>
        /// Logs a debug message with a specified level and an exception.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool Debug(Int32 level, object message, Exception exception);

        /// <summary>
        /// Logs a formatted debug message with a specified level.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string format, params object[] args);

        /// <summary>
        /// Logs a formatted debug message with a specified level and one argument.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string format, object arg0);

        /// <summary>
        /// Logs a formatted debug message with a specified level and two arguments.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted debug message with a specified level and three arguments.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted debug message with a specified level and a format provider.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, IFormatProvider provider, string format, params object[] args);

        /// <summary>
        /// Logs a debug message with a specified level, function name, and an exception.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool Debug(Int32 level, string functionName, object message, Exception exception);

        /// <summary>
        /// Logs a debug message with a specified level, prefix, and an exception.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugPrefix(Int32 level, string logPreFix, object message, Exception exception);

        /// <summary>
        /// Logs a debug message with a specified level, prefix, function name, and an exception.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugPrefix(Int32 level, string logPreFix, string functionName, object message, Exception exception);

        /// <summary>
        /// Logs a formatted debug message with a specified level and function name.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string functionName, string format, params object[] args);

        /// <summary>
        /// Logs a formatted debug message with a specified level, function name, and one argument.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string functionName, string format, object arg0);

        /// <summary>
        /// Logs a formatted debug message with a specified level, prefix, function name, and one argument.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormatPrefix(Int32 level, string logPreFix, string functionName, string format, object arg0);

        /// <summary>
        /// Logs a formatted debug message with a specified level, function name, and two arguments.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string functionName, string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted debug message with a specified level, prefix, function name, and two arguments.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="logPreFix">The prefix for the log message.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormatPrefix(Int32 level, string logPreFix, string functionName, string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted debug message with a specified level, function name, and three arguments.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string functionName, string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted debug message with a specified level, function name, and a format provider.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        bool DebugFormat(Int32 level, string functionName, IFormatProvider provider, string format, params object[] args);
        #endregion Advanced Debug Methods with Level
        #endregion Debug Methods

        // Error Methods
        #region Error Methods
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Error(object message);

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void Error(object message, Exception exception);

        /// <summary>
        /// Logs a formatted error message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void ErrorFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted error message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        void ErrorFormat(string format, object arg0);

        /// <summary>
        /// Logs a formatted error message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        void ErrorFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted error message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        void ErrorFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted error message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void ErrorFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Error Methods

        // Fatal Methods
        #region Fatal Methods
        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Fatal(object message);

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void Fatal(object message, Exception exception);

        /// <summary>
        /// Logs a formatted fatal message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void FatalFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted fatal message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        void FatalFormat(string format, object arg0);

        /// <summary>
        /// Logs a formatted fatal message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        void FatalFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted fatal message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        void FatalFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted fatal message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void FatalFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Fatal Methods

        // Info Methods
        #region Info Methods
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Info(object message);

        /// <summary>
        /// Logs an informational message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void Info(object message, Exception exception);

        /// <summary>
        /// Logs a formatted informational message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted informational message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        void InfoFormat(string format, object arg0);

        /// <summary>
        /// Logs a formatted informational message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        void InfoFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted informational message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        void InfoFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted informational message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void InfoFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Info Methods

        // Warn Methods
        #region Warn Methods
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Warn(object message);

        /// <summary>
        /// Logs a warning message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        void Warn(object message, Exception exception);

        /// <summary>
        /// Logs a formatted warning message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void WarnFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted warning message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The argument for the format string.</param>
        void WarnFormat(string format, object arg0);

        /// <summary>
        /// Logs a formatted warning message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        void WarnFormat(string format, object arg0, object arg1);

        /// <summary>
        /// Logs a formatted warning message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first argument for the format string.</param>
        /// <param name="arg1">The second argument for the format string.</param>
        /// <param name="arg2">The third argument for the format string.</param>
        void WarnFormat(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Logs a formatted warning message with a format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments for the format string.</param>
        void WarnFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Warn Methods

        // Status Check Properties
        #region Status Check Properties
        /// <summary>
        /// Gets a value indicating whether all logging levels are enabled.
        /// </summary>
        bool IsAllEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether debug logging is enabled.
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether informational logging is enabled.
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether warning logging is enabled.
        /// </summary>
        bool IsWarnEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether error logging is enabled.
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether fatal logging is enabled.
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether logging is remote.
        /// </summary>
        bool IsRemoting { get; }

        /// <summary>
        /// Gets a value indicating whether logging to console is enabled.
        /// </summary>
        bool IsLoggingToConsole { get; }

        /// <summary>
        /// Gets a value indicating whether logging to debug window is enabled.
        /// </summary>
        bool IsLoggingToDebugWindow { get; }
        #endregion Status Check Properties

        // Additional Methods
        #region Additional Methods
        /// <summary>
        /// Checks if the current code path includes the specified function.
        /// </summary>
        /// <param name="functionName">The function name to check.</param>
        /// <param name="detectedFunction">The detected function name.</param>
        /// <returns>True if the function is in the code path, otherwise false.</returns>
        bool InCodePathOf(string functionName, out string detectedFunction);

        /// <summary>
        /// Checks if the current code path includes the specified function.
        /// </summary>
        /// <param name="functionName">The function name to check.</param>
        /// <param name="frames">The stack frames to check.</param>
        /// <param name="detectedFunction">The detected function name.</param>
        /// <returns>True if the function is in the code path, otherwise false.</returns>
        bool InCodePathOf(string functionName, StackFrame[] frames, out string detectedFunction);

        /// <summary>
        /// Gets the full path of the specified stack frames.
        /// </summary>
        /// <param name="frames">The stack frames.</param>
        /// <returns>The full path of the stack frames.</returns>
        string FullPath(StackFrame[] frames);

        /// <summary>
        /// Checks if debug logging is enabled.
        /// </summary>
        /// <returns>True if debug logging is enabled, otherwise false.</returns>
        bool LoggingDebug();

        /// <summary>
        /// Checks if the specified log level should be logged.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="detectedCriteria">The detected criteria.</param>
        /// <param name="detectedFunction">The detected function name.</param>
        /// <param name="detectedLevel">The detected log level.</param>
        /// <returns>True if the log level should be logged, otherwise false.</returns>
        bool ToLog(int level, out string detectedCriteria, out string detectedFunction, out int detectedLevel);

        /// <summary>
        /// Checks if the specified log level should be logged.
        /// </summary>
        /// <param name="_level">The log level.</param>
        /// <returns>True if the log level should be logged, otherwise false.</returns>
        bool ToLog(int _level);

        /// <summary>
        /// Toggle all appenders to this specified level.
        /// </summary>
        /// <param name="level">The logging level.</param>
        void ToggleLogging(SharedLevel level);

        /// <summary>
        /// Toggle all appenders to this specified level.
        /// </summary>
        /// <param name="logLevel">The logging level as a string.</param>
        /// <returns>The logging level as a SharedLevel.</returns>
        SharedLevel ToggleLogging(string logLevel);

        /// <summary>
        /// Gets the current logging level.
        /// </summary>
        /// <returns>The current logging level as a SharedLevel.</returns>
        SharedLevel LoggingLevel();

        /// <summary>
        /// Sets the log file for the specified appender.
        /// </summary>
        /// <param name="fileName">The name of the log file.</param>
        void SetLogFile(string fileName);

        /// <summary>
        /// Gets the log file for the specified appender.
        /// </summary>
        /// <returns>The log file name.</returns>
        string GetLogFile();

        /// <summary>
        /// Replaces the current log file with a new log file.
        /// </summary>
        /// <param name="currentFileName">The current log file name.</param>
        /// <param name="fileName">The new log file name.</param>
        /// <returns>True if the log file was replaced, otherwise false.</returns>
        bool ReplaceLogFile(string currentFileName, string fileName);

        /// <summary>
        /// Checks if the specified value is unevaluated.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to check.</param>
        /// <param name="checkForEmpty">Whether to check for empty values.</param>
        /// <returns>True if the value is unevaluated, otherwise false.</returns>
        bool IsUnEvaluated(string name, string value, bool checkForEmpty = false);

        /// <summary>
        /// Checks if the specified value is a valid connection string.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is a valid connection string, otherwise false.</returns>
        bool IsConnectionString(string name, string value);
        #endregion Additional Methods
    }
}
