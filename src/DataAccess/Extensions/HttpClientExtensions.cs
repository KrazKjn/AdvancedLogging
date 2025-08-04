using AdvancedLogging.Constants;
using AdvancedLogging.Logging;
using AdvancedLogging.Utilities;
using Polly;
using Polly.Retry;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// HttpClient extensions adding retry functionality using Polly.
    /// </summary>
    public static class HttpClientExtensions
    {
        private static async Task<T> ExecuteWithRetryAsync<T>(
            this HttpClient httpClient,
            Func<HttpClient, Task<T>> action,
            int retries,
            int retryWaitMS,
            int autoTimeoutIncrement = 0) // autoTimeoutIncrement is no longer used but kept for signature compatibility.
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { httpClient, retries, retryWaitMS }))
            {
                // Note: autoTimeoutIncrement is not supported with Polly's default retry mechanism
                // when using a shared HttpClient. A more advanced setup with Polly's TimeoutPolicy
                // and CancellationToken would be needed. For now, we are simplifying the implementation.
                if (autoTimeoutIncrement > 0)
                {
                    vAutoLogFunction.WriteWarn("autoTimeoutIncrement is not supported in this version of ExecuteWithRetryAsync and will be ignored.");
                }

                var retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .Or<TaskCanceledException>() // Thrown on timeout
                    .WaitAndRetryAsync(retries,
                        attempt => TimeSpan.FromMilliseconds(retryWaitMS),
                        (exception, timeSpan, attempt, context) =>
                        {
                            vAutoLogFunction.WriteWarn($"Retry {attempt} due to {exception.GetType().Name}: {exception.Message}. Waiting {timeSpan.TotalMilliseconds}ms before next retry.");
                        });

                return await retryPolicy.ExecuteAsync(async () =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        T result = await action(httpClient);
                        sw.Stop();

                        if (result is HttpResponseMessage response)
                        {
                            string responseUri = response.RequestMessage.RequestUri.ToString();
                            LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, responseUri, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                        }
                        else
                        {
                            LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, "", LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                        }

                        return result;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        vAutoLogFunction.WriteError("Exception during HTTP request", ex);
                        throw; // Re-throw to allow Polly to handle it
                    }
                });
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
            return await httpClient.ExecuteWithRetryAsync(r => r.DeleteAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.DeleteAsync(requestUri, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetAsync(requestUri, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetByteArrayAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetStreamAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.GetStringAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.PostAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.PostAsync(requestUri, content), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpRequestMessage request, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(r => r.SendAsync(request), retries, retryWaitMS, autoTimeoutIncrement);
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
        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(r => r.SendAsync(request, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
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
            return await httpClient.ExecuteWithRetryAsync(r => r.SendAsync(request, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}