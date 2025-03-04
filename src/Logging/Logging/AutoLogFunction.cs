using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Loggers;
using AdvancedLogging.Utilities;
using AdvancedLogging.Models;
using log4net;
#if __IOS__
using System.Linq2;
#else
using System.Linq;
#endif
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using static AdvancedLogging.Utilities.LoggingUtils;

namespace AdvancedLogging.Logging
{
    public class AutoLogFunction : IDisposable
    {
        private readonly bool m_bIsError = false;
        private bool m_bToLog = true;
        private bool m_bSuppressFunctionDeclaration = false;
        private Stopwatch m_sw = new Stopwatch();
#if __IOS__
        private MethodBase m_function = default!;
        private readonly object m_parameters = default;
#else
        private MethodBase m_function = null;
        private readonly object m_parameters = null;
#endif		
        private Guid m_guidInstanceId;
        private int m_iTabs = 0;
        private string m_strLogPrefix = "";
        private string m_strCallPath = "";
        private string m_strName = "";
        private string m_strFullName = "";
#if __IOS__
        private string? m_strDynamicLoggingNotice = "<-- DYNAMIC LOGGING --> ";
#else
        private string m_strDynamicLoggingNotice = "<-- DYNAMIC LOGGING --> ";
#endif		
        private bool m_bFunctionDeclarationLogged = false;
        private bool m_bFunctionParametersLogged = false;
        private bool m_bMemberTypeInformation = true;
        private ConcurrentDictionary<int, bool> m_dicDetailedLoggingDetected = new ConcurrentDictionary<int, bool>();

#if __IOS__
        public ICommonLogger? Logger
#else
        public ICommonLogger Logger
#endif		
        {
            get { return LoggingUtils.Logger; }
        }

#if __IOS__
        public ILoggerUtility? LoggerUtility
#else
        public ILoggerUtility LoggerUtility
#endif		
        {
            get { return LoggingUtils.LoggerUtility; }
        }
        public int Tabs
        {
            get { return m_iTabs; }
            set { m_iTabs = value; }
        }

        public bool ToLog
        {
            get { return m_bToLog; }
            set { m_bToLog = value; }
        }

        public bool SuppresFunctionDeclaration
        {
            get { return m_bSuppressFunctionDeclaration; }
            set { m_bSuppressFunctionDeclaration = value; }
        }

        private Guid GuidInstanceId
        {
            get
            {
                if (m_guidInstanceId == Guid.Empty)
                    m_guidInstanceId = Guid.NewGuid();
                return m_guidInstanceId;
            }
        }

        /// <summary>
        /// Gets or sets the formatted log prefix, which includes a unique identifier, a log prefix string,
        /// and optional tab formatting based on the function's member type and logging level.
        /// </summary>

        public string LogPrefix
        {
            get
            {
                string prefix = "[" + GetInt64HashCode(GuidInstanceId.ToByteArray()).ToString("X16") + "] - ";

                if (Function == null)
                {
                    return prefix + m_strLogPrefix + (FunctionDeclarationLogged ? "\t" : "");
                }

                int debugLevel = Function.MemberType == MemberTypes.Constructor
                    ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                    : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                bool shouldLog = DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value) ||
                                 (ApplicationSettings.Logger?.LogLevel ?? 0) >= debugLevel;

                if (shouldLog)
                {
                    return prefix + m_strLogPrefix + (FunctionDeclarationLogged ? "\t" : "");
                }

                return prefix + (FunctionDeclarationLogged ? "\t" : "");
            }
            set
            {
                m_strLogPrefix = value;
            }
        }


        public string CallPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_strCallPath))
                {
                    Initialization();
                    if (Function == null)
                    {
                        // Get calling function where this class is instantiated
                        Function = (new StackFrame(2, true)).GetMethod();
                    }
                    if (LoggingUtils.IsRemotingAppender)
                    {
                        ThreadContext.Properties["procnamefull"] = FullName;
                        ThreadContext.Properties["procname"] = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
                    }
                    m_strCallPath = Loggers.Log4NetLogger.FunctionFullPath(new StackTrace(2).GetFrames().ToArray());
                }
                return m_strCallPath;
            }
            set
            {
                m_strCallPath = value;
#if DEBUG
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine((LogPrefix == "" ? "-> " : LogPrefix) + m_strCallPath);
#endif
            }
        }

        public MethodBase Function
        {
            get { return m_function; }
            set { m_function = value; }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(m_strName) && m_function != null)
                {
                    m_strName = m_function.MemberType == MemberTypes.Constructor ? m_function.DeclaringType.Name : m_function.Name;
                }
                return m_strName;
            }
            set { m_strName = value; }
        }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(m_strFullName) && m_function != null)
                {
                    m_strFullName = m_function.DeclaringType.FullName + "." + Name;
                }
                return m_strFullName;
            }
            set { m_strFullName = value; }
        }

        public ConcurrentDictionary<int, bool> DetailedLoggingDetected
        {
            get { return m_dicDetailedLoggingDetected; }
            set { m_dicDetailedLoggingDetected = value; }
        }

        public bool FunctionDeclarationLogged
        {
            get { return m_bFunctionDeclarationLogged; }
            set { m_bFunctionDeclarationLogged = value; }
        }
        public bool FunctionParametersLogged
        {
            get { return m_bFunctionParametersLogged; }
            set { m_bFunctionParametersLogged = value; }
        }
        public bool MemberTypeInformation
        {
            get { return m_bMemberTypeInformation; }
            set { m_bMemberTypeInformation = value; }
        }
