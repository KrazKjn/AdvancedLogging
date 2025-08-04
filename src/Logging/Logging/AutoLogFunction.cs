using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Loggers;
using AdvancedLogging.Utilities;
using AdvancedLogging.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using AdvancedLogging.Logging.Interfaces;
using System.Linq;

namespace AdvancedLogging.Logging
{
    public class AutoLogFunction : IDisposable
    {
        private readonly bool m_bIsError = false;
        private bool m_bToLog = true;
        private bool m_bSuppressFunctionDeclaration = false;
        private Stopwatch m_sw = new Stopwatch();
        private MethodBase m_function = null;
        private readonly object m_parameters = null;
        private Guid m_guidInstanceId;
        private int m_iTabs = 0;
        private string m_strLogPrefix = "";
        private string m_strCallPath = "";
        private string m_strName = "";
        private string m_strFullName = "";
        private string m_strDynamicLoggingNotice = "<-- DYNAMIC LOGGING --> ";
        private bool m_bFunctionDeclarationLogged = false;
        private bool m_bFunctionParametersLogged = false;
        private bool m_bMemberTypeInformation = true;
        private ConcurrentDictionary<int, bool> m_dicDetailedLoggingDetected = new ConcurrentDictionary<int, bool>();

        private readonly ICommonLogger _logger;
        private readonly ILoggingContext _loggingContext;

