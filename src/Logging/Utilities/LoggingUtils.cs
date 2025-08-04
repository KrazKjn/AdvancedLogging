using AdvancedLogging.Constants;
using AdvancedLogging.Extensions;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging.Interfaces;
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
    public static class LoggingUtils
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

        public static bool IsRemotingAppender(ICommonLogger logger) => logger.IsRemoting;

        public static bool IsLoggingToConsole(ICommonLogger logger) => logger.IsLoggingToConsole;

        public static bool IsLoggingToDebugWindow(ICommonLogger logger) => logger.IsLoggingToDebugWindow;

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
                    catch
                    {
                        _consoleStatus = 0;
                    }
                }
                return _consoleStatus == 1;
            }
        }

        public static bool ShouldLogToConsole(ICommonLogger logger)
        {
            return !IsLoggingToConsole(logger) && ConsoleAttached && ApplicationSettings.LogToConsole;
        }

        public static bool ShouldLogToDebugWindow(ICommonLogger logger)
        {
            return !IsLoggingToDebugWindow(logger) && ApplicationSettings.LogToDebugWindow;
        }

        public static void LogFunction(ICommonLogger logger, ILoggingContext loggingContext, MethodBase function, object parameters, bool error = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { function, parameters, error, logPrefix }))
            {
                LogFunctionNoAutoLog(logger, loggingContext, function, parameters, error, logPrefix);
            }
        }

        public static void LogFunctionNoAutoLog(ICommonLogger logger, ILoggingContext loggingContext, MethodBase function, object parameters, bool error = false, string logPrefix = "", Exception exception = null)
        {
            try
            {
                string message = logPrefix;
                StackTrace st = new StackTrace();
                StackFrame[] arrFrames = st.GetFrames();
                string CallPath = Loggers.Log4NetLogger.FunctionFullPath(arrFrames.Skip(1).ToArray());

                ParameterInfo[] pars = function.GetParameters();
                message += string.Format("[Func: {0}", function.DeclaringType.FullName + " (" + function.MemberType.ToString() + ")]");
                if (error)
                    WriteError(logger, message);
                else
                {
                    logger.Debug(message);
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                }
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                if (error)
                {
                    if (exception == null)
                    {
                        logger.ErrorFormat("\tCall Path: {0}", CallPath);
                    }
                }
                ProcessParameters(logger, pars, parameters, error, logPrefix);
                if (exception != null)
                {
                    WriteError(logger, "\t" + new string('*', 120));
                    WriteErrorFormat(logger, "\tException Error: {0}", exception.GetType().Name + ": " + exception.Message);
                    WriteError(logger, "\t" + new string('*', 120));
                    WriteErrorFormat(logger, "\tCall Path: {0}", CallPath);
                    WriteError(logger, "\tSource: " + exception.Source);
                    WriteError(logger, "\tTargetSite: " + exception.TargetSite);
                    foreach (string strItem in GetAllFootprints(exception))
                    {
                        WriteError(logger, "\t" + strItem);
                    }
                    WriteError(logger, "\t" + new string('*', 120));
                }
            }
            catch (Exception ex)
            {
                WriteError(logger, logPrefix + "Error in LogFunction: ", ex);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(logPrefix + string.Format("Error in LogFunction: {0}", ex));
            }
            finally
            {
                if (error)
                    WriteError(logger, "[End Func]");
                else
                {
                    logger.Debug("[End Func]");
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine("[End Func]");
                }
                if (ShouldLogToConsole(logger))
                    Console.WriteLine("[End Func]");
            }
        }
        public static void ProcessParameters(ICommonLogger logger, ILoggingContext loggingContext, ParameterInfo[] pars, object parameters, bool error = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { pars, parameters, error, logPrefix }))
            {
                ProcessParametersNoAutoLog(logger, pars, parameters, error, logPrefix);
            }
        }
        private static bool PrintItForProcessParametersNoAutoLog(ICommonLogger logger, int debugLevel, string logPrefix, string message, bool error)
        {
            if (error)
            {
                WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                return true;
            }
            else
            {
                if (debugLevel == 0)
                {
                    WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
                    return true;
                }
                else
                    return WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
            }
        }
        public static void ProcessParametersNoAutoLog(ICommonLogger logger, ParameterInfo[] pars, object parameters, bool error = false, string logPrefix = "", int debugLevel = 4)
        {
            if (pars == null || pars.Length == 0)
            {
                return;
            }
            int i = 0;
            string message = "";
            foreach (PropertyInfo pi in parameters.GetType().GetProperties())
            {
                string name = pars[i].ParameterType.Name;
                string fullName = pars[i].ParameterType.FullName ?? pars[i].ParameterType.Name;
                System.Type type = pars[i].ParameterType;

                while (i < pars.Length && pars[i].IsOut)
                {
                    message = string.Format("({0}){1} N/A (Out)", pars[i].ParameterType.Name, pars[i].Name);
                    PrintItForProcessParametersNoAutoLog(logger, debugLevel, logPrefix, message, error);
                    i++;
                    if (i < pars.Length)
                    {
                        name = pars[i].ParameterType.Name;
                        fullName = pars[i].ParameterType.FullName ?? pars[i].ParameterType.Name;
                        type = pars[i].ParameterType;
                    }
                }

                if (i >= pars.Length) continue;

                List<string> valueTypes = new List<string>();
                bool isArray = false;
                int dimensions = 0;

                if (fullName.EndsWith("&"))
                {
                    fullName = fullName.Replace("&", "");
                }
                if (fullName.EndsWith("[]"))
                {
                    isArray = true;
                    fullName = fullName.Replace("[]", "");
                }
                if (fullName.Contains('`') && fullName.Contains("[["))
                {
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
                        if (PrintItForProcessParametersNoAutoLog(logger, debugLevel, logPrefix, message, error))
                        {
                            System.Data.SqlClient.SqlCommand sc = (System.Data.SqlClient.SqlCommand)pi.GetValue(parameters, null);
                            if (sc == null)
                            {
                                message = string.Format("\t{0}", LogFormats.NULL_TEXT);
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                                else
                                    WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
                            }
                            else
                            {
                                sc.Log(debugLevel, logPrefix);
                            }
                        }
                        break;
                    default:
                        if (isArray)
                        {
                            message = string.Format("({0}){1} ...", i < pars.Length ? name : "???", pi.Name);
                            if (PrintItForProcessParametersNoAutoLog(logger, debugLevel, logPrefix, message, error))
                            {
                                if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
                                {
                                    object oitem = pi.GetValue(parameters, null);
                                    if (oitem == null)
                                    {
                                        message = $"\t{LogFormats.NULL_TEXT}";
                                        if (error)
                                            WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                                        else
                                            WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            message = string.Format("Object Data  : {0}", ObjectDumper.Dump(oitem));
                                            if (error)
                                                WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                                            else
                                                WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
                                        }
                                        catch (Exception ex)
                                        {
                                            WriteErrorPrefixNoAutoLog(logger, logPrefix, string.Format("Error 'Dumping' object of type [{0}].", fullName), ex);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            message = string.Format("({0}){1}: {2}", i < pars.Length ? name : "???", pi.Name, pi.GetValue(parameters, null));
                            if (PrintItForProcessParametersNoAutoLog(logger, debugLevel, logPrefix, message, error))
                            {
                                object oitem = pi.GetValue(parameters, null);
                                if (oitem != null && !IsSimple(oitem.GetType()))
                                {
                                    if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
                                    {
                                        try
                                        {
                                            ObjectDumper.Maxlevels = 10;
                                            message = string.Format("Object Data  : {0}", ObjectDumper.Dump(oitem));
                                            if (error)
                                                WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                                            else
                                                WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
                                        }
                                        catch (Exception ex)
                                        {
                                            WriteErrorPrefixNoAutoLog(logger, logPrefix, string.Format("Error 'Dumping' object of type [{0}].", fullName), ex);
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
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }
        public static void ProcessStopWatch(ICommonLogger logger, ILoggingContext loggingContext, ref Stopwatch sw, string functionName, SqlCommand cmd, string logPrefix = "", int iDebugLevel = 4)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { sw, functionName, cmd, logPrefix }))
            {
                try
                {
                    if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                    {
                        if (sw?.Elapsed.TotalMinutes >= logger.AutoLogSQLThreshold)
                        {
                            if (logger != null && sw != null)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - SQL Command Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", functionName, logger.AutoLogSQLThreshold, sw.Elapsed);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+   Database Call Information");
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                LogSQLData(logger, false, functionName, cmd.CommandText, cmd.Parameters, true, logPrefix);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            }
                        }
                        else if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else
                    {
                        if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    if (logger.LogLevel >= iDebugLevel && sw != null)
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
        public static void ProcessStopWatch(ICommonLogger logger, ILoggingContext loggingContext, ref Stopwatch sw, Logging.AutoLogFunction vAutoLogFunction, SqlCommand cmd, int iDebugLevel = 4)
        {
            try
            {
                if (vAutoLogFunction.FullName.ToLower().Contains(".sqlhelper") || vAutoLogFunction.FullName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= logger.AutoLogSQLThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - SQL Command Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", vAutoLogFunction.FullName, logger.AutoLogSQLThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarn("+   Database Call Information");
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        LogSQLData(logger, false, vAutoLogFunction.FullName, cmd.CommandText, cmd.Parameters, true, vAutoLogFunction.LogPrefix);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                    else if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                if (logger.LogLevel >= iDebugLevel && sw != null)
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
        public static void ProcessStopWatch(ICommonLogger logger, ILoggingContext loggingContext, ref Stopwatch sw, string functionName, string message = "", string logPrefix = "", int iDebugLevel = 4)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { sw, functionName, message, logPrefix }))
            {
                try
                {
                    if (functionName.ToLower().Contains(".sqlhelper") || functionName.ToLower().Contains("httpwebextensions"))
                    {
                        if (sw?.Elapsed.TotalMinutes >= logger.AutoLogSQLThreshold)
                        {
                            if (logger != null && sw != null)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] minutes - Actual [{3}] minutes.", functionName, (message.Length > 0 ? "HTTP Query" : ""), logger.AutoLogSQLThreshold, sw.Elapsed);
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                if (message.Length > 0)
                                {
                                    vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                    vAutoLogFunction.WriteWarn("+   Web Call Information: " + message);
                                }
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            }
                        }
                        else if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else
                    {
                        if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", functionName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    if (logger.LogLevel >= iDebugLevel && sw != null)
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
        public static void ProcessStopWatch(ICommonLogger logger, ILoggingContext loggingContext, ref Stopwatch sw, Logging.AutoLogFunction vAutoLogFunction, string message = "", int iDebugLevel = 4)
        {
            try
            {
                if (vAutoLogFunction.FullName.ToLower().Contains(".sqlhelper") || vAutoLogFunction.FullName.ToLower().Contains("httpwebextensions"))
                {
                    if (sw?.Elapsed.TotalMinutes >= logger.AutoLogSQLThreshold)
                    {
                        if (logger != null && sw != null)
                        {
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            vAutoLogFunction.WriteWarnFormat("+   {0} Exceeded Time Threashold of [{1}] minutes - Actual [{2}] minutes.", (message.Length > 0 ? "HTTP Query" : ""), logger.AutoLogSQLThreshold, sw.Elapsed);
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                            if (message.Length > 0)
                            {
                                vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                                vAutoLogFunction.WriteWarn("+   Web Call Information: " + message);
                            }
                            vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        }
                    }
                    else if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                else
                {
                    if (sw?.Elapsed.TotalSeconds >= MaxFunctionTimeThreshold)
                    {
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                        vAutoLogFunction.WriteWarnFormat("+   Function: [{0}] - {1} Exceeded Time Threashold of [{2}] seconds - Actual [{3}] seconds.", vAutoLogFunction.FullName, "Function", MaxFunctionTimeThreshold, sw.Elapsed);
                        vAutoLogFunction.WriteWarn("+" + new string('-', 79));
                    }
                }
                if (logger.LogLevel >= iDebugLevel && sw != null)
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

        public static void LogDBError(ICommonLogger logger, ILoggingContext loggingContext, MethodBase CurrentMethod, MethodBase ParentFunction, SqlCommand cmd, Exception ex, string LogPrefix = "")
        {
            string strParent = Loggers.CommonLogger.FunctionFullName(ParentFunction);
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { CurrentMethod, ParentFunction, cmd, ex, LogPrefix }, bSuppressFunctionDeclaration: true))
            {
                try
                {

                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Error");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteErrorFormat("{0}: [{1}] - {2}", Loggers.CommonLogger.FunctionFullName(CurrentMethod), cmd.CommandText, ex.ToString());
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Call Information");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    LogSQLData(logger, strParent, cmd.CommandText, cmd.Parameters, true, LogPrefix);
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { CurrentMethod, ParentFunction, cmd, ex, LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogDBError(ICommonLogger logger, ILoggingContext loggingContext, MethodBase CurrentMethod, MethodBase ParentFunction, string cmdText, Exception ex, string LogPrefix = "")
        {
            string strParent = Loggers.CommonLogger.FunctionFullName(ParentFunction);
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { CurrentMethod, ParentFunction, cmdText, ex, LogPrefix }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Error");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteErrorFormat("{0}: [{1}] - {2}", Loggers.CommonLogger.FunctionFullName(CurrentMethod), cmdText, ex.ToString());
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    vAutoLogFunction.WriteError("+   Database Call Information");
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                    LogSQLData(logger, strParent, cmdText, bForceLogWrite: true, logPrefix: LogPrefix);
                    vAutoLogFunction.WriteError("+" + new string('-', 79));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { CurrentMethod, ParentFunction, cmdText, ex, LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogSQLData(ICommonLogger logger, string functionName, string strCommand, SqlParameter[] sqlParameters, bool bForceLogWrite = false, string logPrefix = "")
        {
            LogSQLData(logger, true, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix);
        }

        public static void LogSQLData(ICommonLogger logger, bool bAutoDetect, string functionName, string strCommand, SqlParameter[] sqlParameters, bool bForceLogWrite = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, null, new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }))
            {
                try
                {
                    string detectedCriteria = "";
                    string detectedFunction;

                    if (logger == null)
                        return;
                    if (!bForceLogWrite)
                    {
                        if (!logger.ToLog(4, out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Comamnd if LogLevel is >= to 4
                            return;
                    }
                    if (vAutoLogFunction.FunctionDeclarationLogged)
                    {
                        if (logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("Command: [{0}]", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("Command: [{0}]", strCommand);
                    }
                    else
                    {
                        if (logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                    }
                    if (!bForceLogWrite)
                    {
                        if (!logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                            return;
                    }
                    if (sqlParameters == null || sqlParameters.Length == 0)
                    {
                        if (logger.IsDebugEnabled)
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
                                if (logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (logger.IsDebugEnabled)
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

        public static void LogSQLData(ICommonLogger logger, string functionName, string strCommand, SqlParameterCollection sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
        {
            LogSQLData(logger, true, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix);
        }

        public static void LogSQLData(ICommonLogger logger, bool bAutoDetect, string functionName, string strCommand, SqlParameterCollection sqlParameters = null, bool bForceLogWrite = false, string logPrefix = "")
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, null, new { bAutoDetect, functionName, strCommand, sqlParameters, bForceLogWrite, logPrefix }))
            {
                try
                {
                    string detectedCriteria = "";
                    string detectedFunction = "";

                    if (logger == null)
                        return;
                    if (!bForceLogWrite)
                    {
                        if (bAutoDetect && !logger.ToLog(4, out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Comamnd if LogLevel is >= to 4
                            return;
                    }
                    if (vAutoLogFunction.FunctionDeclarationLogged)
                    {
                        if (logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("Command: [{0}]", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("Command: [{0}]", strCommand);
                    }
                    else
                    {
                        if (logger.LoggingDebug())
                            vAutoLogFunction.WriteDebugFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                        else
                            vAutoLogFunction.WriteWarnFormat("{0}{1}: Command: [{2}]", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", strCommand);
                    }
                    if (!bForceLogWrite)
                    {
                        if (bAutoDetect && !logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out detectedCriteria, out detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                            return;
                    }
                    if (sqlParameters == null || sqlParameters.Count == 0)
                    {
                        if (logger.IsDebugEnabled)
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
                                if (logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, SqlDbTypeToString(item), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (logger.IsDebugEnabled)
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

        public static void LogSQLData(ICommonLogger logger, ILoggingContext loggingContext, SqlParameterCollection sqlParameters = null, string strAppendLogPrefix = "", int iTabs = -1, bool bSuppressFunctionDeclaration = false)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { sqlParameters, strAppendLogPrefix }, null, iTabs, bSuppressFunctionDeclaration))
            {
                try
                {
                    if (strAppendLogPrefix.Length > 0)
                        vAutoLogFunction.LogPrefix = strAppendLogPrefix;
                    if (logger == null)
                        return;
                    if (!logger.ToLog(DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], out string detectedCriteria, out string detectedFunction, out int detectedLevel)) // Log the Parameters if LogLevel is >= to 10
                        return;
                    if (sqlParameters == null)
                    {
                        if (logger.IsDebugEnabled)
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
                                if (logger.IsDebugEnabled)
                                    vAutoLogFunction.WriteDebugFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                                else
                                    vAutoLogFunction.WriteLogFormat("({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                            }
                            else
                            {
                                if (logger.IsDebugEnabled)
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

        public static void LogSQLOutData(ICommonLogger logger, SqlParameterCollection sqlParameters, Logging.AutoLogFunction vAutoLogFunction)
        {
            if (sqlParameters != null)
            {
                foreach (SqlParameter item in sqlParameters)
                {
                    if (item.Direction == ParameterDirection.Output ||
                        item.Direction == ParameterDirection.InputOutput ||
                        item.Direction == ParameterDirection.ReturnValue)
                    {
                        if (logger != null && logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                        else
                            vAutoLogFunction.WriteLogFormat("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString());
                    }
                }
            }
        }

        public static void WriteLog(ICommonLogger logger, ILoggingContext loggingContext, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { message, ex }))
            {
                WriteLogNoAutoLog(logger, message, ex);
            }
        }

        public static void WriteLogNoAutoLog(ICommonLogger logger, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                logger?.Info(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                logger?.Debug(string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }

        public static void WriteLogPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, message, ex }))
            {
                WriteLogPrefixNoAutoLog(logger, logPrefix, message, ex);
            }
        }

        public static void WriteLogPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                logger?.Info(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, message, ex }, true, exception: exOuter);
                logger?.Debug(string.Format("{0}{1}", logPrefix, System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteLogFormat(ICommonLogger logger, ILoggingContext loggingContext, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { format, args }))
            {
                WriteLogFormatNoAutoLog(logger, format, args);
            }
        }
        public static void WriteLogFormatNoAutoLog(ICommonLogger logger, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.Info(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteDebug(ICommonLogger logger, ILoggingContext loggingContext, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { message, ex }))
            {
                WriteDebugNoAutoLog(logger, message, ex);
            }
        }

        public static void WriteDebugNoAutoLog(ICommonLogger logger, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                logger?.Debug(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteDebugPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, message, ex }))
            {
                WriteDebugPrefixNoAutoLog(logger, logPrefix, message, ex);
            }
        }

        public static void WriteDebugPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                logger?.Debug(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static bool WriteDebug(ICommonLogger logger, ILoggingContext loggingContext, int DebugLevel, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { DebugLevel, message, ex }))
            {
                return WriteDebugNoAutoLog(logger, DebugLevel, message, ex);
            }
        }

        public static bool WriteDebugNoAutoLog(ICommonLogger logger, int DebugLevel, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                if (logger == null)
                    return false;
                else
                {
                    return logger.Debug(DebugLevel, message, ex);
                }
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static bool WriteDebugPrefix(ICommonLogger logger, ILoggingContext loggingContext, int DebugLevel, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { DebugLevel, logPrefix, message, ex }))
            {
                return WriteDebugPrefixNoAutoLog(logger, DebugLevel, logPrefix, message, ex);
            }
        }

        public static bool WriteDebugPrefixNoAutoLog(ICommonLogger logger, int DebugLevel, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                return logger != null && logger.DebugPrefix(DebugLevel, logPrefix, message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteDebugFormatPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, format, args }))
            {
                WriteDebugFormatPrefixNoAutoLog(logger, logPrefix, format, args);
            }
        }
        public static void WriteDebugFormatPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.DebugPrefix(logPrefix, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, format, args }, true, logPrefix);
                logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteDebugFormatPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string functionName, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, functionName, format, args }))
            {
                WriteDebugFormatPrefixNoAutoLog(logger, logPrefix, functionName, format, args);
            }
        }
        public static void WriteDebugFormatPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string functionName, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.DebugPrefix(logPrefix, functionName, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { logPrefix, functionName, format, args }, true, logPrefix);
                logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static bool WriteDebugFormatPrefix(ICommonLogger logger, ILoggingContext loggingContext, int DebugLevel, string logPrefix, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { DebugLevel, logPrefix, format, args }))
            {
                return WriteDebugFormatPrefixNoAutoLog(logger, DebugLevel, logPrefix, format, args);
            }
        }
        public static bool WriteDebugFormatPrefixNoAutoLog(ICommonLogger logger, int DebugLevel, string logPrefix, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                return logger != null && logger.DebugPrefix(DebugLevel, logPrefix, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, logPrefix, format, args }, true, logPrefix);
                logger?.Debug(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static bool WriteDebugFormatPrefix(ICommonLogger logger, ILoggingContext loggingContext, int DebugLevel, string logPrefix, string functionName, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { DebugLevel, logPrefix, functionName, format, args }))
            {
                return WriteDebugFormatPrefixNoAutoLog(logger, DebugLevel, logPrefix, functionName, format, args);
            }
        }
        public static bool WriteDebugFormatPrefixNoAutoLog(ICommonLogger logger, int DebugLevel, string logPrefix, string functionName, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                return logger != null && logger.DebugPrefix(DebugLevel, logPrefix, functionName, message, null);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { DebugLevel, logPrefix, functionName, format, args }, true, logPrefix);
                logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }

        public static void WriteError(ICommonLogger logger, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, null, new { message, ex }))
            {
                WriteErrorNoAutoLog(logger, message, ex);
            }
        }

        public static void WriteErrorNoAutoLog(ICommonLogger logger, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                logger?.Error(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteErrorPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, message, ex }))
            {
                WriteErrorPrefixNoAutoLog(logger, logPrefix, message, ex);
            }
        }

        public static void WriteErrorPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                logger?.Error(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteErrorFormat(ICommonLogger logger, ILoggingContext loggingContext, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { format, args }))
            {
                WriteErrorFormatNoAutoLog(logger, format, args);
            }
        }
        public static void WriteErrorFormatNoAutoLog(ICommonLogger logger, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.ErrorFormat(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteFatal(ICommonLogger logger, ILoggingContext loggingContext, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { message, ex }))
            {
                WriteFatalNoAutoLog(logger, message, ex);
            }
        }

        public static void WriteFatalNoAutoLog(ICommonLogger logger, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                logger?.Fatal(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteFatalPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, message, ex }))
            {
                WriteFatalPrefixNoAutoLog(logger, logPrefix, message, ex);
            }
        }

        public static void WriteFatalPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                logger?.Error(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }
        public static void WriteFatalFormat(ICommonLogger logger, ILoggingContext loggingContext, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { format, args }))
            {
                WriteFatalFormatNoAutoLog(logger, format, args);
            }
        }
        public static void WriteFatalFormatNoAutoLog(ICommonLogger logger, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.ErrorFormat(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteWarn(ICommonLogger logger, ILoggingContext loggingContext, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { message, ex }))
            {
                WriteWarnNoAutoLog(logger, message, ex);
            }
        }

        public static void WriteWarnNoAutoLog(ICommonLogger logger, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(message + "; Error: " + ex);
                }
                logger?.Warn(message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, exception: exOuter);
                throw;
            }
        }

        public static void WriteWarnPrefix(ICommonLogger logger, ILoggingContext loggingContext, string logPrefix, string message, Exception ex = null)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { logPrefix, message, ex }))
            {
                WriteWarnPrefixNoAutoLog(logger, logPrefix, message, ex);
            }
        }

        public static void WriteWarnPrefixNoAutoLog(ICommonLogger logger, string logPrefix, string message, Exception ex = null)
        {
            try
            {
                if (ex == null)
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message);
                }
                else
                {
                    if (ShouldLogToDebugWindow(logger))
                        Debug.WriteLine(logPrefix + message + "; Error: " + ex);
                    if (ShouldLogToConsole(logger))
                        Console.WriteLine(logPrefix + message + "; Error: " + ex);
                }
                logger?.Warn(logPrefix + message, ex);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { message, ex }, true, logPrefix);
                logger?.Error(logPrefix + string.Format("{0}", System.Reflection.MethodBase.GetCurrentMethod().Name), exOuter);
                throw;
            }
        }
        public static void WriteWarnFormat(ICommonLogger logger, ILoggingContext loggingContext, string format, params object[] args)
        {
            using (var vAutoLogFunction = new Logging.AutoLogFunction(logger, loggingContext, new { format, args }))
            {
                WriteWarnFormatNoAutoLog(logger, format, args);
            }
        }
        public static void WriteWarnFormatNoAutoLog(ICommonLogger logger, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                if (ShouldLogToDebugWindow(logger))
                    Debug.WriteLine(message);
                if (ShouldLogToConsole(logger))
                    Console.WriteLine(message);
                logger?.Warn(message);
            }
            catch (Exception exOuter)
            {
                LogFunctionNoAutoLog(logger, null, System.Reflection.MethodBase.GetCurrentMethod(), new { format, args }, true, exception: exOuter);
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
                WriteError(null, "Error in SqlDbTypeToString:", exOuter);
                throw;
            }
        }
    }
}