#if __IOS__
        public string? DynamicLoggingNotice
#else
        public string DynamicLoggingNotice
#endif		
        {
            get { return m_strDynamicLoggingNotice; }
            set { m_strDynamicLoggingNotice = value; }
        }
        private void Initialization()
        {
            if (ApplicationSettings.Logger != null)
                MemberTypeInformation = ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_MemberTypeInformation];
            if (LoggingUtils.Logger != null)
                DynamicLoggingNotice = LoggingUtils.Logger?.MonitoredSettings.GetOrAdd(ConfigurationSetting.Log_DynamicLoggingNotice, DynamicLoggingNotice);
        }

        /// <summary>
        /// Initializes an instance of AutoLogFunction, automatically logging details such as the function name, parameters,
        /// and call path, while adhering to application logging configurations.
        /// </summary>
        /// <param name="function">
        /// The <see cref="MethodBase"/> of the function being logged. If null, the calling function is inferred.
        /// </param>
        /// <param name="_Parent">
        /// The parent <see cref="AutoLogFunction"/> instance, used for hierarchical logging and tab alignment.
        /// </param>
        /// <param name="error">Indicates whether this is an error log.</param>
        /// <param name="bSuppressFunctionDeclaration">
        /// Specifies whether to suppress the function declaration in the log entry.
        /// </param>

#if __IOS__
        public AutoLogFunction(MethodBase? function = null, AutoLogFunction? _Parent = null, bool error = false, bool bSuppressFunctionDeclaration = false)
#else
        public AutoLogFunction(MethodBase function = null, AutoLogFunction _Parent = null, bool error = false, bool bSuppressFunctionDeclaration = false)
#endif
        {
            if (!AllowLogging || (!error && bSuppressFunctionDeclaration) || !WillWriteToLog(error))
                return;

            m_sw?.Start();
            Function = function ?? new StackFrame(1, true).GetMethod();
            m_bIsError = error;
            m_bSuppressFunctionDeclaration = bSuppressFunctionDeclaration;

            // Determine the function name
            Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
            if (IgnoreLogging.GetOrAdd(Name, false))
                return;

            Initialization();
            FullName = $"{Function.DeclaringType.FullName}.{Name}";

            // Set up hierarchical logging
            Tabs = _Parent?.Tabs + 1 ?? ApplicationSettings.AutoLogActivity.Count(p => CallPath.StartsWith(p.Key.Replace(".AutoLogFunction", ""))) - 1;
            LogPrefix = Tabs > 0 ? new string('\t', Tabs) : string.Empty;

            // Update contextual properties and call path
            using (ThreadContext.Stacks["NDC"].Push(FullName))
            {
                StackTrace stackTrace = new StackTrace();
                CallPath = Log4NetLogger.FunctionFullPath(stackTrace.GetFrames()?.Skip(1).ToArray());
                ApplicationSettings.AutoLogActivity.AddOrUpdate(CallPath, GuidInstanceId, (_, __) => GuidInstanceId);

                if (LoggingUtils.IsRemotingAppender)
                {
                    ThreadContext.Properties["procname"] = Name;
                    ThreadContext.Properties["procnamefull"] = FullName;
                }

                // Log function declaration
                string message = $"[Func: {FullName}{(m_bMemberTypeInformation ? $" ({Function.MemberType})" : string.Empty)}]";
                LogDynamicDetails(ref message);
                FunctionDeclarationLogged = WriteToLog(message, error);

                // Log call path
                LogCallPath(error);

                // Optionally log function parameters
                //LogFunctionParameters(parameters, error);
            }
        }

        private void LogDynamicDetails(ref string message)
        {
            if (LoggingUtils.Logger != null &&
                LoggingUtils.Logger.ToLog(LoggingUtils.Logger.LogLevel, out string detectedCriteria, out _, out int detectedLevel) &&
                !string.IsNullOrEmpty(detectedCriteria))
            {
                message = $"[Func: {FullName} ({Function.MemberType}) {DynamicLoggingNotice}[{detectedCriteria}]";
                DetailedLoggingDetected.AddOrUpdate(detectedLevel, true, (_, __) => true);
            }
        }

        private void LogCallPath(bool error)
        {
            int debugLevel = Function.MemberType == MemberTypes.Constructor
                ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

            if (error || (ApplicationSettings.Logger?.LogLevel ?? 0) >= debugLevel + 1)
            {
                string callPathMessage = $"Call Path: {CallPath}";
                if (error)
                    WriteError(callPathMessage);
                else if (ToLog)
                    WriteDebug(callPathMessage);
            }
        }

        private void LogFunctionParameters(object parameters, bool error)
        {
            ParameterInfo[] functionParameters = Function.GetParameters();
            if (ToLog && functionParameters?.Length > 0)
            {
                int debugLevel = Function.MemberType == MemberTypes.Constructor
                    ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                    : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                LoggingUtils.ProcessParametersNoAutoLog(
                    functionParameters,
                    parameters,
                    error,
                    LogPrefix,
                    DetailedLoggingDetected.Any(p => p.Key >= debugLevel + 2 && p.Value) ? 0 : debugLevel + 2);
                FunctionParametersLogged = true;
            }
        }

        /// <summary>
        /// Automatically logs function details, including parameters and call paths, based on logging configuration.
        /// </summary>
        /// <param name="parameters">The parameters passed to the function being logged.</param>
        /// <param name="function">
        /// The <see cref="MethodBase"/> of the function being logged. If null, the calling function is inferred.
        /// </param>
        /// <param name="_Parent">The parent <see cref="AutoLogFunction"/> instance for hierarchical logging.</param>
        /// <param name="error">Indicates whether this is an error log.</param>
        /// <param name="bSuppressFunctionDeclaration">Specifies whether to suppress the function declaration.</param>
