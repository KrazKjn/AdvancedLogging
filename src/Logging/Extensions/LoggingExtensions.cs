using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Reflection;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// Provides extension methods for logging various types of objects and data.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Gets the headers from a WebHeaderCollection as an array of key-value pairs.
        /// </summary>
        /// <param name="webHeaderCollection">The WebHeaderCollection to get headers from.</param>
        /// <returns>An array of key-value pairs representing the headers.</returns>
        public static KeyValuePair<string, string>[] GetHeaders(this WebHeaderCollection webHeaderCollection)
        {
            string[] keys = webHeaderCollection.AllKeys;
            var keyVals = new KeyValuePair<string, string>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                keyVals[i] = new KeyValuePair<string, string>(keys[i], webHeaderCollection[keys[i]]);
            return keyVals;
        }

        /// <summary>
        /// Serializes a WebHeaderCollection to a string.
        /// </summary>
        /// <param name="webHeaderCollection">The WebHeaderCollection to serialize.</param>
        /// <returns>A string representation of the WebHeaderCollection.</returns>
        private static string Serialize(this WebHeaderCollection webHeaderCollection)
        {
            var response = new System.Text.StringBuilder();
            foreach (string k in webHeaderCollection.Keys)
                response.AppendLine(k + ": " + webHeaderCollection[k]);
            return response.ToString();
        }
        #region Log Extensions
        /// <summary>
        /// Logs the content and length of a string, or indicates if the string is null or empty.
        /// </summary>
        public static void Log(this string logMessage, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            void LogMessage(string message)
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
            }

            if (string.IsNullOrEmpty(logMessage))
            {
                LogMessage($"\t{LogFormats.NULL_TEXT}");
            }
            else
            {
                LogMessage($"String: {logMessage}");
                LogMessage($"\tCharacters: {logMessage.Length}");
            }
        }

        /// <summary>
        /// Logs detailed information about a WebRequest, including the request URI, timeout, and credentials.
        /// </summary>
        public static void Log(this WebRequest webRequest, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            void LogMessage(string message)
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
            }

            if (webRequest == null)
            {
                LogMessage($"\t{LogFormats.NULL_TEXT}");
                return;
            }

            LogMessage($"Address: {webRequest.RequestUri}");
            LogMessage($"Timeout: {webRequest.Timeout}");

            if (webRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = webRequest.Credentials?.GetCredential(webRequest.RequestUri, "");
                string credentials = nc == null || (nc.Domain == "" && nc.UserName == "") ? "Credentials: None" : $"Credentials: {nc.Domain}{(nc.Domain == "" ? "" : "\\")}{nc.UserName}";
                LogMessage(credentials);
            }

            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    LogMessage($"Object Data  : {DataObjectDumper.Dump(webRequest, logger: logger)}");
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "Error 'Dumping' object of type [System.Net.WebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about an HttpWebRequest, including the request URI, timeout, and credentials.
        /// </summary>
        public static void Log(this HttpWebRequest httpRequest, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            void LogMessage(string message)
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
            }

            if (httpRequest == null)
            {
                LogMessage($"\t{LogFormats.NULL_TEXT}");
                return;
            }

            LogMessage($"Address: {httpRequest.Address}");
            LogMessage($"Timeout: {httpRequest.Timeout}");

            if (httpRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = httpRequest.Credentials?.GetCredential(httpRequest.RequestUri, "");
                string credentials = nc == null || (nc.Domain == "" && nc.UserName == "") ? "Credentials: None" : $"Credentials: {nc.Domain}{(nc.Domain == "" ? "" : "\\")}{nc.UserName}";
                LogMessage(credentials);
            }

            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    LogMessage($"Object Data  : {DataObjectDumper.Dump(httpRequest, logger: logger)}");
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about each X509Certificate in a X509CertificateCollection.
        /// </summary>
        public static void Log(this System.Security.Cryptography.X509Certificates.X509CertificateCollection x509CertificateCollection, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            foreach (var x509Certificate in x509CertificateCollection)
            {
                string message = string.Format("Issuer: {0}", x509Certificate.Issuer);
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);

                message = string.Format("\tExpires: {0}", x509Certificate.GetExpirationDateString());
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);

                message = string.Format("\tSerial#: {0}", x509Certificate.GetSerialNumberString());
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);

                message = string.Format("\tSubject: {0}", x509Certificate.Subject);
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
            }
        }

        /// <summary>
        /// Logs the elapsed time of a Stopwatch.
        /// </summary>
        public static void Log(this System.Diagnostics.Stopwatch stopwatch, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            string message = stopwatch.IsRunning ? $"Elapsed: {stopwatch.Elapsed}" : "Elapsed: Not Running";

            if (error)
                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
            else
                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
        }

        /// <summary>
        /// Logs detailed information about a CommonLogger.
        /// </summary>
        public static void Log(this Loggers.CommonLogger commonLogger, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            string message = string.Format("FileName: {0}", commonLogger.LogFile ?? "Not Configured");
            if (error)
                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
            else
                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);

            if (commonLogger.LogFile != null)
            {
                message = string.Format("Level  : {0}", commonLogger.Level?.ToString());
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);

                if (commonLogger.IsDebugEnabled)
                {
                    message = string.Format("Debug Level: {0}", commonLogger.LogLevel.ToString());
                    if (error)
                        LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
                }
            }

            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    message = string.Format("Object Data  : {0}", DataObjectDumper.Dump(commonLogger, logger: logger));
                    if (error)
                        LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "Error 'Dumping' object of type [AdvancedLogging.Logging.Log4NetLogger].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about a SqlCommand.
        /// </summary>
        public static void Log(this System.Data.SqlClient.SqlCommand sqlCommand, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_SqlCommand])
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "\tCommandText: " + sqlCommand.CommandText);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, "\tCommandText: " + sqlCommand.CommandText);
            }

            if (sqlCommand.Parameters != null && logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_SqlParameters])
            {
                if (sqlCommand.Parameters.Count == 0)
                {
                    if (error)
                        LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "\tParameters: (None)");
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, "\tParameters: (None)");
                }
                else
                {
                    foreach (SqlParameter item in sqlCommand.Parameters)
                    {
                        if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                        {
                            if (error)
                                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, string.Format("\t({0}){1}: {2}", LoggingUtils.SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                            else
                                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, string.Format("\t({0}){1}: {2}", LoggingUtils.SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                        }
                        else
                        {
                            if (error)
                                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, LoggingUtils.SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                            else
                                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, LoggingUtils.SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                        }
                    }
                }
            }

            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    string message = string.Format("Object Data  : {0}", DataObjectDumper.Dump(sqlCommand, logger: logger));
                    if (error)
                        LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                    else
                        LoggingUtils.WriteDebugPrefixNoAutoLog(logger, debugLevel, logPrefix, message);
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "Error 'Dumping' object of type [System.Data.SqlClient.SqlCommand].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about a DataRow.
        /// </summary>
        public static void Log(this System.Data.DataRow dataRow, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (!logger.ToLog(debugLevel))
                return;

            if (dataRow != null)
            {
                var cols = dataRow.ItemArray.Select(i => "" + i).ToArray();
                var message = "{";
                for (int col = 0; col < cols.Count(); col++)
                {
                    message += string.Format(" \"{0}\":\"{1}\",", col + 1, cols[col]);
                }
                message = message.TrimEnd(',') + " }";

                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, "\tRow Data: " + message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, "\tParameters: " + message);
            }
        }

        /// <summary>
        /// Logs the date and time in a short format.
        /// </summary>
        public static void Log(this DateTime dateTime, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (!logger.ToLog(debugLevel))
                return;

            string message = dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();

            if (error)
                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
            else
                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
        }

        /// <summary>
        /// Logs detailed information about an object.
        /// </summary>
        public static void Log(this object obj, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (!logger.ToLog(debugLevel))
                return;

            string message = obj.ToString();

            MemberInfo[] members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var memberInfo in members.Where(p => p.Name == "ToString"))
            {
                var methodInfo = memberInfo as MethodInfo;

                if (methodInfo == null)
                    continue;

                var type = methodInfo.ReturnType;
                if (type == typeof(string) && memberInfo.Name == "ToString")
                {
                    message = (string)methodInfo.Invoke(obj, null);
                    break;
                }
            }

            if (error)
                LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
            else
                LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, message);

            if (!message.Contains("\n") && message.ToCharArray().Count(p => p == '.') > 2)
            {
                throw new NotImplementedException("Custom ToPrint not found!");
            }
        }

        /// <summary>
        /// Logs detailed information about an exception.
        /// </summary>
        public static void Log(this Exception exception, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (!logger.ToLog(debugLevel) || exception == null)
                return;

            List<string> messages = new List<string>
            {
                new string('*', 120),
                string.Format("Exception Error: {0}", exception.GetType().Name + ": " + exception.Message),
                new string('*', 120),
                "Source: " + exception.Source,
                "TargetSite: " + exception.TargetSite
            };

            foreach (string item in LoggingUtils.GetAllFootprints(exception))
            {
                messages.Add(item);
            }
            messages.Add(new string('*', 120));

            foreach (string message in messages)
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, message);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, message);
            }
        }

        /// <summary>
        /// Logs detailed information about a WebException.
        /// </summary>
        public static void Log(this System.Net.WebException exception, ICommonLogger logger, int debugLevel, string logPrefix = "", bool error = false)
        {
            if (!logger.ToLog(debugLevel) || exception == null)
                return;

            List<string> messages = new List<string>
            {
                new string('*', 120),
                string.Format("Exception Error: {0}", exception.GetType().Name + ": " + exception.Message),
                new string('*', 120),
                "Source: " + exception.Source,
                "TargetSite: " + exception.TargetSite
            };

            if (exception.Status == WebExceptionStatus.ProtocolError)
            {
                int code = (int)((HttpWebResponse)exception.Response).StatusCode;
                messages.Add("Status Code: " + code.ToString());
                messages.Add("Status Description: " + ((HttpWebResponse)exception.Response).StatusDescription);
                messages.Add("Server: " + ((HttpWebResponse)exception.Response).Server);
                messages.Add("Method: " + ((HttpWebResponse)exception.Response).Method);
                messages.Add("Response Uri: " + ((HttpWebResponse)exception.Response).ResponseUri.OriginalString);
            }

            foreach (string item in LoggingUtils.GetAllFootprints(exception))
            {
                messages.Add(item);
            }
            messages.Add(new string('*', 120));

            foreach (string item in messages)
            {
                if (error)
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, logPrefix, item);
                else
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, logPrefix, item);
            }
        }

        #endregion

        /// <summary>
        /// Converts an object to the specified type.
        /// </summary>
        public static object ToType(this object source, Type dest)
        {
            return Convert.ChangeType(source, dest);
        }
    }
}