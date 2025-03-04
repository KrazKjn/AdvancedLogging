using AdvancedLogging.Constants;
using AdvancedLogging.Extensions;
using AdvancedLogging.Loggers;
using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AdvancedLogging.Utilities
{
    public class LoggingUtils
    {
        private enum DebugPrintLevels
        {
            FunctionHeaderMethod = 4, // At this Level, we Print the Function Header, the Parameter Names, Types, and Values if the Value type is a Simple Data Type (i.e., Int, String, etc)
            // (5 - 7 for levels of debug)
            SqlCommand = 8,
            SqlCommandResults = 8,
            SqlParameters = 8,
            ComplexParameterValues = 12,
            FunctionHeaderConstructor = 16, // At this Level, we Print the Constructor Header, the Parameter Names, Types, and Values if the Value type is a Simple Data Type (i.e., Int, String, etc)
            // (16 - 19 for levels of debug)
            MemberTypeInformation = 20,
            DumpComplexParameterValues = 24,
            DebugDumpSQL = 100
        }

        public static int MaxFunctionTimeThreshold { get; set; } = 120;
        public static bool AllowLogging { get; set; } = true;
        public static bool EnableDebugCode { get; set; } = false;
        public static ConcurrentDictionary<string, bool> IgnoreLogging { get; } = new ConcurrentDictionary<string, bool>();

        private static readonly Lazy<ConcurrentDictionary<string, int>> _debugPrintLevel = new Lazy<ConcurrentDictionary<string, int>>(() =>
        {
            var debugPrintLevel = new ConcurrentDictionary<string, int>();
            debugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] = (int)DebugPrintLevels.FunctionHeaderMethod;
            debugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] = (int)DebugPrintLevels.FunctionHeaderConstructor;
            debugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues] = (int)DebugPrintLevels.ComplexParameterValues;
            debugPrintLevel[ConfigurationSetting.Log_SqlCommand] = (int)DebugPrintLevels.SqlCommand;
            debugPrintLevel[ConfigurationSetting.Log_SqlParameters] = (int)DebugPrintLevels.SqlParameters;
            debugPrintLevel[ConfigurationSetting.Log_SqlCommandResults] = (int)DebugPrintLevels.SqlCommandResults;
            debugPrintLevel[ConfigurationSetting.Log_MemberTypeInformation] = (int)DebugPrintLevels.MemberTypeInformation;
            debugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues] = (int)DebugPrintLevels.DumpComplexParameterValues;
            debugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL] = (int)DebugPrintLevels.DebugDumpSQL;
            return debugPrintLevel;
        });

        public static ConcurrentDictionary<string, int> DebugPrintLevel => _debugPrintLevel.Value;

#if __IOS__
        private static ILoggerUtility? _loggerUtility;
        public static ILoggerUtility? LoggerUtility
#else
        private static ILoggerUtility _loggerUtility;
        public static ILoggerUtility LoggerUtility
#endif		
        {
            get { return _loggerUtility; }

            set { _loggerUtility = value; }
        }

        private static ICommonLogger _log = null;

        public static ICommonLogger Logger
        {
            get { return _log; }

            set
            {
                _log = value;
#if __IOS__
                m_bRemotingLogging = false;
#else
                _remotingLogging = _log.IsRemoting;
#endif
            }
        }

        private static bool _remotingLogging = false;
        public static bool IsRemotingAppender
        {
            get
            {
                return _remotingLogging;
            }
        }

        public static bool IsLoggingToConsole => _log?.IsLoggingToConsole ?? false;

        public static bool IsLoggingToDebugWindow => _log?.IsLoggingToDebugWindow ?? false;

        private static int _consoleStatus = -1;
        public static bool ConsoleAttached
        {
            get
            {
                if (_consoleStatus == -1)
                {
                    try
                    {
                        _consoleStatus = Console.WindowHeight;
                        _consoleStatus = 1;
                    }
                    catch (Exception ex)
                    {
                        if (ShouldLogToDebugWindow())
                            Debug.WriteLine(ex.Message);
                        _consoleStatus = 0;
                    }
                }
                return _consoleStatus == 1;
            }
        }

        public static bool ShouldLogToConsole()
        {
            return !LoggingUtils.IsLoggingToConsole && ConsoleAttached && ApplicationSettings.LogToConsole;
        }

        public static bool ShouldLogToDebugWindow()
        {
            return !LoggingUtils.IsLoggingToDebugWindow && ApplicationSettings.LogToDebugWindow;
        }

        public static void LogFunction(MethodBase function, object parameters, bool error = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { function, parameters, error, logPrefix }))
            {
                LogFunctionNoAutoLog(function, parameters, error, logPrefix);
            }
        }
#if __IOS__
        public static void LogFunctionNoAutoLog(MethodBase function, object parameters, bool error = false, string logPrefix = "", Exception? exception = null)
#else
        public static void LogFunctionNoAutoLog(MethodBase function, object parameters, bool error = false, string logPrefix = "", Exception exception = null)