#if __IOS__
        public AutoLogFunction(object parameters, MethodBase function = null, AutoLogFunction? _Parent = null, bool error = false, bool bSuppressFunctionDeclaration = false)
#else
        public AutoLogFunction(object parameters, MethodBase function = null, AutoLogFunction _Parent = null, bool error = false, bool bSuppressFunctionDeclaration = false)
#endif
        {
            if (!AllowLogging || (!error && bSuppressFunctionDeclaration) || !WillWriteToLog(error))
                return;

            m_sw?.Start();
            Function = function ?? new StackFrame(1, true).GetMethod();
            m_parameters = parameters;
            m_bIsError = error;
            m_bSuppressFunctionDeclaration = bSuppressFunctionDeclaration;

            Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
            if (IgnoreLogging.GetOrAdd(Name, false))
                return;

            Initialization();
            FullName = $"{Function.DeclaringType.FullName}.{Name}";

            using (ThreadContext.Stacks["NDC"].Push(FullName))
            {
                try
                {
                    StackTrace st = new StackTrace();
                    StackFrame[] arrFrames = st.GetFrames();
                    CallPath = Log4NetLogger.FunctionFullPath(arrFrames.Skip(1).ToArray());

                    if (LoggingUtils.IsRemotingAppender)
                    {
                        ThreadContext.Properties["procname"] = Name;
                        ThreadContext.Properties["procnamefull"] = FullName;
                    }

                    ApplicationSettings.AutoLogActivity.AddOrUpdate(CallPath, GuidInstanceId, (key, oldValue) => GuidInstanceId);

                    Tabs = _Parent == null
                        ? ApplicationSettings.AutoLogActivity.Count(p => CallPath.StartsWith(p.Key.Replace(".AutoLogFunction", ""))) - 1
                        : _Parent.Tabs + 1;

                    LogPrefix = Tabs > 0 ? new string('\t', Tabs) : string.Empty;

                    string message = $"[Func: {FullName}{(m_bMemberTypeInformation ? $" ({Function.MemberType})" : "")}]";

                    // Handle dynamic logging
                    if (LoggingUtils.Logger != null &&
                        LoggingUtils.Logger.ToLog(LoggingUtils.Logger.LogLevel, out string detectedCriteria, out _, out int detectedLevel))
                    {
                        if (!string.IsNullOrEmpty(detectedCriteria))
                        {
                            message = $"[Func: {FullName} ({Function.MemberType}) {DynamicLoggingNotice}[{detectedCriteria}]]";
                            DetailedLoggingDetected.AddOrUpdate(detectedLevel, true, (key, oldValue) => true);
                        }
                    }

                    FunctionDeclarationLogged = WriteToLog(message, error);

                    int debugLevel = DebugPrintLevel[Function.MemberType == MemberTypes.Constructor
                        ? ConfigurationSetting.Log_FunctionHeaderConstructor
                        : ConfigurationSetting.Log_FunctionHeaderMethod];

                    // Log call path
                    if (error || (ApplicationSettings.Logger?.LogLevel ?? 0) >= debugLevel + 1)
                    {
                        string callPathMessage = $"Call Path: {CallPath}";
                        if (error)
                            WriteError(callPathMessage);
                        else if (ToLog)
                            WriteDebug(callPathMessage);
                    }

                    // Process parameters
                    ParameterInfo[] functionParameters = Function.GetParameters();
                    if (ToLog && functionParameters?.Length > 0)
                    {
                        LoggingUtils.ProcessParametersNoAutoLog(
                            functionParameters,
                            parameters,
                            error,
                            LogPrefix,
                            DetailedLoggingDetected.Any(p => p.Key >= debugLevel + 2 && p.Value) ? 0 : debugLevel + 2);
                        FunctionParametersLogged = true;
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Error in AutoLogFunction: ", ex);
                }
            }
        }

        /// <summary>
        /// Automatically logs function details, including parameters and errors, while adhering to logging configurations.
        /// </summary>
        /// <param name="parameters">The parameters passed to the function being logged.</param>
        /// <param name="function">
        /// The <see cref="MethodBase"/> of the function being logged. If null, the calling function is inferred.
        /// </param>
        /// <param name="_Tabs">The tab count for log formatting.</param>
        /// <param name="error">Indicates whether this is an error log.</param>
        /// <param name="bSuppressFunctionDeclaration">
        /// Whether to suppress the function declaration in the logs.
        /// </param>
        public AutoLogFunction(object parameters, MethodBase function, int _Tabs, bool error = false, bool bSuppressFunctionDeclaration = false)
        {
            if (!AllowLogging || (!error && bSuppressFunctionDeclaration) || !WillWriteToLog(error))
                return;

            m_sw?.Start();
            Function = function ?? new StackFrame(1, true).GetMethod();
            Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;

            if (IgnoreLogging.GetOrAdd(Name, false))
                return;

            // Initialization and context setup
            Initialization();
            FullName = Function.DeclaringType.FullName + "." + Name;
            Tabs = _Tabs >= 0 ? _Tabs + 1 : Tabs;
            LogPrefix = Tabs > 0 ? new string('\t', Tabs) : string.Empty;

            if (LoggingUtils.IsRemotingAppender)
            {
                ThreadContext.Properties["procname"] = Name;
                ThreadContext.Properties["procnamefull"] = FullName;
            }

            using (ThreadContext.Stacks["NDC"].Push(FullName))
            {
                try
                {
                    int logLevel = ApplicationSettings.Logger != null ? ApplicationSettings.Logger.LogLevel : 0;
                    string message = "[Func: " + FullName + (m_bMemberTypeInformation ? " (" + Function.MemberType + ")" : "") + "]";

                    if (LoggingUtils.Logger != null && LoggingUtils.Logger.ToLog(LoggingUtils.Logger.LogLevel, out var detectedCriteria, out var _, out var detectedLevel))
                    {
                        if (!string.IsNullOrEmpty(detectedCriteria))
                        {
                            message = "[Func: " + FullName + " (" + Function.MemberType + ") " + DynamicLoggingNotice + "[" + detectedCriteria + "]]";
                            DetailedLoggingDetected.AddOrUpdate(detectedLevel, true, (key, oldValue) => true);
                            logLevel = detectedLevel;
                        }
                    }

                    FunctionDeclarationLogged = WriteToLog(message, error);

                    // Handle call path logging
                    CallPath = Log4NetLogger.FunctionFullPath(new StackTrace().GetFrames()?.Skip(1).ToArray());
                    if (error || logLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] + 1)
                    {
                        string callPathMessage = "Call Path: " + CallPath;
                        if (error)
                            WriteError(callPathMessage);
                        else if (ToLog)
                            WriteDebug(callPathMessage);
                    }

                    // Process parameters
                    ParameterInfo[] functionParameters = Function.GetParameters();
                    if (ToLog && functionParameters != null && functionParameters.Length > 0)
                    {
                        int debugLevel = Function.MemberType == MemberTypes.Constructor
                            ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                            : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                        LoggingUtils.ProcessParametersNoAutoLog(
                            functionParameters,
                            parameters,
                            error,
                            LogPrefix,
                            DetailedLoggingDetected.Any(p => p.Key >= debugLevel + 2 && p.Value) ? 0 : debugLevel + 2);

                        FunctionParametersLogged = true;
                    }
                }
                catch (Exception ex)
                {
                    WriteError("Error in AutoLogFunction: ", ex);
                }
            }
        }

        /// <summary>
        /// Determines whether a message should be written to the log, based on the logging configuration and conditions.
        /// </summary>
        /// <param name="error">Indicates whether the message represents an error.</param>
        /// <returns>
        /// Returns true if the message should be logged; otherwise, false.
        /// </returns>
        private bool WillWriteToLog(bool error)
        {
            if (!AllowLogging) return false;

            if (error)
            {
                return Logger?.IsErrorEnabled ?? false;
            }

            if (Logger == null || !Logger.IsDebugEnabled) return false;

            if (Function != null)
            {
                if (Logger.ToLog(Logger.LogLevel, out _, out _, out _))
                {
                    return true;
                }

                // Determine debug print level based on member type
                int debugLevel = Function.MemberType == MemberTypes.Constructor
                    ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                    : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                // Check for detailed logging or log level sufficiency
                return DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value) ||
                       LoggingUtils.Logger?.LogLevel >= debugLevel;
            }

            return false;
        }

        /// <summary>
        /// Writes a message to the log, handling both error and debug scenarios based on logging configuration.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="error">Indicates whether the message is an error log.</param>
        /// <returns>
        /// Returns true if the message is logged successfully; otherwise, false.
        /// </returns>
        private bool WriteToLog(string message, bool error)
        {
            if (!AllowLogging) return false;

            if (error)
            {
                WriteError(message);
                return true;
            }

            // Determine the debug print level based on the member type
            int debugLevel = Function.MemberType == MemberTypes.Constructor
                ? DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                : DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

            // Check for detailed logging
            bool detailedLogging = DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value);

            if (detailedLogging)
            {
                WriteDebug(message);
                return true;
            }

            // Log with appropriate debug level
            WriteDebug(debugLevel, message);
            return LoggingUtils.Logger?.LogLevel >= debugLevel;
        }


        /// <summary>
        /// Logs details about a function call, including errors and exceptions if provided.
        /// </summary>
        /// <param name="function">
        /// Optional. The <see cref="MethodBase"/> representing the function being logged. If null, the calling function is used.
        /// </param>
        /// <param name="error">Indicates whether the log entry represents an error.</param>
        /// <param name="exception">
        /// Optional. The <see cref="Exception"/> to include in the log if an error occurred.
        /// </param>
