using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static AdvancedLogging.Utilities.LoggingUtils;

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
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="debugLevel">The debug level.</param>
        /// <param name="commonLogger">The common logger instance.</param>
        /// <param name="strLogPrefix">The log prefix.</param>
        /// <param name="error">Indicates if the dump is for an error.</param>
        public static void Dump(this HttpWebRequest httpWebRequest, int debugLevel, ICommonLogger commonLogger, string strLogPrefix, bool error = false)
        {
            if (error)
                httpWebRequest.DumpError(debugLevel, commonLogger, strLogPrefix);
            else
                httpWebRequest.DumpDebug(debugLevel, strLogPrefix);
        }

        /// <summary>
        /// Dumps the HttpWebRequest error details to the logger.
        /// </summary>
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="iDebugLevel">The debug level.</param>
        /// <param name="commonLogger">The common logger instance.</param>
        /// <param name="strLogPrefix">The log prefix.</param>
        public static void DumpError(this HttpWebRequest httpWebRequest, int iDebugLevel, ICommonLogger commonLogger, string strLogPrefix)
        {
            if (!commonLogger.ToLog(iDebugLevel))
                return;
            string strMessage;
            strMessage = string.Format("Address: {0}", httpWebRequest.Address);
            WriteErrorPrefixNoAutoLog(strLogPrefix, strMessage);
            strMessage = string.Format("Timeout: {0}", httpWebRequest.Timeout.ToString());
            WriteErrorPrefixNoAutoLog(strLogPrefix, strMessage);
            if (httpWebRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = httpWebRequest.Credentials?.GetCredential(httpWebRequest.RequestUri, "");
                if (nc == null)
                {
                    strMessage = "Credentials: None";
                    WriteErrorPrefixNoAutoLog(strLogPrefix, strMessage);
                }
                else
                {
                    if (nc.Domain == "" && nc.UserName == "")
                        strMessage = "Credentials: None";
                    else
                        strMessage = string.Format("Credentials: {0}{1}", nc.Domain == "" ? "" : nc.Domain + "\\", nc.UserName);
                    WriteErrorPrefixNoAutoLog(strLogPrefix, strMessage);
                }
            }
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    strMessage = string.Format("Object Data  : {0}", DataObjectDumper.Dump(httpWebRequest));
                    WriteErrorPrefixNoAutoLog(strLogPrefix, strMessage);
                }
                catch (Exception ex)
                {
                    WriteErrorPrefixNoAutoLog(strLogPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        /// <summary>
        /// Dumps the HttpWebRequest debug details to the logger.
        /// </summary>
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="iDebugLevel">The debug level.</param>
        /// <param name="strLogPrefix">The log prefix.</param>
        public static void DumpDebug(this HttpWebRequest httpWebRequest, int iDebugLevel, string strLogPrefix)
        {
            string strMessage;
            strMessage = string.Format("Address: {0}", httpWebRequest.Address);
            WriteDebugPrefixNoAutoLog(iDebugLevel, strLogPrefix, strMessage);
            strMessage = string.Format("Timeout: {0}", httpWebRequest.Timeout.ToString());
            WriteDebugPrefixNoAutoLog(iDebugLevel, strLogPrefix, strMessage);
            if (httpWebRequest.Credentials != null)
            {
                System.Net.NetworkCredential nc = httpWebRequest.Credentials?.GetCredential(httpWebRequest.RequestUri, "");
                if (nc == null)
                {
                    strMessage = "Credentials: None";
                    WriteDebugPrefixNoAutoLog(iDebugLevel, strLogPrefix, strMessage);
                }
                else
                {
                    if (nc.Domain == "" && nc.UserName == "")
                        strMessage = "Credentials: None";
                    else
                        strMessage = string.Format("Credentials: {0}{1}", nc.Domain == "" ? "" : nc.Domain + "\\", nc.UserName);
                    WriteDebugPrefixNoAutoLog(iDebugLevel, strLogPrefix, strMessage);
                }
            }
            if (ApplicationSettings.Logger?.LogLevel >= DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues])
            {
                try
                {
                    strMessage = string.Format("Object Data  : {0}", DataObjectDumper.Dump(httpWebRequest));
                    WriteDebugPrefixNoAutoLog(iDebugLevel, strLogPrefix, strMessage);
                }
                catch (Exception ex)
                {
                    WriteErrorPrefixNoAutoLog(strLogPrefix, "Error 'Dumping' object of type [System.Net.HttpWebRequest].", ex);
                }
            }
        }

        private static T ExecuteWithRetry<T>(this HttpWebRequest httpWebRequest, Func<HttpWebRequest, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { httpWebRequest, retries, retryWaitMS, autoTimeoutIncrement }))
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
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, httpWebRequest.RequestUri.ToString(), LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            if (!success)
                            {
                                vAutoLogFunction.WriteLog($"{action.Method.Name}: Retry is Successful!");
                            }
                            return result;
                        }
                        catch (WebException ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            httpWebRequest = ExtensionsFunctions.CreateHttpWebRequest(httpWebRequest, vAutoLogFunction, ex.Status == WebExceptionStatus.Timeout ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                        }
                        ExtensionsFunctions.PerformRetryDelay($"{action.Method.Name}", vAutoLogFunction, retryWaitMS);
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
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The automatic timeout increment.</param>
        /// <returns>The WebResponse from the HttpWebRequest.</returns>
        public static WebResponse GetResponse(this HttpWebRequest httpWebRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return httpWebRequest.ExecuteWithRetry(r => r.GetResponse(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously gets the response from the HttpWebRequest with retry logic.
        /// </summary>
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The automatic timeout increment.</param>
        /// <returns>The WebResponse from the HttpWebRequest.</returns>
        public static async Task<WebResponse> GetResponseAsync(this HttpWebRequest httpWebRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpWebRequest.ExecuteWithRetry(r => r.GetResponseAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Gets the response stream from the HttpWebRequest with retry logic.
        /// </summary>
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The automatic timeout increment.</param>
        /// <returns>The WebResponse Stream from the HttpWebRequest.</returns>
        public static Stream GetRequestStream(this HttpWebRequest httpWebRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return httpWebRequest.ExecuteWithRetry(r => r.GetRequestStream(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously gets the response stream from the HttpWebRequest with retry logic.
        /// </summary>
        /// <param name="httpWebRequest">The HttpWebRequest instance.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The automatic timeout increment.</param>
        /// <returns>The WebResponse Stream from the HttpWebRequest.</returns>
        public static async Task<Stream> GetRequestStreamAsync(this HttpWebRequest httpWebRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpWebRequest.ExecuteWithRetry(r => r.GetRequestStreamAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}
