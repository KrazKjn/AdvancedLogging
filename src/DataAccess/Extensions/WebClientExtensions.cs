using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using Polly;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// WebClient extensions adding retry logic.
    /// Note: WebClient is considered obsolete. Consider migrating to HttpClient.
    /// </summary>
    [CLSCompliant(false)]
    public static class WebClientExtensions
    {
        private static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            string actionName,
            int retries,
            int retryWaitMS,
            int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { actionName, retries, retryWaitMS }))
            {
                if (autoTimeoutIncrement > 0)
                {
                    vAutoLogFunction.WriteWarn("autoTimeoutIncrement is not supported and will be ignored for WebClient extensions.");
                }

                var retryPolicy = Policy
                    .Handle<WebException>()
                    .WaitAndRetryAsync(retries,
                        attempt => TimeSpan.FromMilliseconds(retryWaitMS),
                        (exception, timeSpan, attempt, context) =>
                        {
                            vAutoLogFunction.WriteWarn($"Retry {attempt} for {actionName} due to {exception.GetType().Name}: {exception.Message}. Waiting {timeSpan.TotalMilliseconds}ms before next retry.");
                        });

                return await retryPolicy.ExecuteAsync(async () =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        T result = await action();
                        sw.Stop();
                        LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, actionName, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        vAutoLogFunction.WriteError($"Exception during {actionName}", ex);
                        throw;
                    }
                });
            }
        }

        private static async Task ExecuteWithRetryAsync(
            Action action,
            string actionName,
            int retries,
            int retryWaitMS,
            int autoTimeoutIncrement = 0)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                action();
                return await Task.FromResult(true); // Wrap synchronous action
            }, actionName, retries, retryWaitMS, autoTimeoutIncrement);
        }


        /// <summary>
        /// Downloads data from the specified address with retry logic.
        /// </summary>
        public static byte[] DownloadData(this WebClient webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return ExecuteWithRetryAsync(() => Task.Run(() => webClient.DownloadData(address)), nameof(webClient.DownloadData), retries, retryWaitMS, autoTimeoutIncrement).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously downloads data from the specified address with retry logic.
        /// </summary>
        public async static Task<byte[]> DownloadDataAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await ExecuteWithRetryAsync(() => webClient.DownloadDataTaskAsync(address), nameof(webClient.DownloadDataTaskAsync), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Downloads a file from the specified address with retry logic.
        /// </summary>
        public static void DownloadFile(this WebClient webClient, string address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            ExecuteWithRetryAsync(() => webClient.DownloadFile(address, fileName), nameof(webClient.DownloadFile), retries, retryWaitMS, autoTimeoutIncrement).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously downloads a file from the specified address with retry logic.
        /// </summary>
        public async static Task DownloadFileAsync(this WebClient webClient, Uri address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            await ExecuteWithRetryAsync(() => webClient.DownloadFileTaskAsync(address, fileName), nameof(webClient.DownloadFileTaskAsync), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Downloads a string from the specified address with retry logic.
        /// </summary>
        public static string DownloadString(this WebClient webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return ExecuteWithRetryAsync(() => Task.Run(() => webClient.DownloadString(address)), nameof(webClient.DownloadString), retries, retryWaitMS, autoTimeoutIncrement).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously downloads a string from the specified address with retry logic.
        /// </summary>
        public async static Task<string> DownloadStringAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return await ExecuteWithRetryAsync(() => webClient.DownloadStringTaskAsync(address), nameof(webClient.DownloadStringTaskAsync), retries, retryWaitMS, autoTimeoutIncrement);
        }

        // Note: The remaining WebClient extensions are not implemented with retry logic for brevity.
        // The pattern would be the same as above. It is highly recommended to migrate to HttpClient for new development.
    }
}