#if __IOS__
        public void LogFunction(MethodBase function? = null, bool error = false, Exception? exception = null)
#else
        public void LogFunction(MethodBase function = null, bool error = false, Exception exception = null)
#endif
        {
            if (!AllowLogging) return;

            try
            {
                if (function == null)
                {
                    // Get calling function where this class is instantiated if not explicitly provided
                    function = (new StackFrame(1, true)).GetMethod();
                }
                Function = function;
                Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
                FullName = $"{Function.DeclaringType.FullName}.{Name}";
                string message = $"[Func: {FullName} ({Function.MemberType})]";

                // Log function header
                if (error)
                {
                    WriteError(message);
                    if (exception == null)
                        WriteErrorFormat("\tCall Path: {0}", CallPath);
                }
                else
                {
                    WriteDebug(message);
                }

                // Log exception details if present
                if (exception != null)
                {
                    LogExceptionDetails(exception);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error in LogFunction: ", ex);
            }
            finally
            {
                // Log function exit with elapsed time if available
                if (m_sw == null)
                {
                    if (error)
                        WriteError("[End Func]");
                    else
                        WriteDebug("[End Func]");
                }
                else
                {
                    if (error)
                        WriteErrorFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                    else
                        WriteDebugFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                }
            }
        }


        /// <summary>
        /// Logs details about a function call, including parameters, errors, and exceptions.
        /// Handles both debug and error scenarios.
        /// </summary>
        /// <param name="parameters">The parameters passed to the function being logged.</param>
        /// <param name="function">
        /// Optional. The <see cref="MethodBase"/> of the function being logged. If null, the calling function is used.
        /// </param>
        /// <param name="error">Indicates whether the log entry represents an error.</param>
        /// <param name="exception">
        /// Optional. The <see cref="Exception"/> to log if an error occurred.
        /// </param>
#if __IOS__
        public void LogFunction(object parameters, MethodBase function? = null, bool error = false, Exception? exception = null)
#else
        public void LogFunction(object parameters, MethodBase function = null, bool error = false, Exception exception = null)
#endif
        {
            if (!AllowLogging) return;

            try
            {
                if (function == null)
                {
                    // Get calling function where this class is instantiated if not explicitly provided
                    function = (new StackFrame(1, true)).GetMethod();
                }
                Function = function;
                Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
                FullName = $"{Function.DeclaringType.FullName}.{Name}";
                string message = $"[Func: {FullName} ({Function.MemberType})]";

                // Log function entry
                if (error)
                {
                    WriteError(message);
                    if (exception == null)
                        WriteErrorFormat("\tCall Path: {0}", CallPath);
                }
                else
                {
                    WriteDebug(message);
                }

                // Process function parameters
                int debugLevel = DebugPrintLevel[Function.MemberType == MemberTypes.Constructor
                    ? ConfigurationSetting.Log_FunctionHeaderConstructor
                    : ConfigurationSetting.Log_FunctionHeaderMethod];

                bool detailedLogging = DetailedLoggingDetected.Any(p => p.Key >= (debugLevel + 2) && p.Value);
                LoggingUtils.ProcessParametersNoAutoLog(Function.GetParameters(), parameters, error, $"{LogPrefix}\t", detailedLogging ? 0 : debugLevel + 2);

                // Log exception details if present
                if (exception != null)
                {
                    LogExceptionDetails(exception);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error in LogFunction: ", ex);
            }
            finally
            {
                // Log function exit with time elapsed
                if (m_sw == null)
                {
                    if (error) WriteError("[End Func]");
                    else WriteDebug("[End Func]");
                }
                else
                {
                    if (error) WriteErrorFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                    else WriteDebugFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                }
            }
        }

        /// <summary>
        /// Logs detailed exception information, including call path and stack trace.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        private void LogExceptionDetails(Exception exception)
        {
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tException Error: {0}", $"{exception.GetType().Name}: {exception.Message}");
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tCall Path: {0}", CallPath);
            WriteError($"\tSource: {exception.Source}");
            WriteError($"\tTargetSite: {exception.TargetSite}");

            foreach (string item in GetAllFootprints(exception))
            {
                WriteError($"\t{item}");
            }

            WriteError("\t" + new string('*', 120));
        }

        /// <summary>
        /// Logs details about a function call, including parameters and exceptions if provided.
        /// Handles logging for both debug and error scenarios.
        /// </summary>
        /// <param name="parameters">The parameters passed to the function being logged.</param>
        /// <param name="function">
        /// Optional. The <see cref="MethodBase"/> of the function being logged. If null, the caller function is used.
        /// </param>
        /// <param name="error">Indicates whether the log entry represents an error.</param>
        /// <param name="exception">
        /// Optional. A <see cref="System.Net.WebException"/> to include in the log if an error occurred.
        /// </param>
#if __IOS__
        public void LogFunction(object parameters, MethodBase function? = null, bool error = false, System.Net.WebException? exception = null)
#else
        public void LogFunction(object parameters, MethodBase function = null, bool error = false, System.Net.WebException exception = null)
#endif
        {
            if (!AllowLogging) return;

            try
            {
                if (function == null)
                {
                    // Get calling function where this class is instantiated if not explicitly provided
                    function = (new StackFrame(1, true)).GetMethod();
                }
                Function = function;
                Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
                FullName = $"{Function.DeclaringType.FullName}.{Name}";
                string message = $"[Func: {FullName} ({Function.MemberType})]";

                // Write initial log
                if (error) WriteError(message);
                else WriteDebug(message);

                // Process function parameters
                int debugLevel = DebugPrintLevel[Function.MemberType == MemberTypes.Constructor
                    ? ConfigurationSetting.Log_FunctionHeaderConstructor
                    : ConfigurationSetting.Log_FunctionHeaderMethod];

                var additionalDebug = DetailedLoggingDetected.Any(p => p.Key >= debugLevel + 2 && p.Value);
                LoggingUtils.ProcessParametersNoAutoLog(Function.GetParameters(), parameters, error, $"{LogPrefix}\t", additionalDebug ? 0 : debugLevel + 2);

                // Handle exception logging
                if (exception != null)
                {
                    LogExceptionDetails(exception);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error in LogFunction: ", ex);
            }
            finally
            {
                // Final log entry with elapsed time (if available)
                if (m_sw == null)
                {
                    if (error) WriteError("[End Func]");
                    else WriteDebug("[End Func]");
                }
                else
                {
                    if (error) WriteErrorFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                    else WriteDebugFormat("[End Func]: Time elapsed: {0}", m_sw.Elapsed);
                }
            }
        }

        /// <summary>
        /// Logs detailed exception information, including call path and HTTP response details for protocol errors.
        /// </summary>
        /// <param name="exception">The <see cref="System.Net.WebException"/> to log.</param>
        private void LogExceptionDetails(System.Net.WebException exception)
        {
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tException Error: {0}", $"{exception.GetType().Name}: {exception.Message}");
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tCall Path: {0}", CallPath);
            WriteError($"\tSource: {exception.Source}");
            WriteError($"\tTargetSite: {exception.TargetSite}");

            if (exception.Status == WebExceptionStatus.ProtocolError && exception.Response is HttpWebResponse response)
            {
                WriteError($"\tStatus Code: {response.StatusCode}");
                WriteError($"\tStatus Description: {response.StatusDescription}");
                WriteError($"\tServer: {response.Server}");
                WriteError($"\tMethod: {response.Method}");
                WriteError($"\tResponse Uri: {response.ResponseUri.OriginalString}");
            }

            foreach (string item in GetAllFootprints(exception))
            {
                WriteError($"\t{item}");
            }
            WriteError("\t" + new string('*', 120));
        }

#if __IOS__
        public void WriteLog(string message, Exception? exception = null)
#else
        public void WriteLog(string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsInfoEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteLogPrefixNoAutoLog(LogPrefix, message, exception);
            }
        }
        public void WriteLogFormat(string format, params object[] args)
        {
            if (Logger != null && Logger.IsInfoEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteLogPrefixNoAutoLog(LogPrefix, string.Format(format, args));
            }
        }
#if __IOS__
        public void WriteDebug(string message, Exception? exception = null)
#else
        public void WriteDebug(string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsDebugEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteDebugPrefixNoAutoLog(LogPrefix, message, exception);
            }
        }
#if __IOS__
        public void WriteDebug(int DebugLevel, string message, Exception? exception = null)
#else
        public void WriteDebug(int DebugLevel, string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsDebugEnabled)
            {
                if (AllowLogging)
                {
                    if (DetailedLoggingDetected.Any(p => p.Key >= DebugLevel && p.Value))
                        LoggingUtils.WriteDebugPrefixNoAutoLog(LogPrefix, message, exception);
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(DebugLevel, LogPrefix, message, exception);
                }
            }
        }
        public void WriteDebugFormat(string format, params object[] args)
        {
            if (Logger != null && Logger.IsDebugEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteDebugFormatPrefixNoAutoLog(LogPrefix, format, args);
            }
        }
        public void WriteDebugFormat(int DebugLevel, string format, params object[] args)
        {
            if (Logger != null && Logger.IsDebugEnabled)
            {
                if (AllowLogging)
                {
                    if (DetailedLoggingDetected.Any(p => p.Key >= DebugLevel && p.Value))
                        LoggingUtils.WriteDebugFormatPrefixNoAutoLog(LogPrefix, format, args);
                    else
                        LoggingUtils.WriteDebugFormatPrefixNoAutoLog(DebugLevel, LogPrefix, format, args);
                }
            }
        }

