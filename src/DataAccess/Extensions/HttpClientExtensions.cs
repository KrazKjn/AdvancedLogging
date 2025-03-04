using AdvancedLogging.Constants;
using AdvancedLogging.Logging;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// HttpClient extensions adding retry functionality.
    /// </summary>
    public static class HttpClientExtensions
    {
        private static T ExecuteWithRetry<T>(this HttpClient httpClient, Func<HttpClient, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { httpClient, retries, retryWaitMS, autoTimeoutIncrement }))
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
                            result = action(httpClient);
                            ((Task<HttpResponseMessage>)(object)result).Wait();
                            sw?.Stop();
                            if (sw != null)
                            {
                                string responseUri = ((Task<HttpResponseMessage>)(object)result).Result.RequestMessage.RequestUri.ToString();
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, responseUri, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            if (!success)
                            {
                                vAutoLogFunction.WriteLog($"{action.Method.Name}: Retry is Successful!");
                            }
                            return result;
                        }
                        catch (HttpRequestException ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            httpClient = ExtensionsFunctions.CreateHttpClient(httpClient, vAutoLogFunction, ex.Message.Contains("timeout") ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            if (ex.InnerException?.Message == "A task was canceled.")
                            {
                                httpClient = ExtensionsFunctions.CreateHttpClient(httpClient, vAutoLogFunction, timeoutIncrement);
                            }
                        }
                        ExtensionsFunctions.PerformRetryDelay($"{action.Method.Name}", vAutoLogFunction, retryWaitMS);
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { httpClient, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Sends a DELETE request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.DeleteAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a DELETE request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, Uri requestUri, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.DeleteAsync(requestUri, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URL, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URL to which the request is sent.</param>
        /// <param name="completionOption">The HttpCompletionOption value to use when sending the request.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="completionOption">The HttpCompletionOption value to use when sending the request.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URL, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="completionOption">The HttpCompletionOption value to use when sending the request.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUri, HttpCompletionOption completionOption, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetAsync(requestUri, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URL, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetry(r => r.GetAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The byte[] response.</returns>
        public static async Task<byte[]> GetByteArrayAsync(this HttpClient httpClient, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetByteArrayAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The Stream response.</returns>
        public static async Task<Stream> GetStreamAsync(this HttpClient httpClient, Uri requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetStreamAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a GET request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The string response.</returns>
        public static async Task<string> GetStringAsync(this HttpClient httpClient, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.GetStringAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a POST request to the specified URI, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, string requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.PostAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a POST request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, Uri requestUri, HttpContent content, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.PostAsync(requestUri, content), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a PUT request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> PutAsync(this HttpClient httpClient, string requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends a PUT request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> PutAsync(this HttpClient httpClient, Uri requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends an HTTP request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpRequestMessage request, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.SendAsync(request), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends an HTTP request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.SendAsync(request, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Sends an HTTP request to the specified URI with the specified content, retrying the request if it fails.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="completionOption">The HttpCompletionOption value to use when sending the request.</param>
        /// <param name="retries">The number of times to retry the request if it fails.</param>
        /// <param name="retryWaitMS">The wait time in milliseconds between retries.</param>
        /// <param name="autoTimeoutIncrement">The increment value for the timeout in case of a timeout exception.</param>
        /// <returns>The HTTP response message.</returns>
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpRequestMessage request, HttpCompletionOption completionOption, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await httpClient.ExecuteWithRetry(r => r.SendAsync(request, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}