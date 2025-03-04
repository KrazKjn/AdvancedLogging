using AdvancedLogging.Constants;
using AdvancedLogging.Logging;
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
        private static T ExecuteWithRetry<T>(this WebRequest webRequest, Func<WebRequest, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webRequest, retries, retryWaitMS, autoTimeoutIncrement }))
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
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, webRequest.RequestUri.ToString(), LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
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
                            webRequest = ExtensionsFunctions.CreateWebRequest(webRequest, vAutoLogFunction, ex.Status == WebExceptionStatus.Timeout ? timeoutIncrement : 0);
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
                    vAutoLogFunction.LogFunction(new { webRequest, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Begins an asynchronous request for a Stream object to use to write data.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="callback">The System.AsyncCallback delegate.</param>
        /// <param name="state">An object containing state information for this asynchronous request.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>An System.IAsyncResult that references the asynchronous request.</returns>
        public static IAsyncResult BeginGetRequestStream(this WebRequest webRequest, AsyncCallback callback, object state, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.BeginGetRequestStream(callback, state), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Begins an asynchronous request to an Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns></returns>
        public static IAsyncResult BeginGetResponse(this WebRequest webRequest, AsyncCallback callback, object state, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.BeginGetResponse(callback, state), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Ends an asynchronous request for a Stream object to use to write data.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="asyncResult">An System.IAsyncResult that references a pending request for a stream.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.IO.Stream to use to write request data.</returns>
        public static Stream EndGetRequestStream(this WebRequest webRequest, IAsyncResult asyncResult, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.EndGetRequestStream(asyncResult), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Ends an asynchronous request to an Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="asyncResult">An System.IAsyncResult that references a pending request for a stream.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.Net.WebResponse that contains the response from the Internet resource.</returns>
        public static WebResponse EndGetResponse(this WebRequest webRequest, IAsyncResult asyncResult, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.EndGetResponse(asyncResult), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a response from an Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.Net.WebResponse that contains the response from the Internet resource.</returns>
        public static async Task<WebResponse> GetResponseAsync(this WebRequest webRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await webRequest.ExecuteWithRetry(r => r.GetResponseAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a response from an Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.Net.WebResponse that contains the response from the Internet resource.</returns>
        public static WebResponse GetResponse(this WebRequest webRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.GetResponse(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.IO.Stream to use to write request data.</returns>
        public static Task<Stream> GetRequestStreamAsync(this WebRequest webRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.GetRequestStreamAsync(), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        /// <param name="webRequest">The WebRequest instance.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>A System.IO.Stream to use to write request data.</returns>
        public static Stream GetRequestStream(this WebRequest webRequest, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webRequest.ExecuteWithRetry(r => r.GetRequestStream(), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}