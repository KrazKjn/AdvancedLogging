using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
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
            ICommonLogger logger,
            ILoggingContext loggingContext,
            Func<HttpClient, Task<T>> action,
            int retries,
            int retryWaitMS,
            int autoTimeoutIncrement = 0) // autoTimeoutIncrement is no longer used but kept for signature compatibility.
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { httpClient, retries, retryWaitMS }))
            {
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
                            LoggingUtils.ProcessStopWatch(logger, loggingContext, ref sw, vAutoLogFunction, responseUri, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                        }
                        else
                        {
                            LoggingUtils.ProcessStopWatch(logger, loggingContext, ref sw, vAutoLogFunction, "", LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
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

        public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.DeleteAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> DeleteAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, Uri requestUri, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.DeleteAsync(requestUri, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetAsync(requestUri, completionOption, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, HttpCompletionOption completionOption, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetAsync(requestUri, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> GetAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<byte[]> GetByteArrayAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetByteArrayAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<Stream> GetStreamAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, Uri requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetStreamAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<string> GetStringAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.GetStringAsync(requestUri), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.PostAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, Uri requestUri, HttpContent content, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.PostAsync(requestUri, content), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> PutAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, string requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> PutAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, Uri requestUri, HttpContent content, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.PutAsync(requestUri, content, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, HttpRequestMessage request, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.SendAsync(request), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, HttpRequestMessage request, CancellationToken cancellationToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.SendAsync(request, cancellationToken), retries, retryWaitMS, autoTimeoutIncrement);
        }

        public static async Task<HttpResponseMessage> SendAsync(this HttpClient httpClient, ICommonLogger logger, ILoggingContext loggingContext, HttpRequestMessage request, HttpCompletionOption completionOption, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await httpClient.ExecuteWithRetryAsync(logger, loggingContext, r => r.SendAsync(request, completionOption), retries, retryWaitMS, autoTimeoutIncrement);
        }
    }
}