#endif
        {
            try
            {
                string message = logPrefix;
                StackTrace st = new StackTrace();
                StackFrame[] arrFrames = st.GetFrames();
                string CallPath = Log4NetLogger.FunctionFullPath(arrFrames.Skip(1).ToArray());

                ParameterInfo[] pars = function.GetParameters();
                message += string.Format("[Func: {0}", function.DeclaringType.FullName + " (" + function.MemberType.ToString() + ")]");
                if (error)
                    LoggingUtils.WriteError(message);
                else
                {
                    Logger?.Debug(message);
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                }
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                if (error)
                {
                    if (exception == null)
                    {
                        Logger?.ErrorFormat("\tCall Path: {0}", CallPath);
                    }
                }
                ProcessParameters(pars, parameters, error, logPrefix);
                if (exception != null)
                {
                    WriteError("\t" + new string('*', 120));
                    WriteErrorFormat("\tException Error: {0}", exception.GetType().Name + ": " + exception.Message);
                    WriteError("\t" + new string('*', 120));
                    WriteErrorFormat("\tCall Path: {0}", CallPath);
                    WriteError("\tSource: " + exception.Source);
                    WriteError("\tTargetSite: " + exception.TargetSite);
                    // {"Attempted to divide by zero."}
                    foreach (string strItem in GetAllFootprints(exception))
                    {
                        WriteError("\t" + strItem);
                    }
                    WriteError("\t" + new string('*', 120));
                }
            }
            catch (Exception ex)
            {
                LoggingUtils.WriteError(logPrefix + "Error in LogFunction: ", ex);
                if (ShouldLogToConsole())
                    Console.WriteLine(logPrefix + string.Format("Error in LogFunction: {0}", ex));
            }
            finally
            {
                if (error)
                    LoggingUtils.WriteError("[End Func]");
                else
                {
                    Logger?.Debug("[End Func]");
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine("[End Func]");
                }
                if (ShouldLogToConsole())
                    Console.WriteLine("[End Func]");
            }
        }
        public static void ProcessParameters(ParameterInfo[] pars, object parameters, bool error = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { pars, parameters, error, logPrefix }))
            {
                ProcessParametersNoAutoLog(pars, parameters, error, logPrefix);
            }
        }
        private static bool PrintItForProcessParametersNoAutoLog(int debugLevel, string logPrefix, string message, bool error)
        {
            if (error)
            {
                WriteErrorPrefixNoAutoLog(logPrefix, message);
                return true;
            }
            else
            {
                if (debugLevel == 0)
                {
                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                    return true;
                }
                else
                    return WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
            }
        }
        public static void ProcessParametersNoAutoLog(ParameterInfo[] pars, object parameters, bool error = false, string logPrefix = "", int debugLevel = 4)
        {
            if (pars == null || pars.Length == 0)
            {
                throw new ArgumentOutOfRangeException("Parameter [pars] is empty.");
            }
            int i = 0;
            string message = "";
            foreach (PropertyInfo pi in parameters.GetType().GetProperties())
            {
                string name = pars[i].ParameterType.Name;
                string fullName = pars[i].ParameterType.FullName ?? pars[i].ParameterType.Name;
                System.Type type = pars[i].ParameterType;

                while (pars[i].IsOut && i < pars.Length)
                {
                    message = string.Format("({0}){1} N/A (Out)", pars[i].ParameterType.Name, pars[i].Name);
                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                    i++;
                    if (i < pars.Length)
                    {
                        name = pars[i].ParameterType.Name;
                        fullName = pars[i].ParameterType.FullName ?? pars[i].ParameterType.Name;
                        type = pars[i].ParameterType;
                    }
                }

                List<string> valueTypes = new List<string>();
                bool isArray = false;
                int dimensions = 0;
#if DEBUG
                if (ApplicationSettings.Logger?.LogLevel >= debugLevel && ShouldLogToDebugWindow())
                    Debug.WriteLine((logPrefix == "" ? "-> " : logPrefix) + fullName);
#endif
                if (fullName.EndsWith("&"))
                {
                    fullName = fullName.Replace("&", "");
                }
                if (fullName.EndsWith("[]"))
                {
#if DEBUG
                    isArray = pars[i].ParameterType.IsArray;
#endif
                    isArray = true;
                    fullName = fullName.Replace("[]", "");
                }
                if (fullName.Contains('`') && fullName.Contains("[["))
                {
                    // "System.Collections.Generic.List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]"
#if DEBUG
                    isArray = pars[i].ParameterType.IsArray;
#endif
                    isArray = true;
                    string[] items = fullName.Split('`');
                    fullName = items[0];
                    string temp = items[1].Substring(0, items[1].IndexOf("["));
                    dimensions = int.Parse(temp);
                    temp = items[1].Substring(items[1].IndexOf("[") + 1);
                    temp = temp.Substring(0, temp.Length - 2);
                    items = temp.Split(']');
                    foreach (string item in items)
                    {
                        valueTypes.Add(item.Split('[')[1].Split(',')[0]);
                    }
                }
                if (pars[i].ParameterType.Name.Contains('`'))
                {
                    name = pars[i].ParameterType.Name.Split('`')[0];
                }
                if (fullName == "System.Nullable" && isArray && dimensions == 1)
                {
                    isArray = false;
                    fullName = valueTypes[0];
                }
                switch (fullName)
                {
                    case "System.Data.SqlClient.SqlCommand":
                        message = string.Format("({0}){1}: {2}", i < pars.Length ? name : "???", pi.Name, ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_SqlParameters] ? "(See Command/Parameters/Values Below)" : "(See Command Below.  Set LogLevel >= " + DebugPrintLevel[ConfigurationSetting.Log_SqlParameters].ToString() + " for Parameters/Values)");
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error))
                        {
                            System.Data.SqlClient.SqlCommand sc = (System.Data.SqlClient.SqlCommand)pi.GetValue(parameters, null);
                            if (sc == null)
                            {
                                message = string.Format("\t{0}", LogFormats.NULL_TEXT);
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                sc.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    case "System.Collections.Generic.List":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            switch (valueTypes[0])
                            {
                                case "System.String":
                                    {
                                        List<string> values = (List<string>)pi.GetValue(parameters, null);
                                        if (values == null)
                                        {
                                            message = $"\t{LogFormats.NULL_TEXT}";
                                            if (error)
                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                            else
                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                        }
                                        else
                                        {
                                            foreach (string value in values)
                                            {
                                                message = string.Format("\t{0}", value);
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                        }
                                    }
                                    break;
                                case "System.Int":
                                    {
                                        List<int> values = (List<int>)pi.GetValue(parameters, null);
                                        if (values == null)
                                        {
                                            message = $"\t{LogFormats.NULL_TEXT}";
                                            if (error)
                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                            else
                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                        }
                                        else
                                        {
                                            foreach (int value in values)
                                            {
                                                message = string.Format("\t{0}", value.ToString());
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "System.Collections.Generic.SortedDictionary":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            switch (valueTypes[0])
                            {
                                case "System.String":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    SortedDictionary<string, string> values = (SortedDictionary<string, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    SortedDictionary<string, int> values = (SortedDictionary<string, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case "System.Int":
                                case "System.Int32":
                                case "System.Int64":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    SortedDictionary<int, string> values = (SortedDictionary<int, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    SortedDictionary<int, int> values = (SortedDictionary<int, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "System.Collections.Generic.Dictionary":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            switch (valueTypes[0])
                            {
                                case "System.String":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    Dictionary<string, string> values = (Dictionary<string, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    Dictionary<string, int> values = (Dictionary<string, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case "System.Int":
                                case "System.Int32":
                                case "System.Int64":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    Dictionary<int, string> values = (Dictionary<int, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    Dictionary<int, int> values = (Dictionary<int, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "System.Collections.Concurrent.ConcurrentDictionary":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            switch (valueTypes[0])
                            {
                                case "System.String":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    ConcurrentDictionary<string, string> values = (ConcurrentDictionary<string, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    ConcurrentDictionary<string, int> values = (ConcurrentDictionary<string, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case "System.Int":
                                case "System.Int32":
                                case "System.Int64":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    ConcurrentDictionary<int, string> values = (ConcurrentDictionary<int, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    ConcurrentDictionary<int, int> values = (ConcurrentDictionary<int, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "System.Collections.Generic.SortedList":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            switch (valueTypes[0])
                            {
                                case "System.String":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    SortedList<string, string> values = (SortedList<string, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    SortedList<string, int> values = (SortedList<string, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case "System.Int":
                                case "System.Int32":
                                case "System.Int64":
                                    {
                                        switch (valueTypes[1])
                                        {
                                            case "System.String":
                                                {
                                                    SortedList<int, string> values = (SortedList<int, string>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                            case "System.Int16":
                                            case "System.Int32":
                                            case "System.Int64":
                                                {
                                                    SortedList<int, int> values = (SortedList<int, int>)pi.GetValue(parameters, null);
                                                    if (values == null)
                                                    {
                                                        message = $"\t{LogFormats.NULL_TEXT}";
                                                        if (error)
                                                            WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                        else
                                                            WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                    }
                                                    else
                                                    {
                                                        foreach (var value in values)
                                                        {
                                                            message = string.Format("\t{0} - {1}", value.Key, value.Value);
                                                            if (error)
                                                                WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                            else
                                                                WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "System.Array":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            if (!(pi.GetValue(parameters, null) is System.Array vArray))
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                foreach (var value in vArray)
                                {
                                    message = string.Format("\t{0}", value);
                                    if (error)
                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                    else
                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                }
                            }
                        }
                        break;
                    case "AdvancedLogging.Logging.ICommonLogger":
                    case "AdvancedLogging.Logging.CommonLogger":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            CommonLogger log = (CommonLogger)pi.GetValue(parameters, null);
                            if (log == null)
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                log.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    case "System.Configuration.Configuration":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                        break;
                    case "System.Data.SqlClient.SqlParameter":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error))
                        {
                            if (isArray)
                            {
                                var sqlParameters = (SqlParameter[])pi.GetValue(parameters, null);
                                if (sqlParameters == null)
                                {
                                    message = $"\t{LogFormats.NULL_TEXT}";
                                    if (error)
                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                    else
                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                }
                                else
                                {
                                    if (sqlParameters.Length == 0)
                                    {
                                        if (error)
                                            WriteErrorPrefixNoAutoLog(logPrefix, "\tParameters: (None)");
                                        else
                                            WriteDebugPrefixNoAutoLog(logPrefix, "\tParameters: (None)");
                                    }
                                    else
                                    {
                                        foreach (SqlParameter item in sqlParameters)
                                        {
                                            if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                                            {
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                            }
                                            else
                                            {
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SqlParameter item = (SqlParameter)pi.GetValue(parameters, null);
                                if (item == null)
                                {
                                    message = $"\t{LogFormats.NULL_TEXT}";
                                    if (error)
                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                    else
                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                }
                                else
                                {
                                    if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                                    {
                                        if (error)
                                            WriteErrorPrefixNoAutoLog(logPrefix, string.Format("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                        else
                                            WriteDebugPrefixNoAutoLog(logPrefix, string.Format("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                    }
                                    else
                                    {
                                        if (error)
                                            WriteErrorPrefixNoAutoLog(logPrefix, string.Format("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                        else
                                            WriteDebugPrefixNoAutoLog(logPrefix, string.Format("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                    }
                                }
                            }
                        }
                        break;
                    case "System.Data.SqlClient.SqlParameterCollection":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                        //System.Data.SqlClient.SqlParameterCollection pc = (System.Data.SqlClient.SqlParameterCollection)pi.GetValue(parameters, null);
                        //if (pc != null)
                        //{
                        //    if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[CLogger.Common_SqlParameters])
                        //    {
                        //        LogSQLData(pc, bSuppressFunctionDeclaration:true);
                        //    }
                        //}
                        break;
                    case "System.Security.Cryptography.X509Certificates.X509CertificateCollection":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            System.Security.Cryptography.X509Certificates.X509CertificateCollection values = (System.Security.Cryptography.X509Certificates.X509CertificateCollection)pi.GetValue(parameters, null);
                            if (values == null)
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                values.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    case "System.Diagnostics.Stopwatch":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            Stopwatch sw = (Stopwatch)pi.GetValue(parameters, null);
                            if (sw == null)
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                sw.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    case "System.Net.BufferAsyncResult":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                        //if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[CLogger.Common_FunctionHeaderMethod])
                        //{
                        //    System.Net.BufferAsyncResult sw = (System.Net.BufferAsyncResult)pi.GetValue(parameters, null);
                        //    if (sw.IsRunning)
                        //    {
                        //        message = string.Format("Elapsed: {0}", sw.Elapsed.ToString());
                        //    }
                        //    else
                        //    {
                        //        message = string.Format("Elapsed: Not Running");
                        //    }
                        //    if (error)
                        //        WriteErrorPrefixNoAutoLog(logPrefix, message);
                        //    else
                        //        WriteDebugPrefixNoAutoLog(logPrefix, message);
                        //}
                        break;
                    case "System.Net.HttpWebRequest":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            System.Net.HttpWebRequest httpRequest = (System.Net.HttpWebRequest)pi.GetValue(parameters, null);
                            if (httpRequest == null)
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                httpRequest.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    case "System.Net.WebRequest":
                        message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                        if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error) && ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues])
                        {
                            System.Net.WebRequest httpRequest = (System.Net.WebRequest)pi.GetValue(parameters, null);
                            if (httpRequest == null)
                            {
                                message = $"\t{LogFormats.NULL_TEXT}";
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                            }
                            else
                            {
                                //
                                // Call LoggingExtensions.Log Extension Method
                                //
                                httpRequest.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    default:
                        if (isArray)
                        {
                            message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                            if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error))
                            {
                                switch (fullName)
                                {
                                    case "System.String":
                                        {
                                            string[] values = (string[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (string value in values)
                                                {
                                                    message = string.Format("\t{0}", value);
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Int16":
                                    case "System.Int32":
                                    case "System.Int64":
                                    case "System.IntPtr":
                                        {
                                            int[] values = (int[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (int value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.UInt16":
                                    case "System.UInt32":
                                    case "System.UInt64":
                                    case "System.UIntPtr":
                                        {
                                            uint[] values = (uint[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (uint value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Double":
                                    case "System.Single":
                                        {
                                            Double[] values = (Double[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (Double value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Boolean":
                                        {
                                            Boolean[] values = (Boolean[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (Boolean value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Byte":
                                        {
                                            Byte[] values = (Byte[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (Byte value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.SByte":
                                        {
                                            SByte[] values = (SByte[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (SByte value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Char":
                                        {
                                            Char[] values = (Char[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (Char value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.Decimal":
                                        {
                                            Decimal[] values = (Decimal[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (Decimal value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    case "System.DateTime":
                                        {
                                            DateTime[] values = (DateTime[])pi.GetValue(parameters, null);
                                            if (values == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                foreach (DateTime value in values)
                                                {
                                                    message = string.Format("\t{0}", value.ToShortDateString() + " " + value.ToShortTimeString());
                                                    PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error);
                                                }
                                            }
                                        }
                                        break;
                                    default:
                                        if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
                                        {
                                            object oitem = (DateTime[])pi.GetValue(parameters, null);
                                            if (oitem == null)
                                            {
                                                message = $"\t{LogFormats.NULL_TEXT}";
                                                if (error)
                                                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                else
                                                    WriteDebugPrefixNoAutoLog(logPrefix, message);
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    message = string.Format("Object Data  : {0}", ObjectDumper.Dump(oitem));
                                                    if (error)
                                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                    else
                                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                }
                                                catch (Exception ex)
                                                {
                                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("Error 'Dumping' object of type [{0}].", fullName), ex);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else
                        {
                            message = string.Format("({0}){1}: {2}", i < pars.Length ? name : "???", pi.Name, pi.GetValue(parameters, null));
                            if (PrintItForProcessParametersNoAutoLog(debugLevel, logPrefix, message, error))
                            {
                                object oitem = pi.GetValue(parameters, null);
                                if (oitem == null)
                                {
                                    message = $"\t{LogFormats.NULL_TEXT}";
                                    if (error)
                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                    else
                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                }
                                else
                                {
                                    if (!IsSimple(oitem.GetType()))
                                    {
                                        bool bLogged = false;
                                        try
                                        {
                                            dynamic dt = oitem.ToType(oitem.GetType());

                                            //
                                            // Call LoggingExtensions.Log Extension Method
                                            //
#if !__IOS__
                                            dt.Log(debugLevel, logPrefix, error);
#endif											
                                        }
                                        catch (NotImplementedException ex)
                                        {
                                            bLogged = (ex.Message == "Custom ToPrint not found!");
                                        }
                                        if (!bLogged)
                                        {
                                            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
                                            {
                                                try
                                                {
                                                    ObjectDumper.Maxlevels = 10;
                                                    message = string.Format("Object Data  : {0}", ObjectDumper.Dump(oitem));
                                                    if (error)
                                                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                                                    else
                                                        WriteDebugPrefixNoAutoLog(logPrefix, message);
                                                }
                                                catch (Exception ex)
                                                {
                                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("Error 'Dumping' object of type [{0}].", fullName), ex);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }

                i++;
            }
        }
        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }
        public static void ProcessStopWatch(ref Stopwatch sw, string functionName, SqlCommand cmd, string logPrefix = "", int iDebugLevel = 4)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sw, functionName, cmd, logPrefix }))
            {
                try
                {
                    if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                    {
                        if (sw?.Elapsed.TotalMinutes >= Logger?.AutoLogSQLThreshold)
                        {
                            if (Logger != null && sw != null)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - SQL Command Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", functionName, Logger.AutoLogSQLThreshold, sw.Elapsed);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+   Database Call Information");
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                LogSQLData(false, functionName, cmd.CommandText, cmd.Parameters, true, logPrefix);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            }
                        }
                        else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else
                    {
                        if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    if (Logger?.LogLevel >= iDebugLevel && sw != null)
                    {
                        if (vAutoLogFunction.FunctionDeclarationLogged)
                            vAutoLogFunction.WriteDebugFormat("Time elapsed: {0}", sw.Elapsed);
                        else
                            vAutoLogFunction.WriteDebugFormat(functionName + "\t: Time elapsed: {0}", sw.Elapsed);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sw, functionName, cmd, logPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static void ProcessStopWatch(ref Stopwatch sw, AutoLogFunction vAutoLogFunction, SqlCommand cmd, int iDebugLevel = 4)
        {
            try
            {
                if (vAutoLogFunction.FullName.ToLower().Contains(".sqlhelper") || vAutoLogFunction.FullName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= Logger?.AutoLogSQLThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - SQL Command Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", vAutoLogFunction.FullName, Logger.AutoLogSQLThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarn("+   Database Call Information");
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        LogSQLData(false, vAutoLogFunction.FullName, cmd.CommandText, cmd.Parameters, true, vAutoLogFunction.LogPrefix);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                    else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                if (Logger?.LogLevel >= iDebugLevel && sw != null)
                {
                    if (vAutoLogFunction.FunctionDeclarationLogged)
                        vAutoLogFunction.WriteDebugFormat("Time elapsed: {0}", sw.Elapsed);
                    else
                        vAutoLogFunction.WriteDebugFormat(vAutoLogFunction.FullName + "\t: Time elapsed: {0}", sw.Elapsed);
                }
            }
            catch (Exception exOuter)
            {
                vAutoLogFunction.LogFunction(new { sw, vAutoLogFunction, cmd, iDebugLevel }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                throw;
            }
        }
        public static void ProcessStopWatch(ref Stopwatch sw, string functionName, string message = "", string logPrefix = "", int iDebugLevel = 4)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sw, functionName, message, logPrefix }))
            {
                try
                {
                    if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                    {
                        if (sw?.Elapsed.TotalMinutes >= Logger?.AutoLogSQLThreshold)
                        {
                            if (Logger != null && sw != null)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] minutes - Actual [{3}] minutes.", functionName, (message.Length > 0 ? "HTTP Query" : ""), Logger.AutoLogSQLThreshold, sw.Elapsed);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                if (message.Length > 0)
                                {
                                    vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                    vAutoLogFunction.WriteWarn("+   Web Call Information: " + message);
                                }
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            }
                        }
                        else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else
                    {
                        if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    if (Logger?.LogLevel >= iDebugLevel && sw != null)
                    {
                        if (vAutoLogFunction.FunctionDeclarationLogged)
                            vAutoLogFunction.WriteDebugFormat("Time elapsed: {0}", sw.Elapsed);
                        else
                            vAutoLogFunction.WriteDebugFormat(functionName + "\t: Time elapsed: {0}", sw.Elapsed);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sw, functionName, message, logPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static void ProcessStopWatch(ref Stopwatch sw, AutoLogFunction vAutoLogFunction, string message = "", int iDebugLevel = 4)
        {
            try
            {
                if (vAutoLogFunction.FullName.ToLower().Contains(".sqlhelper") || vAutoLogFunction.FullName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= Logger?.AutoLogSQLThreshold)
                    {
                        if (Logger != null && sw != null)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   {0} Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", (message.Length > 0 ? "HTTP Query" : ""), Logger.AutoLogSQLThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            if (message.Length > 0)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+   Web Call Information: " + message);
                            }
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= LoggingUtils.MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", LoggingUtils.MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                if (Logger?.LogLevel >= iDebugLevel && sw != null)
                {
                    vAutoLogFunction.WriteDebugFormat("Time elapsed: {0}", sw.Elapsed);
                }
            }
            catch (Exception exOuter)
            {
                vAutoLogFunction.LogFunction(new { sw, message }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                throw;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CurrentMethod"></param>
        /// <param name="ParentFunction"></param>
        /// <param name="cmd"></param>
        /// <param name="ex"></param>
        public static void LogDBError(MethodBase CurrentMethod, MethodBase ParentFunction, SqlCommand cmd, Exception ex, string LogPrefix = "")
        {
            string strParent = CommonLogger.FunctionFullName(ParentFunction);
            using (var vAutoLogFunction = new AutoLogFunction(new { CurrentMethod, ParentFunction, cmd, ex, LogPrefix }, bSuppressFunctionDeclaration: true))
            {
                try
                {

                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Error");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteErrorFormat("{0}: [{1}] - {2}", CommonLogger.FunctionFullName(CurrentMethod), cmd.CommandText, ex.ToString());
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Call Information");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    LoggingUtils.LogSQLData(strParent, cmd.CommandText, cmd.Parameters, true, LogPrefix);
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { CurrentMethod, ParentFunction, cmd, ex, LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="CurrentMethod"></param>
        /// <param name="ParentFunction"></param>
        /// <param name="cmdText"></param>
        /// <param name="ex"></param>
        public static void LogDBError(MethodBase CurrentMethod, MethodBase ParentFunction, string cmdText, Exception ex, string LogPrefix = "")
        {
            string strParent = CommonLogger.FunctionFullName(ParentFunction);
            using (var vAutoLogFunction = new AutoLogFunction(new { CurrentMethod, ParentFunction, cmdText, ex, LogPrefix }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Error");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteErrorFormat("{0}: [{1}] - {2}", CommonLogger.FunctionFullName(CurrentMethod), cmdText, ex.ToString());
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Call Information");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    LoggingUtils.LogSQLData(strParent, cmdText, bForceLogWrite: true, logPrefix: LogPrefix);
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { CurrentMethod, ParentFunction, cmdText, ex, LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogSQLData(string functionName, string strCommand, SqlParameter[] sqlParameters, bool bForceLogWrite = false, string logPrefix = "")
        {
            LogSQLData(true, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix);
        }

        public static void LogSQLData(bool bAutoDetect, string functionName, string strCommand, SqlParameter[] sqlParameters, bool bForceLogWrite = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }))
            {
                try
                {
                    string detectedCriteria = "";
                    string detectedFunction;

                    if (Logger == null)
                        return;
                    if (!bForceLogWrite)
                    {
                        if (!Logger.ToLog(4, out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Comamnd if LogLevel is >= to 4
                            return;
                    }
                    if (vAutoLogFunction.FunctionDeclarationLogged)
                    {
                        if (Logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("Command: [{0}]", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("Command: [{0}]", strCommand);
                    }
                    else
                    {
                        if (Logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                    }
                    if (!bForceLogWrite)
                    {
                        if (!Logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                            return;
                    }
                    if (sqlParameters == null || sqlParameters.Length == 0)
                    {
                        if (Logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebug("No Parameters!");
                        else
                            vAutoLogFunction.WriteWarn("No Parameters!");
                    }
                    else
                    {
                        foreach (SqlParameter item in sqlParameters)
                        {
                            if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

#if __IOS__
        public static void LogSQLData(string functionName, string strCommand, SqlParameterCollection? sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
#else
        public static void LogSQLData(string functionName, string strCommand, SqlParameterCollection sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
#endif
        {
            LogSQLData(true, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix);
        }

#if __IOS__
        public static void LogSQLData(bool bAutoDetect, string functionName, string strCommand, SqlParameterCollection? sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
#else
        public static void LogSQLData(bool bAutoDetect, string functionName, string strCommand, SqlParameterCollection sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }))
            {
                try
                {
                    string detectedCriteria = "";
                    string detectedFunction = "";

                    if (Logger == null)
                        return;
                    if (!bForceLogWrite)
                    {
                        if (bAutoDetect && !Logger.ToLog(4, out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Comamnd if LogLevel is >= to 4
                            return;
                    }
                    if (vAutoLogFunction.FunctionDeclarationLogged)
                    {
                        if (Logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("Command: [{0}]", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("Command: [{0}]", strCommand);
                    }
                    else
                    {
                        if (Logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                    }
                    if (!bForceLogWrite)
                    {
                        if (bAutoDetect && !Logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                            return;
                    }
                    if (sqlParameters == null || sqlParameters.Count == 0)
                    {
                        if (Logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("No Parameters!");
                        else
                            vAutoLogFunction.WriteWarnFormat("No Parameters!");
                    }
                    else
                    {
                        foreach (SqlParameter item in sqlParameters)
                        {
                            if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, SqlDbTypeToString(item), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

#if __IOS__
        public static void LogSQLData(SqlParameterCollection? sqlParameters = null, string strAppendLogPrefix = "", int iTabs = -1, bool bSuppressFunctionDeclaration = false)
#else
        public static void LogSQLData(SqlParameterCollection sqlParameters = null, string strAppendLogPrefix = "", int iTabs = -1, bool bSuppressFunctionDeclaration = false)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlParameters, strAppendLogPrefix }, null, iTabs, bSuppressFunctionDeclaration))
            {
                try
                {
                    if (strAppendLogPrefix.Length > 0)
                        vAutoLogFunction.LogPrefix = strAppendLogPrefix;
                    if (Logger == null)
                        return;
                    if (!Logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out string detectedCriteria, out string detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                        return;
                    if (sqlParameters == null)
                    {
                        if (Logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("No Parameters:");
                        else
                            vAutoLogFunction.WriteWarnFormat("No Parameters:");
                    }
                    else
                    {
                        foreach (SqlParameter item in sqlParameters)
                        {
                            if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (Logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlParameters, strAppendLogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogSQLOutData(SqlParameterCollection sqlParameters, AutoLogFunction vAutoLogFunction)
        {
            if (sqlParameters != null)
            {
                foreach (SqlParameter item in sqlParameters)
                {
                    if (item.Direction == ParameterDirection.Output ||
                        item.Direction == ParameterDirection.InputOutput ||
                        item.Direction == ParameterDirection.ReturnValue)
                    {
                        if (Logger != null && Logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                        else
                            vAutoLogFunction.WriteLogFormat("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                    }
                }
            }
        }

#if __IOS__
        public static void WriteLog(string message, Exception? ex = null)
#else
        public static void WriteLog(string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
            {
                WriteLogNoAutoLog(message, ex);
            }
        }
#if __IOS__
        public static void WriteLogNoAutoLog(string message, Exception? ex = null)
#else
        public static void WriteLogNoAutoLog(string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
#if __IOS__
                if (ex == null)
                    Log.Information(message);
                else
                    Log.Information(ex, message);
#endif
                Logger?.Info(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true);
#if __IOS__
                Log.Debug(exOuter, string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name));
#endif
                Logger?.Debug(string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        //public static void WriteLogPrefix(string logPrefix, string message, Exception ex = null)
        //{
        //    using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
        //    {
        //        try
        //        {
        //            if (ex == null)
        //            {
        //                Debug.WriteLine(logPrefix + message);
        //                if (ConsoleAttached && ApplicationSettings.WriteConsole)
        //                    Console.WriteLine(logPrefix + message);
        //            }
        //            else
        //            {
        //                Debug.WriteLine(logPrefix + message + "; Error: " + ex);
        //                if (ConsoleAttached && ApplicationSettings.WriteConsole)
        //                    Console.WriteLine(logPrefix + message + "; Error: " + ex);
        //            }
        //            Logger?.InfoPrefix(logPrefix, message, ex);
        //        }
        //        catch (Exception exOuter)
        //        {
        //            LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
        //            Logger?.Debug(string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
        //            throw;
        //        }
        //    }
        //}
#if __IOS__
        public static void WriteLogPrefix(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteLogPrefix(string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, message, ex }))
            {
                WriteLogPrefixNoAutoLog(logPrefix, message, ex);
            }
        }
#if __IOS__
        public static void WriteLogPrefixNoAutoLog(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteLogPrefixNoAutoLog(string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
#if __IOS__
                if (ex == null)
                    Log.Information(logPrefix + message);
                else
                    Log.Information(ex, logPrefix + message);
#endif
                Logger?.Info(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, message, ex }, true, exception: exOuter);
#if __IOS__
                Log.Debug(exOuter, string.Format("{0}{1}", logPrefix, System.Reflection.MethodBase.GetCurrentMethod().Name));
#endif
                Logger?.Debug(string.Format("{0}{1}", logPrefix, System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteLogFormat(string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { format, args }))
            {
                WriteLogFormatNoAutoLog(format, args);
            }
        }
        public static void WriteLogFormatNoAutoLog(string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
#if __IOS__
                Log.Information(message);
#endif
                Logger?.Info(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteDebug(string message, Exception? ex = null)
#else
        public static void WriteDebug(string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
            {
                WriteDebugNoAutoLog(message, ex);
            }
        }
#if __IOS__
        public static void WriteDebugNoAutoLog(string message, Exception? ex = null)
#else
        public static void WriteDebugNoAutoLog(string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
                Logger?.Debug(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

#if __IOS__
        public static void WriteDebugPrefix(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteDebugPrefix(string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, message, ex }))
            {
                WriteDebugPrefixNoAutoLog(logPrefix, message, ex);
            }
        }
#if __IOS__
        public static void WriteDebugPrefixNoAutoLog(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteDebugPrefixNoAutoLog(string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                Logger?.Debug(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static bool WriteDebug(int DebugLevel, string message, Exception? ex = null)
#else
        public static bool WriteDebug(int DebugLevel, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { DebugLevel, message, ex }))
            {
                return WriteDebugNoAutoLog(DebugLevel, message, ex);
            }
        }
#if __IOS__
        public static bool WriteDebugNoAutoLog(int DebugLevel, string message, Exception? ex = null)
#else
        public static bool WriteDebugNoAutoLog(int DebugLevel, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
                if (Logger == null)
                    return false;
                else
                {
                    return Logger.Debug(DebugLevel, message, ex);
                }
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, message, ex }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static bool WriteDebugPrefix(int DebugLevel, string logPrefix, string message, Exception? ex = null)
#else
        public static bool WriteDebugPrefix(int DebugLevel, string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { DebugLevel, logPrefix, message, ex }))
            {
                return WriteDebugPrefixNoAutoLog(DebugLevel, logPrefix, message, ex);
            }
        }
#if __IOS__
        public static bool WriteDebugPrefixNoAutoLog(int DebugLevel, string logPrefix, string message, Exception? ex = null)
#else
        public static bool WriteDebugPrefixNoAutoLog(int DebugLevel, string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
#pragma warning disable IDE0075 // Simplify conditional expression
                return Logger == null ? false : Logger.DebugPrefix(DebugLevel, logPrefix, message, ex);
#pragma warning restore IDE0075 // Simplify conditional expression
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteDebugFormatPrefix(string logPrefix, string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, format, args }))
            {
                WriteDebugFormatPrefixNoAutoLog(logPrefix, format, args);
            }
        }
        public static void WriteDebugFormatPrefixNoAutoLog(string logPrefix, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                Logger?.DebugPrefix(logPrefix, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, format, args }, true, logPrefix);
                Logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteDebugFormatPrefix(string logPrefix, string functionName, string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, functionName, format, args }))
            {
                WriteDebugFormatPrefixNoAutoLog(logPrefix, functionName, format, args);
            }
        }
        public static void WriteDebugFormatPrefixNoAutoLog(string logPrefix, string functionName, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                Logger?.DebugPrefix(logPrefix, functionName, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, functionName, format, args }, true, logPrefix);
                Logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static bool WriteDebugFormatPrefix(int DebugLevel, string logPrefix, string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { DebugLevel, logPrefix, format, args }))
            {
                return WriteDebugFormatPrefixNoAutoLog(DebugLevel, logPrefix, format, args);
            }
        }
        public static bool WriteDebugFormatPrefixNoAutoLog(int DebugLevel, string logPrefix, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                return Logger != null && Logger.DebugPrefix(DebugLevel, logPrefix, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, logPrefix, format, args }, true, logPrefix);
                Logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static bool WriteDebugFormatPrefix(int DebugLevel, string logPrefix, string functionName, string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { DebugLevel, logPrefix, functionName, format, args }))
            {
                return WriteDebugFormatPrefixNoAutoLog(DebugLevel, logPrefix, functionName, format, args);
            }
        }
        public static bool WriteDebugFormatPrefixNoAutoLog(int DebugLevel, string logPrefix, string functionName, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                return Logger != null && Logger.DebugPrefix(DebugLevel, logPrefix, functionName, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, logPrefix, functionName, format, args }, true, logPrefix);
                Logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteError(string message, Exception? ex = null)
#else
        public static void WriteError(string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
            {
                WriteErrorNoAutoLog(message, ex);
            }
        }
#if __IOS__
        public static void WriteErrorNoAutoLog(string message, Exception? ex = null)
#else
        public static void WriteErrorNoAutoLog(string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
                Logger?.Error(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteErrorPrefix(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteErrorPrefix(string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, message, ex }))
            {
                WriteErrorPrefixNoAutoLog(logPrefix, message, ex);
            }
        }
#if __IOS__
        public static void WriteErrorPrefixNoAutoLog(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteErrorPrefixNoAutoLog(string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                Logger?.Error(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteErrorFormat(string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { format, args }))
            {
                WriteErrorFormatNoAutoLog(format, args);
            }
        }
        public static void WriteErrorFormatNoAutoLog(string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                Logger?.ErrorFormat(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteFatal(string message, Exception? ex = null)
#else
        public static void WriteFatal(string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
            {
                WriteFatalNoAutoLog(message, ex);
            }
        }
#if __IOS__
        public static void WriteFatalNoAutoLog(string message, Exception? ex = null)
#else
        public static void WriteFatalNoAutoLog(string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
                Logger?.Fatal(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteFatalPrefix(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteFatalPrefix(string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, message, ex }))
            {
                WriteFatalPrefixNoAutoLog(logPrefix, message, ex);
            }
        }
#if __IOS__
        public static void WriteFatalPrefixNoAutoLog(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteFatalPrefixNoAutoLog(string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                Logger?.Error(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteFatalFormat(string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { format, args }))
            {
                WriteFatalFormatNoAutoLog(format, args);
            }
        }
        public static void WriteFatalFormatNoAutoLog(string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                Logger?.ErrorFormat(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteWarn(string message, Exception? ex = null)
#else
        public static void WriteWarn(string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message, ex }))
            {
                WriteWarnAutoLog(message, ex);
            }
        }
#if __IOS__
        public static void WriteWarnAutoLog(string message, Exception? ex = null)
#else
        public static void WriteWarnAutoLog(string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(message + "; Error: " + ex);
                }
                Logger?.Warn(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
#if __IOS__
        public static void WriteWarnPrefix(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteWarnPrefix(string logPrefix, string message, Exception ex = null)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logPrefix, message, ex }))
            {
                WriteWarnPrefixNoAutoLog(logPrefix, message, ex);
            }
        }
#if __IOS__
        public static void WriteWarnPrefixNoAutoLog(string logPrefix, string message, Exception? ex = null)
#else
        public static void WriteWarnPrefixNoAutoLog(string logPrefix, string message, Exception ex = null)
#endif
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow())
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole())
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                Logger?.Warn(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, logPrefix);
                Logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteWarnFormat(string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { format, args }))
            {
                WriteWarnFormatNoAutoLog(format, args);
            }
        }
        public static void WriteWarnFormatNoAutoLog(string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow())
                    Debug.WriteLine(message);
                if (ShouldLogToConsole())
                    Console.WriteLine(message);
                Logger?.Warn(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }
        public static string[] GetAllFootprints(Exception x)
        {
            var st = new StackTrace(x, true);
            var frames = st.GetFrames();
            List<string> traceString = new List<string>();

            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                traceString.Add("File: " + frame.GetFileName() + ", Method:" + frame.GetMethod().Name + ", LineNumber: " + frame.GetFileLineNumber());
            }
            return traceString.ToArray();
        }
        public static string SqlDbTypeToString(SqlParameter _SqlParameter)
        {
            try
            {
                switch (_SqlParameter.SqlDbType)
                {
                    case SqlDbType.Char:
                    case SqlDbType.NChar:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                        return _SqlParameter.SqlDbType.ToString() + "(" + _SqlParameter.Size.ToString("#,##0") + ")";
                    default:
                        return _SqlParameter.SqlDbType.ToString();
                }
            }
            catch (Exception exOuter)
            {
                LoggingUtils.WriteError("Error in SqlDbTypeToString:", exOuter);
                throw;
            }
        }
    }
}