        public ICommonLogger Logger => _logger;

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
                    ? LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                    : LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                bool shouldLog = DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value) ||
                                 (_logger?.LogLevel ?? 0) >= debugLevel;

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
                        Function = (new StackFrame(2, true)).GetMethod();
                    }
                    if (LoggingUtils.IsRemotingAppender(_logger))
                    {
                        _loggingContext.SetProperty("procnamefull", FullName);
                        _loggingContext.SetProperty("procname", Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name);
                    }
                    m_strCallPath = Loggers.Log4NetLogger.FunctionFullPath(new StackTrace(2).GetFrames().ToArray());
                }
                return m_strCallPath;
            }
            set
            {
                m_strCallPath = value;
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
        public string DynamicLoggingNotice
        {
            get { return m_strDynamicLoggingNotice; }
            set { m_strDynamicLoggingNotice = value; }
        }
        private void Initialization()
        {
            if (_logger != null)
            {
                MemberTypeInformation = _logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_MemberTypeInformation];
                DynamicLoggingNotice = _logger.MonitoredSettings.GetOrAdd(ConfigurationSetting.Log_DynamicLoggingNotice, DynamicLoggingNotice);
            }
        }

        public AutoLogFunction(ICommonLogger logger, ILoggingContext loggingContext, object parameters, MethodBase function = null, AutoLogFunction _Parent = null, bool error = false, bool bSuppressFunctionDeclaration = false)
        {
            _logger = logger;
            _loggingContext = loggingContext;

            if (!LoggingUtils.AllowLogging || (!error && bSuppressFunctionDeclaration) || !WillWriteToLog(error))
                return;

            m_sw?.Start();
            Function = function ?? new StackFrame(1, true).GetMethod();
            m_parameters = parameters;
            m_bIsError = error;
            m_bSuppressFunctionDeclaration = bSuppressFunctionDeclaration;

            Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
            if (LoggingUtils.IgnoreLogging.GetOrAdd(Name, false))
                return;

            Initialization();
            FullName = $"{Function.DeclaringType.FullName}.{Name}";

            _loggingContext.PushProperty("NDC", FullName);

            try
            {
                StackTrace st = new StackTrace();
                StackFrame[] arrFrames = st.GetFrames();
                CallPath = Loggers.Log4NetLogger.FunctionFullPath(arrFrames.Skip(1).ToArray());

                if (LoggingUtils.IsRemotingAppender(_logger))
                {
                    _loggingContext.SetProperty("procname", Name);
                    _loggingContext.SetProperty("procnamefull", FullName);
                }

                ApplicationSettings.AutoLogActivity.AddOrUpdate(CallPath, GuidInstanceId, (key, oldValue) => GuidInstanceId);

                Tabs = _Parent == null
                    ? ApplicationSettings.AutoLogActivity.Count(p => CallPath.StartsWith(p.Key.Replace(".AutoLogFunction", ""))) - 1
                    : _Parent.Tabs + 1;

                LogPrefix = Tabs > 0 ? new string('\t', Tabs) : string.Empty;

                string message = $"[Func: {FullName}{(m_bMemberTypeInformation ? $" ({Function.MemberType})" : "")}]";

                // Handle dynamic logging
                if (_logger != null &&
                    _logger.ToLog(_logger.LogLevel, out string detectedCriteria, out _, out int detectedLevel))
                {
                    if (!string.IsNullOrEmpty(detectedCriteria))
                    {
                        message = $"[Func: {FullName} ({Function.MemberType}) {DynamicLoggingNotice}[{detectedCriteria}]]";
                        DetailedLoggingDetected.AddOrUpdate(detectedLevel, true, (key, oldValue) => true);
                    }
                }

                FunctionDeclarationLogged = WriteToLog(message, error);

                int debugLevel = LoggingUtils.DebugPrintLevel[Function.MemberType == MemberTypes.Constructor
                    ? ConfigurationSetting.Log_FunctionHeaderConstructor
                    : ConfigurationSetting.Log_FunctionHeaderMethod];

                // Log call path
                if (error || (_logger?.LogLevel ?? 0) >= debugLevel + 1)
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
                        _logger,
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

        private bool WillWriteToLog(bool error)
        {
            if (!LoggingUtils.AllowLogging) return false;

            if (error)
            {
                return _logger?.IsErrorEnabled ?? false;
            }

            if (_logger == null || !_logger.IsDebugEnabled) return false;

            if (Function != null)
            {
                if (_logger.ToLog(_logger.LogLevel, out _, out _, out _))
                {
                    return true;
                }

                int debugLevel = Function.MemberType == MemberTypes.Constructor
                    ? LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                    : LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

                return DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value) ||
                       _logger?.LogLevel >= debugLevel;
            }

            return false;
        }

        private bool WriteToLog(string message, bool error)
        {
            if (!LoggingUtils.AllowLogging) return false;

            if (error)
            {
                WriteError(message);
                return true;
            }

            int debugLevel = Function.MemberType == MemberTypes.Constructor
                ? LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor]
                : LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod];

            bool detailedLogging = DetailedLoggingDetected.Any(p => p.Key >= debugLevel && p.Value);

            if (detailedLogging)
            {
                WriteDebug(message);
                return true;
            }

            WriteDebug(debugLevel, message);
            return _logger?.LogLevel >= debugLevel;
        }

        public void LogFunction(MethodBase function = null, bool error = false, Exception exception = null)
        {
            if (!LoggingUtils.AllowLogging) return;

            try
            {
                if (function == null)
                {
                    function = (new StackFrame(1, true)).GetMethod();
                }
                Function = function;
                Name = Function.MemberType == MemberTypes.Constructor ? Function.DeclaringType.Name : Function.Name;
                FullName = $"{Function.DeclaringType.FullName}.{Name}";
                string message = $"[Func: {FullName} ({Function.MemberType})]";

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

        private void LogExceptionDetails(Exception exception)
        {
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tException Error: {0}", $"{exception.GetType().Name}: {exception.Message}");
            WriteError("\t" + new string('*', 120));
            WriteErrorFormat("\tCall Path: {0}", CallPath);
            WriteError($"\tSource: {exception.Source}");
            WriteError($"\tTargetSite: {exception.TargetSite}");

            foreach (string item in LoggingUtils.GetAllFootprints(exception))
            {
                WriteError($"\t{item}");
            }

            WriteError("\t" + new string('*', 120));
        }

        public void WriteLog(string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsInfoEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteLogPrefixNoAutoLog(_logger, LogPrefix, message, exception);
            }
        }
        public void WriteLogFormat(string format, params object[] args)
        {
            if (_logger != null && _logger.IsInfoEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteLogPrefixNoAutoLog(_logger, LogPrefix, string.Format(format, args));
            }
        }

        public void WriteDebug(string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsDebugEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteDebugPrefixNoAutoLog(_logger, LogPrefix, message, exception);
            }
        }

        public void WriteDebug(int DebugLevel, string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsDebugEnabled)
            {
                if (LoggingUtils.AllowLogging)
                {
                    if (DetailedLoggingDetected.Any(p => p.Key >= DebugLevel && p.Value))
                        LoggingUtils.WriteDebugPrefixNoAutoLog(_logger, LogPrefix, message, exception);
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(_logger, DebugLevel, LogPrefix, message, exception);
                }
            }
        }
        public void WriteDebugFormat(string format, params object[] args)
        {
            if (_logger != null && _logger.IsDebugEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteDebugFormatPrefixNoAutoLog(_logger, LogPrefix, format, args);
            }
        }
        public void WriteDebugFormat(int DebugLevel, string format, params object[] args)
        {
            if (_logger != null && _logger.IsDebugEnabled)
            {
                if (LoggingUtils.AllowLogging)
                {
                    if (DetailedLoggingDetected.Any(p => p.Key >= DebugLevel && p.Value))
                        LoggingUtils.WriteDebugFormatPrefixNoAutoLog(_logger, LogPrefix, format, args);
                    else
                        LoggingUtils.WriteDebugFormatPrefixNoAutoLog(_logger, DebugLevel, LogPrefix, format, args);
                }
            }
        }

        public void WriteWarn(string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsWarnEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteWarnPrefixNoAutoLog(_logger, LogPrefix, message, exception);
            }
        }
        public void WriteWarnFormat(string format, params object[] args)
        {
            if (_logger != null && _logger.IsWarnEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteWarnPrefixNoAutoLog(_logger, LogPrefix, string.Format(format, args));
            }
        }

        public void WriteError(string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsErrorEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(_logger, LogPrefix, message, exception);
            }
        }
        public void WriteErrorFormat(string format, params object[] args)
        {
            if (_logger != null && _logger.IsErrorEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteErrorFormatNoAutoLog(_logger, LogPrefix + format, args);
            }
        }

        public void WriteFatal(string message, Exception exception = null)
        {
            if (_logger != null && _logger.IsFatalEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteFatalPrefixNoAutoLog(_logger, LogPrefix, message, exception);
            }
        }
        public void WriteFatalFormat(string format, params object[] args)
        {
            if (_logger != null && _logger.IsFatalEnabled)
            {
                if (LoggingUtils.AllowLogging)
                    LoggingUtils.WriteFatalFormatNoAutoLog(_logger, LogPrefix + format, args);
            }
        }

        private void ProcessStopWatchNoAutoLog(ref Stopwatch sw, string functionName, string message = "")
        {
            if (!LoggingUtils.AllowLogging)
                return;
            string logPrefix = "";
            try
            {
                if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= _logger?.AutoLogSQLThreshold)
                    {
                        if (logPrefix == "")
                            logPrefix = LogPrefix;
                        _logger?.Warn(logPrefix + "+" + new string('-', 79));
                        _logger?.WarnFormat("{0}+   Function: [{1}] - {2} Exceeded Time Threashold of [{3}] minutes - Actual [{4}] minutes.", logPrefix, functionName, (message.Length > 0 ? "HTTP Query" : ""), _logger?.AutoLogSQLThreshold, sw?.Elapsed);
                        _logger?.Warn(logPrefix + "+" + new string('-', 79));
                        if (message.Length > 0)
                        {
                            _logger?.Warn(logPrefix + "+" + new string('-', 79));
                            _logger?.Warn(logPrefix + "+   Web Call Information: " + message);
                            _logger?.Warn(logPrefix + "+" + new string('-', 79));
                        }
                    }
                    else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        _logger?.Warn("+" + new string('-', 79));
                        _logger?.WarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        _logger?.Warn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        _logger?.Warn("+" + new string('-', 79));
                        _logger?.WarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        _logger?.Warn("+" + new string('-', 79));
                    }
                }
                switch (Function.MemberType)
                {
                    case MemberTypes.Method:
                    case MemberTypes.Property:
                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value) || _logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                _logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                _logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                    case MemberTypes.Constructor:
                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value) || _logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                _logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                _logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                    default:
                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value) || _logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
                            if (logPrefix == "")
                                logPrefix = LogPrefix;
                            if (sw == null)
                                _logger?.DebugFormat(logPrefix + "[End Func]");
                            else
                                _logger?.DebugFormat(logPrefix + "[End Func]: Time elapsed: {0}", sw.Elapsed);
                        }
                        break;
                }
            }
            catch (Exception exOuter)
            {
                if (logPrefix == "")
                    logPrefix = LogPrefix;
                LoggingUtils.LogFunctionNoAutoLog(_logger, _loggingContext, System.Reflection.MethodBase.GetCurrentMethod(), new { sw, functionName, message, logPrefix }, true, logPrefix);
                _logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public void Dispose()
        {
            if (!LoggingUtils.AllowLogging)
                return;
            if (FunctionDeclarationLogged && !LoggingUtils.IgnoreLogging.GetOrAdd(Name, false))
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
                                if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                    WriteDebug("[End Func]");
                                else
                                    WriteDebug(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]");
                                break;
                            case MemberTypes.Constructor:
                                if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value))
                                    WriteDebug("[End Func]");
                                else
                                    WriteDebug(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor], "[End Func]");
                                break;
                            default:
                                WriteDebug(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]");
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
                            if (_logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
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
                                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]: Time elapsed: {0}", ts);
                                    }
                                }
                            }
                            break;
                        case MemberTypes.Constructor:
                            if (_logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor])
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
                                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor], "[End Func]: Time elapsed: {0}", ts);
                                    }
                                }
                            }
                            break;
                        default:
                            if (_logger?.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
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
                                        if (DetailedLoggingDetected.Any(p => p.Key >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] && p.Value))
                                            WriteDebugFormat("[End Func]: Time elapsed: {0}", ts);
                                        else
                                            WriteDebugFormat(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], "[End Func]: Time elapsed: {0}", ts);
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

            _loggingContext.PopProperty("NDC");
        }

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
                System.Security.Cryptography.SHA256 hash =
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
                byte[] hashText = hash.ComputeHash(contents);
                Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
                Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
                Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);
                hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            }
            return hashCode;
        }
    }
}
