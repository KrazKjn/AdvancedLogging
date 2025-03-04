using AdvancedLogging.Constants;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// Shared functions used by the extensions.
    /// </summary>
    [CLSCompliant(false)]
    public static class ExtensionsFunctions
    {
        private const int retryMessageAfterWaitMS = 250;
        /// <summary>
        /// Performs a delay before retrying a function, with logging.
        /// </summary>
        /// <param name="functionName">The name of the function to log.</param>
        /// <param name="logFunction">The logging function to use.</param>
        /// <param name="retryWaitMS">The delay in milliseconds before retrying.</param>
        public static void PerformRetryDelay(string functionName, AutoLogFunction logFunction, int retryWaitMS)
        {
            if (retryWaitMS > retryMessageAfterWaitMS)
            {
                var endTime = DateTime.Now.AddMilliseconds(retryWaitMS);
                logFunction.WriteLog($"{functionName}: Waiting {retryWaitMS / 1000} seconds before retrying...");
                while (endTime > DateTime.Now && ApplicationSettings.IsRunning)
                {
                    Thread.Sleep(250);
                }
            }
            else
            {
                Thread.Sleep(retryWaitMS);
            }
        }

        /// <summary>
        /// Asynchronously performs a delay before retrying a function, with logging.
        /// </summary>
        /// <param name="functionName">The name of the function to log.</param>
        /// <param name="logFunction">The logging function to use.</param>
        /// <param name="retryWaitMS">The delay in milliseconds before retrying.</param>
        public static async Task PerformRetryDelayAsync(string functionName, AutoLogFunction logFunction, int retryWaitMS)
        {
            if (retryWaitMS > retryMessageAfterWaitMS)
            {
                var endTime = DateTime.Now.AddMilliseconds(retryWaitMS);
                logFunction.WriteLog($"{functionName}: Waiting {retryWaitMS / 1000} seconds before retrying...");
                while (endTime > DateTime.Now && ApplicationSettings.IsRunning)
                {
                    await Task.Delay(250);
                }
            }
            else
            {
                await Task.Delay(retryWaitMS);
            }
        }

        /// <summary>
        /// Asynchronously performs a delay before retrying a function, with logging and cancellation support.
        /// </summary>
        /// <param name="functionName">The name of the function to log.</param>
        /// <param name="logFunction">The logging function to use.</param>
        /// <param name="retryWaitMS">The delay in milliseconds before retrying.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public static async Task PerformRetryDelayAsync(string functionName, AutoLogFunction logFunction, int retryWaitMS, CancellationToken cancellationToken)
        {
            if (retryWaitMS > retryMessageAfterWaitMS)
            {
                var endTime = DateTime.Now.AddMilliseconds(retryWaitMS);
                logFunction.WriteLog($"{functionName}: Waiting {retryWaitMS / 1000} seconds before retrying...");
                while (endTime > DateTime.Now && ApplicationSettings.IsRunning)
                {
                    await Task.Delay(250, cancellationToken);
                }
            }
            else
            {
                await Task.Delay(retryWaitMS, cancellationToken);
            }
        }

        /// <summary>
        /// Handles exceptions during a retry operation, with logging and timeout adjustments.
        /// </summary>
        /// <param name="functionName">The name of the function to log.</param>
        /// <param name="vAutoLogFunction">The logging function to use.</param>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="attempt">The current retry attempt number.</param>
        /// <param name="maxRetries">The maximum number of retry attempts.</param>
        /// <param name="success">A reference to a boolean indicating success.</param>
        /// <param name="timeoutIncrement">A reference to the timeout increment value.</param>
        /// <param name="autoTimeoutIncrement">The automatic timeout increment value.</param>
        public static void HandleException(string functionName, AutoLogFunction vAutoLogFunction, Exception ex, int attempt, int maxRetries, ref bool success, ref int timeoutIncrement, int autoTimeoutIncrement)
        {
            if (attempt == (maxRetries - 1))
            {
                vAutoLogFunction.LogFunction(new { attempt, maxRetries, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                throw ex;
            }

            if (success)
            {
                success = false;
                vAutoLogFunction.WriteLog($"{functionName}: Retrying ({attempt + 1}/{maxRetries} ...", ex);
            }
            else
            {
                vAutoLogFunction.WriteLog($"{functionName}: Retrying ({attempt + 1}/{maxRetries} ... (Error: {ex.Message})");
            }

            if (ex is WebException webEx && webEx.Status == WebExceptionStatus.Timeout)
            {
                timeoutIncrement = autoTimeoutIncrement;
            }
        }

        /// <summary>
        /// Create a NEW HttpWebRequest from an existing HttpWebRequest, with logging and timeout adjustments.
        /// </summary>
        /// <param name="httpWebRequest">The existing HttpWebRequest to copy.</param>
        /// <param name="vAutoLogFunction">The logging instance to use.</param>
        /// <param name="timeoutIncrement">The timeout increment value to apply.</param>
        /// <returns>A new HttpWebRequest instance with the copied properties.</returns>
        public static HttpWebRequest CreateHttpWebRequest(HttpWebRequest httpWebRequest, AutoLogFunction vAutoLogFunction, int timeoutIncrement = 0)
        {
            HttpWebRequest request = WebRequest.Create(httpWebRequest.RequestUri) as HttpWebRequest;
            var propInfo = httpWebRequest.GetType().GetProperties();
            foreach (var item in propInfo)
            {
                if (!item.CanWrite)
                    continue;
                try
                {
                    if (item.CanWrite && item.CanRead)
                    {
                        try
                        {
                            vAutoLogFunction.WriteDebugFormat(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] + 2, "Copying Property [{0}] ...", item.Name);
                            switch (item.Name)
                            {
                                case "Headers":
                                    if (httpWebRequest.Headers != null)
                                    {
                                        foreach (var oHeader in httpWebRequest.Headers)
                                        {
                                            switch ((string)oHeader)
                                            {
                                                case "Content-Type":
                                                case "Host":
                                                    // Set with a Property
                                                    break;
                                                default:
                                                    vAutoLogFunction.WriteDebug("Setting HttpWebRequest Property [" + item.Name + "].[" + (string)oHeader + "] = " + httpWebRequest.Headers[(string)oHeader] + " ...");
                                                    request.Headers.Add((string)oHeader, httpWebRequest.Headers[(string)oHeader]);
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                case "ClientCertificates":
                                    if (httpWebRequest.ClientCertificates != null)
                                    {
                                        foreach (var oCerts in httpWebRequest.ClientCertificates)
                                        {
                                            vAutoLogFunction.WriteDebug("Setting ClientCertificates Property [" + item.Name + "].[" + oCerts.Subject + "] = " + oCerts.Issuer + " ...");
                                            request.ClientCertificates.Add(oCerts);
                                        }
                                    }
                                    break;
                                case "CookieContainer":
                                    break;
                                case "ContentLength":
                                    if (item.GetValue(httpWebRequest, null) is int iTimeout)
                                    {
                                        if (iTimeout > 0)
                                        {
                                            vAutoLogFunction.WriteDebug("Setting HttpWebRequest Property [" + item.Name + "] = " + iTimeout + " ...");
                                            request.GetType().GetProperty(item.Name).SetValue(request, iTimeout, null);
                                        }
                                    }
                                    break;
                                case "Connection":
                                    if (!string.IsNullOrEmpty((string)item.GetValue(httpWebRequest, null)))
                                    {
                                        string strValue = ((string)item.GetValue(httpWebRequest, null)).ToLower();
                                        // "Keep-Alive" and "Close"
                                        if (!(strValue == "Keep-Alive".ToLower() || strValue == "Close".ToLower()))
                                        {
                                            vAutoLogFunction.WriteDebug("Setting HttpWebRequest Property [" + item.Name + "] = " + item.GetValue(httpWebRequest, null) + " ...");
                                            request.GetType().GetProperty(item.Name).SetValue(request, item.GetValue(httpWebRequest, null), null);
                                        }
                                    }
                                    break;
                                default:
                                    vAutoLogFunction.WriteDebug("Setting HttpWebRequest Property [" + item.Name + "] = " + item.GetValue(httpWebRequest, null) + " ...");
                                    request.GetType().GetProperty(item.Name).SetValue(request, item.GetValue(httpWebRequest, null), null);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("Error Copying Property [{0}] ... [{1}].", item.Name, ex.Message);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    vAutoLogFunction.WriteError("Setting HttpWebRequest Property [" + item.Name + "] = " + item.GetValue(httpWebRequest, null) + " ...", ex2);
                }
            }
            if (timeoutIncrement > 0)
            {
                // Set the new Timeout AFTER we copy the Properties
                request.Timeout += timeoutIncrement;
            }
            return request;
        }

        /// <summary>
        /// Create a NEW WebRequest from an existing WebRequest, with logging and timeout adjustments.
        /// </summary>
        /// <param name="webRequest">The existing WebRequest to copy.</param>
        /// <param name="vAutoLogFunction">The logging instance to use.</param>
        /// <param name="timeoutIncrement">The timeout increment value to apply.</param>
        /// <returns>A new WebRequest instance with the copied properties.</returns>
        public static WebRequest CreateWebRequest(WebRequest webRequest, AutoLogFunction vAutoLogFunction, int timeoutIncrement = 0)
        {
            return CreateHttpWebRequest((HttpWebRequest)webRequest, vAutoLogFunction, timeoutIncrement);
        }

        /// <summary>
        /// Create a NEW HttpClient from an existing HttpClient, with logging and timeout adjustments.
        /// </summary>
        /// <param name="httpClient">The existing HttpClient to copy.</param>
        /// <param name="vAutoLogFunction">The logging instance to use.</param>
        /// <param name="timeoutIncrement">The timeout increment value to apply.</param>
        /// <returns>A new HttpClient instance with the copied properties.</returns>
        public static HttpClient CreateHttpClient(HttpClient httpClient, AutoLogFunction vAutoLogFunction, int timeoutIncrement = 0)
        {
            try
            {

                HttpClient request = new HttpClient()
                {
                    BaseAddress = httpClient.BaseAddress,
                    MaxResponseContentBufferSize = httpClient.MaxResponseContentBufferSize
                };
                if (timeoutIncrement > 0)
                {
                    // Set the new Timeout AFTER we copy the Properties
                    request.Timeout = request.Timeout.Add(new TimeSpan(timeoutIncrement * 1000));
                }
                return request;
            }
            catch (Exception ex)
            {
                vAutoLogFunction.WriteError("Creating new HttpClient.", ex);
                throw;
            }
        }

        /// <summary>
        /// Create a NEW WebClient from an existing HttpWebRequest, with logging and timeout adjustments.
        /// </summary>
        /// <param name="webClient">The existing HttpWebRequest to copy.</param>
        /// <param name="vAutoLogFunction">The logging instance to use.</param>
        /// <param name="timeoutIncrement">The timeout increment value to apply.</param>
        /// <returns>A new HttpWebRequest instance with the copied properties.</returns>
        public static WebClient CreateWebClient(WebClient webClient, AutoLogFunction vAutoLogFunction, int timeoutIncrement = 0)
        {
            try
            {

                WebClientExt request = new WebClientExt()
                {
                    BaseAddress = webClient.BaseAddress,
                    CachePolicy = webClient.CachePolicy,
                    Credentials = webClient.Credentials,
                    Encoding = webClient.Encoding,
                    Headers = webClient.Headers,
                    Proxy = webClient.Proxy,
                    QueryString = webClient.QueryString,
                    UseDefaultCredentials = webClient.UseDefaultCredentials,
                    Site = webClient.Site,
                    Timeout = ((WebClientExt)webClient).Timeout
                };
                if (timeoutIncrement > 0)
                {
                    // Set the new Timeout AFTER we copy the Properties
                    request.Timeout += timeoutIncrement;
                }
                return request;
            }
            catch (Exception ex)
            {
                vAutoLogFunction.WriteError("Creating new HttpClient.", ex);
                throw;
            }
        }

        public static T ExecuteWithRetry<T>(Func<T> action, string actionName, AutoLogFunction vAutoLogFunction, int retries, int retryWaitMS, int autoTimeoutIncrement)
        {
            bool success = true;
            T result = default;
            for (int i = 0; i < retries + 1; i++)
            {
                Stopwatch sw = null;
                int timeoutIncrement = 0;
                try
                {
                    sw = new Stopwatch();
                    sw?.Start();
                    result = action();
                    sw?.Stop();
                    if (sw != null)
                    {
                        LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, actionName, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                    }
                    if (!success)
                    {
                        vAutoLogFunction.WriteLog($"{actionName}: Retry is Successful!");
                    }
                    return result;
                }
                catch (WebException ex)
                {
                    HandleException(actionName, vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                }
                catch (Exception ex)
                {
                    HandleException(actionName, vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                }
                PerformRetryDelay(actionName, vAutoLogFunction, retryWaitMS);
            }
            return result;
        }
    }
}
