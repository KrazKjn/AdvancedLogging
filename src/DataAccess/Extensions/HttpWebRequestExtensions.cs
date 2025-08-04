using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// HttpWebRequest extensions adding retry logic.
    /// </summary>
    [CLSCompliant(false)]
    public static class HttpWebRequestExtensions
    {

        /// <summary>
        /// Dumps the HttpWebRequest details to the logger.
        /// </summary>
        public static void Dump(this HttpWebRequest httpWebRequest, ICommonLogger logger, int debugLevel, string strLogPrefix, bool error = false)
        {
            if (error)
                httpWebRequest.DumpError(logger, debugLevel, strLogPrefix);
            else
                httpWebRequest.DumpDebug(logger, debugLevel, strLogPrefix);
        }

        /// <summary>
        /// Dumps the HttpWebRequest error details to the logger.
        /// </summary>
        public static void DumpError(this HttpWebRequest httpWebRequest, ICommonLogger logger, int iDebugLevel, string strLogPrefix)
        {
            if (!logger.ToLog(iDebugLevel))
                return;
            string strMessage;
            strMessage = string.Format("Address: {0}", httpWebRequest.Address);
            LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, strMessage);
            strMessage = string.Format("Timeout: {0}", httpWebRequest.Timeout.ToString());
            LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, strMessage);
            if (httpWebRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = httpWebRequest.Credentials?.GetCredential(httpWebRequest.RequestUri, "");
                if (nc == null)
                {
                    strMessage = "Credentials: None";
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, strMessage);
                }
                else
                {
                    if (nc.Domain == "" && nc.UserName == "")
                        strMessage = "Credentials: None";
                    else
                        strMessage = string.Format("Credentials: {0}{1}", nc.Domain == "" ? "" : nc.Domain + "\\", nc.UserName);
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, strMessage);
                }
            }
            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    strMessage = string.Format("Object Data  : {0}", DataObjectDumper.Dump(httpWebRequest, logger: logger));
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, strMessage);
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Dumps the HttpWebRequest debug details to the logger.
        /// </summary>
        public static void DumpDebug(this HttpWebRequest httpWebRequest, ICommonLogger logger, int iDebugLevel, string strLogPrefix)
        {
            string strMessage;
            strMessage = string.Format("Address: {0}", httpWebRequest.Address);
            LoggingUtils.WriteDebugPrefixNoAutoLog(logger, iDebugLevel, strLogPrefix, strMessage);
            strMessage = string.Format("Timeout: {0}", httpWebRequest.Timeout.ToString());
            LoggingUtils.WriteDebugPrefixNoAutoLog(logger, iDebugLevel, strLogPrefix, strMessage);
            if (httpWebRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = httpWebRequest.Credentials?.GetCredential(httpWebRequest.RequestUri, "");
                if (nc == null)
                {
                    strMessage = "Credentials: None";
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, iDebugLevel, strLogPrefix, strMessage);
                }
                else
                {
                    if (nc.Domain == "" && nc.UserName == "")
                        strMessage = "Credentials: None";
                    else
                        strMessage = string.Format("Credentials: {0}{1}", nc.Domain == "" ? "" : nc.Domain + "\\", nc.UserName);
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, iDebugLevel, strLogPrefix, strMessage);
                }
            }
            if (logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    strMessage = string.Format("Object Data  : {0}", DataObjectDumper.Dump(httpWebRequest, logger: logger));
                    LoggingUtils.WriteDebugPrefixNoAutoLog(logger, iDebugLevel, strLogPrefix, strMessage);
                }
                catch (Exception ex)
                {
                    LoggingUtils.WriteErrorPrefixNoAutoLog(logger, strLogPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        private static T ExecuteWithRetry<T>(this HttpWebRequest httpWebRequest, ICommonLogger logger, ILoggingContext loggingContext, Func<HttpWebRequest, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { httpWebRequest, retries, retryWaitMS, autoTimeoutIncrement }))
            {
                try
                {
                    T result = default;
                    bool success = true;
                    for (int i = 0; i < (retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        int timeoutIncrement = 0;
                        try
                        {
                            sw = new Stopwatch();
                            sw?.Start();
                            result = action(httpWebRequest);
                            sw?.Stop();
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(logger, loggingContext, ref sw, vAutoLogFunction, httpWebRequest.RequestUri.ToString(), LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            if (!success)
                            {
                                vAutoLogFunction.WriteLog($"{action.Method.Name}: Retry is Successful!");
                            }
                            return result;
                        }
                        catch (WebException ex)
                        {
                            ExtensionsFunctions.HandleException(logger, loggingContext, $"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            httpWebRequest = ExtensionsFunctions.CreateHttpWebRequest(httpWebRequest, vAutoLogFunction, ex.Status == WebExceptionStatus.Timeout ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException(logger, loggingContext, $"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                        }
                        ExtensionsFunctions.PerformRetryDelay(vAutoLogFunction, retryWaitMS);
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { httpWebRequest, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the response from the HttpWebRequest with retry logic.
        /// </summary>
        public static WebResponse GetResponse(this HttpWebRequest httpWebRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return httpWebRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetResponse(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously gets the response from the HttpWebRequest with retry logic.
        /// </summary>
        public static async Task<WebResponse> GetResponseAsync(this HttpWebRequest httpWebRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpWebRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetResponseAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Gets the response stream from the HttpWebRequest with retry logic.
        /// </summary>
        public static Stream GetRequestStream(this HttpWebRequest httpWebRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return httpWebRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetRequestStream(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously gets the response stream from the HttpWebRequest with retry logic.
        /// </summary>
        public static async Task<Stream> GetRequestStreamAsync(this HttpWebRequest httpWebRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpWebRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetRequestStreamAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}
