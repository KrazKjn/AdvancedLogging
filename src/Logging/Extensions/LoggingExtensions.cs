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
using static AdvancedLogging.Utilities.LoggingUtils;

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
        /// <param name="logMessage">The string to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this string logMessage, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Local function to handle the logging, reducing redundancy
            void LogMessage(string message)
            {
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
            }

            // Check if the input string is null or empty
            if (string.IsNullOrEmpty(logMessage))
            {
                // Log null or empty string message
                LogMessage($"\t{LogFormats.NULL_TEXT}");
            }
            else
            {
                // Log the string content
                LogMessage($"String: {logMessage}");
                // Log the length of the string
                LogMessage($"\tCharacters: {logMessage.Length}");
            }
        }

        /// <summary>
        /// Logs detailed information about a WebRequest, including the request URI, timeout, and credentials.
        /// </summary>
        /// <param name="webRequest">The WebRequest to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this WebRequest webRequest, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Local function to handle the logging, reducing redundancy
            void LogMessage(string message)
            {
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
            }

            // Check if the WebRequest object is null
            if (webRequest == null)
            {
                // Log null WebRequest message
                LogMessage($"\t{LogFormats.NULL_TEXT}");
                return;
            }

            // Log the request URI
            LogMessage($"Address: {webRequest.RequestUri}");
            // Log the request timeout value
            LogMessage($"Timeout: {webRequest.Timeout}");

            // Check if the WebRequest has credentials
            if (webRequest.Credentials != null)
            {
#if __IOS__
                System.Net.NetworkCredential? nc = _WebRequest.Credentials?.GetCredential(_WebRequest.RequestUri, "");
#else
                System.Net.NetworkCredential nc = webRequest.Credentials?.GetCredential(webRequest.RequestUri, "");
#endif
                // Determine the credentials message based on the presence of domain and username
                string credentials = nc == null || (nc.Domain == "" && nc.UserName == "") ? "Credentials: None" : $"Credentials: {nc.Domain}{(nc.Domain == "" ? "" : "\\")}{nc.UserName}";
                // Log the credentials message
                LogMessage(credentials);
            }

            // Check if detailed logging is enabled based on the application settings
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    // Log the detailed object data
                    LogMessage($"Object Data  : {ObjectDumper.Dump(webRequest)}");
                }
                catch (Exception ex)
                {
                    // Log any exception that occurs while dumping the object data
                    WriteErrorPrefixNoAutoLog(logPrefix, "Error 'Dumping' object of type [System.Net.WebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about an HttpWebRequest, including the request URI, timeout, and credentials.
        /// </summary>
        /// <param name="httpRequest">The HttpWebRequest to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this HttpWebRequest httpRequest, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Local function to handle the logging, reducing redundancy
            void LogMessage(string message)
            {
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
            }

            // Check if the HttpWebRequest object is null
            if (httpRequest == null)
            {
                // Log null HttpWebRequest message
                LogMessage($"\t{LogFormats.NULL_TEXT}");
                return;
            }

            // Log the request URI
            LogMessage($"Address: {httpRequest.Address}");
            // Log the request timeout value
            LogMessage($"Timeout: {httpRequest.Timeout}");

            // Check if the HttpWebRequest has credentials
            if (httpRequest.Credentials != null)
            {
#if __IOS__
                System.Net.NetworkCredential? nc = _httpRequest.Credentials?.GetCredential(_httpRequest.RequestUri, "");
#else
                System.Net.NetworkCredential nc = httpRequest.Credentials?.GetCredential(httpRequest.RequestUri, "");
#endif
                // Determine the credentials message based on the presence of domain and username
                string credentials = nc == null || (nc.Domain == "" && nc.UserName == "") ? "Credentials: None" : $"Credentials: {nc.Domain}{(nc.Domain == "" ? "" : "\\")}{nc.UserName}";
                // Log the credentials message
                LogMessage(credentials);
            }

            // Check if detailed logging is enabled based on the application settings
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    // Log the detailed object data
                    LogMessage($"Object Data  : {ObjectDumper.Dump(httpRequest)}");
                }
                catch (Exception ex)
                {
                    // Log any exception that occurs while dumping the object data
                    WriteErrorPrefixNoAutoLog(logPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about each X509Certificate in a X509CertificateCollection, including the issuer, expiration date, serial number, and subject.
        /// </summary>
        /// <param name="x509CertificateCollection">The X509CertificateCollection to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this System.Security.Cryptography.X509Certificates.X509CertificateCollection x509CertificateCollection, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Iterate through each X509Certificate in the collection
            foreach (var x509Certificate in x509CertificateCollection)
            {
                // Log the issuer of the certificate
                string message = string.Format("Issuer: {0}", x509Certificate.Issuer);
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);

                // Log the expiration date of the certificate
                message = string.Format("\tExpires: {0}", x509Certificate.GetExpirationDateString());
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);

                // Log the serial number of the certificate
                message = string.Format("\tSerial#: {0}", x509Certificate.GetSerialNumberString());
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);

                // Log the subject of the certificate
                message = string.Format("\tSubject: {0}", x509Certificate.Subject);
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
            }
        }

        /// <summary>
        /// Logs the elapsed time of a Stopwatch, indicating whether it is currently running or not.
        /// </summary>
        /// <param name="stopwatch">The Stopwatch to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this System.Diagnostics.Stopwatch stopwatch, int debugLevel, string logPrefix = "", bool error = false)
        {
            string message;

            // Check if the Stopwatch is currently running
            if (stopwatch.IsRunning)
            {
                // Log the elapsed time if the Stopwatch is running
                message = string.Format("Elapsed: {0}", stopwatch.Elapsed.ToString());
            }
            else
            {
                // Log a message indicating the Stopwatch is not running
                message = "Elapsed: Not Running";
            }

            // Log the message based on the error flag
            if (error)
                WriteErrorPrefixNoAutoLog(logPrefix, message);
            else
                WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
        }

        /// <summary>
        /// Logs detailed information about a Log4NetLogger, including the log file name, logging level, and debug level.
        /// </summary>
        /// <param name="commonLogger">The Log4NetLogger to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this Loggers.CommonLogger commonLogger, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Log the log file name or indicate if it is not configured
            string message = string.Format("FileName: {0}", commonLogger.LogFile ?? "Not Configured");
            if (error)
                WriteErrorPrefixNoAutoLog(logPrefix, message);
            else
                WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);

            // If the log file is configured, log additional details
            if (commonLogger.LogFile != null)
            {
                // Log the logging level
                message = string.Format("Level  : {0}", commonLogger.Level?.ToString());
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);

                // If debug logging is enabled, log the debug level
                if (commonLogger.IsDebugEnabled)
                {
                    message = string.Format("Debug Level: {0}", commonLogger.LogLevel.ToString());
                    if (error)
                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                    else
                        WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
                }
            }

            // If detailed logging is enabled based on the application settings, log detailed object data
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    // Log the detailed object data
                    message = string.Format("Object Data  : {0}", ObjectDumper.Dump(commonLogger));
                    if (error)
                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                    else
                        WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
                }
                catch (Exception ex)
                {
                    // Log any exception that occurs while dumping the object data
                    WriteErrorPrefixNoAutoLog(logPrefix, "Error 'Dumping' object of type [AdvancedLogging.Logging.Log4NetLogger].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about a SqlCommand, including the command text and parameters.
        /// </summary>
        /// <param name="sqlCommand">The SqlCommand to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this System.Data.SqlClient.SqlCommand sqlCommand, int debugLevel, string logPrefix = "", bool error = false)
        {
            // Check if logging for SqlCommand is enabled
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_SqlCommand])
            {
                // Log the command text
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, "\tCommandText: " + sqlCommand.CommandText);
                else
                    WriteDebugPrefixNoAutoLog(logPrefix, "\tCommandText: " + sqlCommand.CommandText);
            }

            // Check if the SqlCommand has parameters
            if (sqlCommand.Parameters != null)
            {
                // Check if logging for SQL parameters is enabled
                if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_SqlParameters])
                {
                    // Log if there are no parameters
                    if (sqlCommand.Parameters.Count == 0)
                    {
                        if (error)
                            WriteErrorPrefixNoAutoLog(logPrefix, "\tParameters: (None)");
                        else
                            WriteDebugPrefixNoAutoLog(logPrefix, "\tParameters: (None)");
                    }
                    else
                    {
                        // Log each parameter
                        foreach (SqlParameter item in sqlCommand.Parameters)
                        {
                            if (item.Direction == ParameterDirection.Input || item.Direction == ParameterDirection.InputOutput)
                            {
                                // Log input and input-output parameters
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {2}", SqlDbTypeToString(item), item.ParameterName, (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                            }
                            else
                            {
                                // Log other types of parameters
                                if (error)
                                    WriteErrorPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                                else
                                    WriteDebugPrefixNoAutoLog(logPrefix, string.Format("\t({0}){1}: {3}({2})", item.ParameterName, SqlDbTypeToString(item), item.Direction.ToString(), (item.Value == System.DBNull.Value || item.Value == null) ? LogFormats.NULL_TEXT : item.Value.ToString()));
                            }
                        }
                    }
                }
            }

            // Check if detailed logging for complex parameter values is enabled
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    // Log detailed object data
                    string message = string.Format("Object Data  : {0}", ObjectDumper.Dump(sqlCommand));
                    if (error)
                        WriteErrorPrefixNoAutoLog(logPrefix, message);
                    else
                        WriteDebugPrefixNoAutoLog(debugLevel, logPrefix, message);
                }
                catch (Exception ex)
                {
                    // Log any exception that occurs while dumping the object data
                    WriteErrorPrefixNoAutoLog(logPrefix, "Error 'Dumping' object of type [System.Data.SqlClient.SqlCommand].", ex);
                }
            }
        }

        /// <summary>
        /// Logs detailed information about a DataRow, including the values of each column in the row.
        /// </summary>
        /// <param name="dataRow">The DataRow to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="commonLogger">The logger to use for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this System.Data.DataRow dataRow, int debugLevel, ICommonLogger commonLogger, string logPrefix = "", bool error = false)
        {
            // Check if logging is enabled for the specified debug level
            if (!commonLogger.ToLog(debugLevel))
                return;

            string message = "";

            // Check if the DataRow is not null
            if (dataRow != null)
            {
                // Get the values of each column in the row as strings
                var cols = dataRow.ItemArray.Select(i => "" + i) // Not i.ToString() so when i is null -> ""
                                .ToArray(); // For .NET35 and before, .NET4 Join takes IEnumerable

                message = "{";

                // Construct the log message with column values
                for (int col = 0; col < cols.Count(); col++)
                {
                    message += string.Format(" \"{0}\":\"{1}\",", col + 1, cols[col]);
                }
                message = message.TrimEnd(',');
                message += " }";
                //strMessage = string.Join("|", cols);

                // Log the row data based on the error flag
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, "\tRow Data: " + message);
                else
                    WriteDebugPrefixNoAutoLog(logPrefix, "\tParameters: " + message);
            }
        }

        /// <summary>
        /// Logs the date and time in a short format.
        /// </summary>
        /// <param name="_dateTime">The DateTime to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="commonLogger">The logger to use for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this DateTime _dateTime, int debugLevel, ICommonLogger commonLogger, string logPrefix = "", bool error = false)
        {
            // Check if logging is enabled for the specified debug level
            if (!commonLogger.ToLog(debugLevel))
                return;

            // Create the log message with the short date and time format
            string message = _dateTime.ToShortDateString() + " " + _dateTime.ToShortTimeString();

            // Log the message based on the error flag
            if (error)
                WriteErrorPrefixNoAutoLog(logPrefix, message);
            else
                WriteDebugPrefixNoAutoLog(logPrefix, message);
        }

        /// <summary>
        /// Logs detailed information about an object, including its string representation.
        /// </summary>
        /// <param name="_object">The object to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="commonLogger">The logger to use for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this object _object, int debugLevel, ICommonLogger commonLogger, string logPrefix = "", bool error = false)
        {
            // Check if logging is enabled for the specified debug level
            if (!commonLogger.ToLog(debugLevel))
                return;

            // Get the string representation of the object
            string message = _object.ToString();

            // Get the public instance members of the object's type
            MemberInfo[] members = _object.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var memberInfo in members.Where(p => p.Name == "ToString"))
            {
                var methodInfo = memberInfo as MethodInfo;

                if (methodInfo == null)
                    continue;

                var type = methodInfo.ReturnType;
                if (type == typeof(string) && memberInfo.Name == "ToString")
                {
                    // Invoke the ToString method and get the result as a string
                    message = (string)methodInfo.Invoke(_object, null);
                    break;
                }
            }

            // Log the string representation of the object based on the error flag
            if (error)
                WriteErrorPrefixNoAutoLog(logPrefix, message);
            else
                WriteDebugPrefixNoAutoLog(logPrefix, message);

            // Check if the string representation does not contain a newline character
            if (!message.Contains("\n"))
            {
                // Check if the string representation contains more than two periods
                if (message.ToCharArray().Count(p => p == '.') > 2)
                {
                    throw new NotImplementedException("Custom ToPrint not found!");
                }
            }
        }

        /// <summary>
        /// Logs detailed information about an exception, including its type, message, source, and target site.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="commonLogger">The logger to use for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(Exception exception, int debugLevel, ICommonLogger commonLogger, string logPrefix = "", bool error = false)
        {
            // Check if logging is enabled for the specified debug level
            if (!commonLogger.ToLog(debugLevel))
                return;

            // Check if the exception is not null
            if (exception == null)
                return;

            // Initialize a list to hold the log messages
            List<string> messages = new List<string>
            {
                new string('*', 120),
                string.Format("Exception Error: {0}", exception.GetType().Name + ": " + exception.Message),
                new string('*', 120),
                "Source: " + exception.Source,
                "TargetSite: " + exception.TargetSite
            };

            // Add all footprints of the exception to the log messages
            foreach (string item in GetAllFootprints(exception))
            {
                messages.Add(item);
            }
            messages.Add(new string('*', 120));

            // Log each message based on the error flag
            foreach (string message in messages)
            {
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, message);
                else
                    WriteDebugPrefixNoAutoLog(logPrefix, message);
            }
        }

        /// <summary>
        /// Logs detailed information about a WebException, including its type, message, source, target site, and additional protocol error details.
        /// </summary>
        /// <param name="exception">The WebException to log.</param>
        /// <param name="debugLevel">The debug level for logging.</param>
        /// <param name="commonLogger">The logger to use for logging.</param>
        /// <param name="logPrefix">The prefix to add to the log message.</param>
        /// <param name="error">Indicates whether the message is an error.</param>
        public static void Log(this System.Net.WebException exception, int debugLevel, ICommonLogger commonLogger, string logPrefix = "", bool error = false)
        {
            // Check if logging is enabled for the specified debug level
            if (!commonLogger.ToLog(debugLevel))
                return;

            // Check if the WebException is not null
            if (exception == null)
                return;

            // Initialize a list to hold the log messages
            List<string> messages = new List<string>
            {
                new string('*', 120),
                string.Format("Exception Error: {0}", exception.GetType().Name + ": " + exception.Message),
                new string('*', 120),
                "Source: " + exception.Source,
                "TargetSite: " + exception.TargetSite
            };

            // Check if the WebException status is a protocol error
            if (exception.Status == WebExceptionStatus.ProtocolError)
            {
                // Get the status code and other details from the response
                int code = (int)((HttpWebResponse)exception.Response).StatusCode;
                messages.Add("Status Code: " + code.ToString());
                messages.Add("Status Description: " + ((HttpWebResponse)exception.Response).StatusDescription);
                messages.Add("Server: " + ((HttpWebResponse)exception.Response).Server);
                messages.Add("Method: " + ((HttpWebResponse)exception.Response).Method);
                messages.Add("Response Uri: " + ((HttpWebResponse)exception.Response).ResponseUri.OriginalString);
            }

            // Add all footprints of the exception to the log messages
            foreach (string item in GetAllFootprints(exception))
            {
                messages.Add(item);
            }
            messages.Add(new string('*', 120));

            // Log each message based on the error flag
            foreach (string item in messages)
            {
                if (error)
                    WriteErrorPrefixNoAutoLog(logPrefix, item);
                else
                    WriteDebugPrefixNoAutoLog(logPrefix, item);
            }
        }

        #endregion

        /// <summary>
        /// Checks if an object has a method with the specified name.
        /// </summary>
        /// <param name="objectToCheck">The object to check for the method.</param>
        /// <param name="methodName">The name of the method to check for.</param>
        /// <returns>True if the method exists; otherwise, false.</returns>
        public static bool HasMethod(this object objectToCheck, string methodName)
        {
            // Get all methods of the LoggingExtensions class
            MethodInfo[] methodsOfLoggingExtensions = Type.GetType("AdvancedLogging.Logging.LoggingExtensions")
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            // Iterate through each method to check for a matching method name and parameter type
            foreach (var thisMethod in methodsOfLoggingExtensions.Where(p => p.Name.ToLower() == methodName.ToLower()))
            {
                ParameterInfo[] pi = thisMethod.GetParameters();
                if (pi.Count() > 0 && pi[0].ParameterType == objectToCheck.GetType())
                {
                    return true;
                }
            }

            // Define binding flags to include non-public, public, instance, and static methods
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var type = objectToCheck.GetType();

            // Check if the object has a method with the specified name
            return type.GetMethod(methodName, flags) != null;
        }

        /// <summary>
        /// Converts an object to the specified type.
        /// </summary>
        /// <param name="source">The object to convert.</param>
        /// <param name="dest">The type to convert the object to.</param>
        /// <returns>The converted object.</returns>
        public static object ToType(this object source, Type dest)
        {
            // Convert the source object to the specified destination type
            return Convert.ChangeType(source, dest);
        }
    }
}