#if __IOS__
        public void WriteWarn(string message, Exception? exception = null)
#else
        public void WriteWarn(string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsWarnEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteWarnPrefixNoAutoLog(LogPrefix, message, exception);
            }
        }
        public void WriteWarnFormat(string format, params object[] args)
        {
            if (Logger != null && Logger.IsWarnEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteWarnPrefixNoAutoLog(LogPrefix, string.Format(format, args));
            }
        }
#if __IOS__
        public void WriteError(string message, Exception? exception = null)
#else
        public void WriteError(string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsErrorEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(LogPrefix, message, exception);
            }
        }
        public void WriteErrorFormat(string format, params object[] args)
        {
            if (Logger != null && Logger.IsErrorEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteErrorFormatNoAutoLog(LogPrefix + format, args);
            }
        }
#if __IOS__
        public void WriteFatal(string message, Exception? exception = null)
#else
        public void WriteFatal(string message, Exception exception = null)
#endif
        {
            if (Logger != null && Logger.IsFatalEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteFatalPrefixNoAutoLog(LogPrefix, message, exception);
            }
        }
        public void WriteFatalFormat(string format, params object[] args)
        {
            if (Logger != null && Logger.IsFatalEnabled)
            {
                if (AllowLogging)
                    LoggingUtils.WriteFatalFormatNoAutoLog(LogPrefix + format, args);
            }
        }
        // public static void WriteErrorFormat(string format, params object[] args)
        private void ProcessStopWatchNoAutoLog(ref Stopwatch sw, string functionName, string message = "")
        {
            if (!AllowLogging)
                return;
            string logPrefix = "";
            try
            {
                if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= LoggingUtils.Logger?.AutoLogSQLThreshold)
                    {
                        if (logPrefix == "")
                            logPrefix = LogPrefix;
                        LoggingUtils.Logger?.Warn(logPrefix + "+" + new string('-', 79));
                        LoggingUtils.Logger?.WarnFormat("{0}+   Function: [{1}] - {2} Exceeded Time Threashold of [{3}] minutes - Actual [{4}] minutes.", logPrefix, functionName, (message.Length > 0 ? "HTTP Query" : ""), LoggingUtils.Logger?.AutoLogSQLThreshold, sw?.Elapsed);
                        LoggingUtils.Logger?.Warn(logPrefix + "+" + new string('-', 79));
                        if (message.Length > 0)
                        {
                            LoggingUtils.Logger?.Warn(logPrefix + "+" + new string('-', 79));
                            LoggingUtils.Logger?.Warn(logPrefix + "+   Web Call Information: " + message);
                            LoggingUtils.Logger?.Warn(logPrefix + "+" + new string('-', 79));
                        }
                    }
                    else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        LoggingUtils.Logger?.Warn("+" + new string('-', 79));
                        LoggingUtils.Logger?.WarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        LoggingUtils.Logger?.Warn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        LoggingUtils.Logger?.Warn("+" + new string('-', 79));
                        LoggingUtils.Logger?.WarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        LoggingUtils.Logger?.Warn("+" + new string('-', 79));
                    }
                }
                switch (Function.MemberType)
                {
                    case MemberTypes.Method:
                    case MemberTypes.Property:
                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value) || LoggingUtils.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                    case MemberTypes.Constructor:
                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value) || LoggingUtils.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                    default:
                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value) || LoggingUtils.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                LoggingUtils.Logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                }
            }
            catch (Exception exOuter)
            {
                if (logPrefix == "")
                    logPrefix = LogPrefix;
                LoggingUtils.LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { sw, functionName, message, logPrefix }, true, logPrefix);
                LoggingUtils.Logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public void Dispose()
        {
            if (!AllowLogging)
                return;
            if (FunctionDeclarationLogged && !IgnoreLogging.GetOrAdd(Name, false))
            {
                FunctionDeclarationLogged = false;
                if (m_sw == null)
                {
                    if (m_bIsError)
                        WriteError("[End Func]");
                    else
                    {
                        if (m_bSuppressFunctionDeclaration)
                            return;
                        switch (Function.MemberType)
                        {
                            case MemberTypes.Method:
                            case MemberTypes.Property:
                                if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                    WriteDebug("[End Func]");
                                else
                                    WriteDebug(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]");
                                break;
                            case MemberTypes.Constructor:
                                if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value))
                                    WriteDebug("[End Func]");
                                else
                                    WriteDebug(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor], "[End Func]");
                                break;
                            default:
                                WriteDebug(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]");
                                break;
                        }
                    }
                }
                else
                {
                    TimeSpan ts = m_sw.Elapsed;
                    switch (Function.MemberType)
                    {
                        case MemberTypes.Method:
                        case MemberTypes.Property:
                            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                            {
                                if (!m_bSuppressFunctionDeclaration)
                                    ProcessStopWatchNoAutoLog(ref m_sw, FullName);
                            }
                            else
                            {
                                if (m_bIsError)
                                    WriteErrorFormat("[End Func]: Time elapsed: {0}", ts);
                                else
                                {
                                    if (!m_bSuppressFunctionDeclaration)
                                    {
                                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]: Time elapsed: {0}", ts);
                                    }
                                }
                            }
                            break;
                        case MemberTypes.Constructor:
                            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor])
                            {
                                if (!m_bSuppressFunctionDeclaration)
                                    ProcessStopWatchNoAutoLog(ref m_sw, FullName);
                            }
                            else
                            {
                                if (m_bIsError)
                                    WriteErrorFormat("[End Func]: Time elapsed: {0}", ts);
                                else
                                {
                                    if (!m_bSuppressFunctionDeclaration)
                                    {
                                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor], "[End Func]: Time elapsed: {0}", ts);
                                    }
                                }
                            }
                            break;
                        default:
                            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                            {
                                if (!m_bSuppressFunctionDeclaration)
                                    ProcessStopWatchNoAutoLog(ref m_sw, FullName);
                            }
                            else
                            {
                                if (m_bIsError)
                                    WriteErrorFormat("[End Func]: Time elapsed: {0}", ts);
                                else
                                {
                                    if (!m_bSuppressFunctionDeclaration)
                                    {
                                        if (DetailedLoggingDetected.Any(p => p.Key >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]: Time elapsed: {0}", ts);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            m_sw?.Stop();
            if (!string.IsNullOrEmpty(CallPath))
            {
                if (ApplicationSettings.AutoLogActivity.TryRemove(CallPath, out Guid guidInstance))
                {

                }
            }
        }
        /// <summary>
        /// Return unique Int64 value for input string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static Int64 GetInt64HashCode(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                byte[] byteContents = Encoding.Unicode.GetBytes(text);
                return GetInt64HashCode(byteContents);
            }
            return 0;
        }
        static Int64 GetInt64HashCode(byte[] contents)
        {
            Int64 hashCode = 0;
            if (contents.Length > 0)
            {
                // Unicode Encode Covering all characterset
                System.Security.Cryptography.SHA256 hash =
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
                byte[] hashText = hash.ComputeHash(contents);
                // 32Byte hashText separate
                // hashCodeStart = 0~7  8Byte
                // hashCodeMedium = 8~23  8Byte
                // hashCodeEnd = 24~31  8Byte
                // and Fold
                Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
                Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
                Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);
                hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            }
            return hashCode;
        }
    }
}
