using AdvancedLogging.Constants;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// WebClient extensions adding retry logic.
    /// </summary>
    [CLSCompliant(false)]
    public static class WebClientExtensions
    {
        private static T ExecuteWithRetry<T>(this WebClient webClient, Func<WebClient, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }))
            {
                try
                {
                    T result = default;
                    bool success = true;
                    string methodName = action.Method.Name;

                    for (int i = 0; i < (retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        int timeoutIncrement = 0;
                        try
                        {
                            // Detect the action's method name

                            sw = new Stopwatch();
                            sw?.Start();
                            result = action(webClient);
                            sw?.Stop();
                            if (sw != null)
                            {
                                string responseUri = "";
                                // Detect and log values from the Target property
                                if (action.Target != null)
                                {
                                    var targetType = action.Target.GetType();
                                    var field = targetType.GetFields().FirstOrDefault(f => f.Name == "address");
                                    if (field != null)
                                    {
                                        var value = field.GetValue(action.Target);
                                        responseUri = value is string strValue ? strValue : ((Uri)value).OriginalString;
                                    }
                                }
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, responseUri, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            if (!success)
                            {
                                vAutoLogFunction.WriteLog($"{methodName}: Retry is Successful!");
                            }
                            return result;
                        }
                        catch (WebException ex)
                        {
                            ExtensionsFunctions.HandleException($"{methodName}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            webClient = ExtensionsFunctions.CreateWebClient(webClient, vAutoLogFunction, ex.Status == WebExceptionStatus.Timeout ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException($"{methodName}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            if (ex.InnerException?.Message == "A task was canceled.")
                            {
                                webClient = ExtensionsFunctions.CreateWebClient(webClient, vAutoLogFunction, timeoutIncrement);
                            }
                        }
                        ExtensionsFunctions.PerformRetryDelay($"{methodName}", vAutoLogFunction, retryWaitMS);
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static T ExecuteWithRetryExt<T>(this WebClientExtended webClient, Func<WebClientExtended, T> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }))
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
                            result = action(webClient);
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
                            webClient = (WebClientExtended)ExtensionsFunctions.CreateWebClient((WebClient)webClient, vAutoLogFunction, ex.Message.Contains("timeout") ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            if (ex.InnerException?.Message == "A task was canceled.")
                            {
                                webClient = (WebClientExtended)ExtensionsFunctions.CreateWebClient((WebClient)webClient, vAutoLogFunction, timeoutIncrement);
                            }
                        }
                        ExtensionsFunctions.PerformRetryDelay($"{action.Method.Name}", vAutoLogFunction, retryWaitMS);
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void ExecuteWithRetry(this WebClient webClient, Action<WebClient> action, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }))
            {
                try
                {
                    bool success = true;
                    for (int i = 0; i < (retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        int timeoutIncrement = 0;
                        try
                        {
                            sw = new Stopwatch();
                            sw?.Start();
                            action(webClient);
                            sw?.Stop();
                            if (sw != null)
                            {
                                string responseUri = "";
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, responseUri, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            if (!success)
                            {
                                vAutoLogFunction.WriteLog($"{action.Method.Name}: Retry is Successful!");
                            }
                            return;
                        }
                        catch (HttpRequestException ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            webClient = ExtensionsFunctions.CreateWebClient(webClient, vAutoLogFunction, ex.Message.Contains("timeout") ? timeoutIncrement : 0);
                        }
                        catch (Exception ex)
                        {
                            ExtensionsFunctions.HandleException($"{action.Method.Name}", vAutoLogFunction, ex, i, retries, ref success, ref timeoutIncrement, autoTimeoutIncrement);
                            if (ex.InnerException?.Message == "A task was canceled.")
                            {
                                webClient = ExtensionsFunctions.CreateWebClient(webClient, vAutoLogFunction, timeoutIncrement);
                            }
                        }
                        ExtensionsFunctions.PerformRetryDelay($"{action.Method.Name}", vAutoLogFunction, retryWaitMS);
                    }
                    return;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClient, retries, retryWaitMS, autoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Downloads data from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download data from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>The downloaded data as a byte array.</returns>
        public static byte[] DownloadData(this WebClient webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return webClient.ExecuteWithRetry(r => r.DownloadData(address), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously downloads data from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download data from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        public async static Task<byte[]> DownloadDataAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return await webClient.ExecuteWithRetry(r => r.DownloadDataTaskAsync(address), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Downloads a file from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download the file from.</param>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        public static void DownloadFile(this WebClient webClient, string address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            webClient.ExecuteWithRetry(r => r.DownloadFile(address, fileName), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Asynchronously downloads a file from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download the file from.</param>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        public async static void DownloadFileAsync(this WebClient webClient, Uri address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            await webClient.ExecuteWithRetry(r => r.DownloadFileTaskAsync(address, fileName), retries, retryWaitMS, autoTimeoutIncrement);
        }
        //public System.Threading.Tasks.Task DownloadFileTaskAsync(string address, string fileName);

        /// <summary>
        /// Downloads a string from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download the string from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>The downloaded string.</returns>
        public static string DownloadString(this WebClientExtended webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webClient.ExecuteWithRetry(r => r.DownloadString(address), retries, retryWaitMS, autoTimeoutIncrement);
        }

        /// <summary>
        /// Downloads a string from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The IWebClient instance.</param>
        /// <param name="address">The address to download the string from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>The downloaded string.</returns>
        public static string DownloadString(this IWebClientExtended webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return ((WebClient)webClient).ExecuteWithRetry(r => r.DownloadString(address), retries, retryWaitMS, autoTimeoutIncrement);
        }

        // public void DownloadStringAsync(Uri address);
        // public System.Threading.Tasks.Task<string> DownloadStringTaskAsync(string address);

        /// <summary>
        /// Downloads a string from the specified address with retry logic.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download the string from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>The downloaded string.</returns>
        public static string DownloadString(this WebClientExtended webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            return webClient.ExecuteWithRetry(r => r.DownloadString(address), retries, retryWaitMS, autoTimeoutIncrement);
        }

        //public static void DownloadStringAsync(this WebClient webClient, Uri address, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void DownloadStringAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenRead(this WebClient webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenRead(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void OpenReadAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void OpenReadAsync(this WebClient webClient, Uri address, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenWrite(this WebClient webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenWrite(this WebClient webClient, string address, string method, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenWrite(this WebClient webClient, Uri address, string method, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static Stream OpenWrite(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void OpenWriteAsync(this WebClient webClient, Uri address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void OpenWriteAsync(this WebClient webClient, Uri address, string method, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void OpenWriteAsync(this WebClient webClient, Uri address, string method, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadData(this WebClient webClient, string address, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadData(this WebClient webClient, string address, string method, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadData(this WebClient webClient, Uri address, string method, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadData(this WebClient webClient, Uri address, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadDataAsync(this WebClient webClient, Uri address, string method, byte[] data, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadDataAsync(this WebClient webClient, Uri address, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadDataAsync(this WebClient webClient, Uri address, string method, byte[] data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadFile(this WebClient webClient, string address, string method, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadFile(this WebClient webClient, Uri address, string method, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadFile(this WebClient webClient, Uri address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadFile(this WebClient webClient, string address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadFileAsync(this WebClient webClient, Uri address, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadFileAsync(this WebClient webClient, Uri address, string method, string fileName, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadFileAsync(this WebClient webClient, Uri address, string method, string fileName, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static string UploadString(this WebClient webClient, Uri address, string method, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static string UploadString(this WebClient webClient, string address, string method, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static string UploadString(this WebClient webClient, string address, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static string UploadString(this WebClient webClient, Uri address, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadStringAsync(this WebClient webClient, Uri address, string method, string data, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadStringAsync(this WebClient webClient, Uri address, string method, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadStringAsync(this WebClient webClient, Uri address, string data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadValues(this WebClient webClient, string address, string method, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadValues(this WebClient webClient, Uri address, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadValues(this WebClient webClient, Uri address, string method, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static byte[] UploadValues(this WebClient webClient, string address, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadValuesAsync(this WebClient webClient, Uri address, string method, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadValuesAsync(this WebClient webClient, Uri address, NameValueCollection data, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
        //public static void UploadValuesAsync(this WebClient webClient, Uri address, string method, NameValueCollection data, object userToken, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);

        /// <summary>
        /// Get the WebRequest object.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The address to download data from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>WebRequest object.</returns>
        public static WebRequest GetWebRequest(this WebClientExtended webClient, string address, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return webClient.ExecuteWithRetryExt(r => r.GetWebRequest(address), retries, retryWaitMS, autoTimeoutIncrement);

        }

        /// <summary>
        /// Get the WebResponse object.
        /// </summary>
        /// <param name="webClient">The WebClient instance.</param>
        /// <param name="address">The WebRequest to download data from.</param>
        /// <param name="retries">The number of retries.</param>
        /// <param name="retryWaitMS">The wait time between retries in milliseconds.</param>
        /// <param name="autoTimeoutIncrement">The auto timeout increment value.</param>
        /// <returns>WebResponse object.</returns>
        public static WebResponse GetWebResponse(this WebClientExtended webClient, WebRequest request, int retries, int retryWaitMS, int autoTimeoutIncrement = 0)
        {
            // TODO: Fully Test! This is NOT fully Tested!
            return webClient.ExecuteWithRetryExt(r => r.GetWebResponse(request), retries, retryWaitMS, autoTimeoutIncrement);
        }

        //public static WebResponse GetWebResponse(this WebClient webClient, WebRequest request, IAsyncResult result, int retries, int retryWaitMS, int autoTimeoutIncrement = 0);
    }
}