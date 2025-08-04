using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// WebRequest extensions adding retry functionality.
    /// </summary>
    [CLSCompliant(false)]
    public static class WebRequestExtensions
    {
        private static T ExecuteWithRetry<T>(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, Func<WebRequest, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { webRequest, retries, retryWaitMS, autoTimeoutIncrement }))
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
                            result = action(webRequest);
                            sw?.Stop();
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(logger, loggingContext, ref sw, vAutoLogFunction, webRequest.RequestUri.ToString(), LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
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
                            webRequest = ExtensionsFunctions.CreateWebRequest(webRequest, vAutoLogFunction, ex.Status == WebExceptionStatus.Timeout ? timeoutIncrement : 0);
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
                    vAutoLogFunction.LogFunction(new { webRequest, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Begins an asynchronous request for a Stream object to use to write data.
        /// </summary>
        public static IAsyncResult BeginGetRequestStream(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, AsyncCallback callback, object state, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.BeginGetRequestStream(callback, state), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Begins an asynchronous request to an Internet resource.
        /// </summary>
        public static IAsyncResult BeginGetResponse(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, AsyncCallback callback, object state, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.BeginGetResponse(callback, state), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Ends an asynchronous request for a Stream object to use to write data.
        /// </summary>
        public static Stream EndGetRequestStream(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, IAsyncResult asyncResult, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.EndGetRequestStream(asyncResult), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Ends an asynchronous request to an Internet resource.
        /// </summary>
        public static WebResponse EndGetResponse(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, IAsyncResult asyncResult, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.EndGetResponse(asyncResult), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a response from an Internet resource.
        /// </summary>
        public static async Task<WebResponse> GetResponseAsync(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await webRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetResponseAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a response from an Internet resource.
        /// </summary>
        public static WebResponse GetResponse(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetResponse(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        public static Task<Stream> GetRequestStreamAsync(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetRequestStreamAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        public static Stream GetRequestStream(this WebRequest webRequest, ICommonLogger logger, ILoggingContext loggingContext, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(logger, loggingContext, r => r.GetRequestStream(), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}