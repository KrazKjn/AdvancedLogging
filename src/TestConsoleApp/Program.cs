using AdvancedLogging.Extensions;
using AdvancedLogging.Loggers;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using AdvancedLogging.SecureCredentials;
using AdvancedLogging.Utilities;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using AdvancedLogging.AutoCoder;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedLogging.TestConsoleApp
{
    class Program
    {
        static void Main() // string[] args
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ICommonLogger>(new Log4NetLogger("TestConsoleApp"))
                .AddSingleton<ILoggingContext, Log4NetLoggingContext>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ICommonLogger>();

            SecurityProtocol.EnableAllTlsSupport();
            LogConfigData(logger);
            RunAllTests(serviceProvider);

            serviceProvider = new ServiceCollection()
                .AddSingleton<ICommonLogger>(new SeriLogger())
                .AddSingleton<ILoggingContext, SeriLogLoggingContext>()
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ICommonLogger>();

            LogConfigData(logger);
            RunAllTests(serviceProvider);
        }
        private static void RunAllTests(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ICommonLogger>();
            var loggingContext = serviceProvider.GetService<ILoggingContext>();

            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { }))
            {
                try
                {
                    string url = "https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-9.0&pivots=without-bff-pattern";
                    vAutoLogFunction.WriteLog("Testing: VerifyUrl ...");
                    if (Utils.VerifyUrl(url, out Uri _DetectedUri))
                    {
                        Debug.WriteLine(_DetectedUri.ToString());
                    }
                    try
                    {
                        dynamic dt = "Test".ToType("Test".GetType());

                        dt.Log(logger, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                    HttpWebRequest httpWebRequest = WebRequest.Create(_DetectedUri ?? new Uri(url)) as HttpWebRequest;
                    httpWebRequest.AllowAutoRedirect = true;
                    httpWebRequest.Timeout = 20 * 1000;
                    httpWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

                    WebRequest webRequest = WebRequest.Create(_DetectedUri ?? new Uri(url));
                    webRequest.Timeout = 20 * 1000;
                    webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

                    HttpClient httpClient = new HttpClient();

                    WebClientExtended webClient = new WebClientExtended()
                    {
                        Credentials = System.Net.CredentialCache.DefaultCredentials,
                        Timeout = 10
                    };

                    string userName = "";
                    string password = "";
                    string name = null;

                    CredentialsDialog dialog = new CredentialsDialog("SQL Credentials");
                    if (name != null) dialog.AlwaysDisplay = true;
                    if (dialog.Show(name) == DialogResult.OK)
                    {
                        userName = dialog.Name;
                        password = dialog.Password;
                    }
                    SqlCommand sqlCommand = new SqlCommand("SELECT GETUTCDATE() AS SERVERTIME, @TestInt AS TESTINT, @TestBit AS TESTBIT, @TestFloat AS TESTFLOAT, @TestChar50 AS TESTCHAR50", new SqlConnection(@"Data Source=localhost;Initial Catalog=master;Persist Security Info=True;User ID=" + userName + ";Password=" + password + ";Network Library=DBMSSOCN;Application Name=Test App Name;"));

                    SqlParameter[] arrParms = {
                            new SqlParameter("TestInt", System.Data.SqlDbType.BigInt),
                            new SqlParameter("TestBit", System.Data.SqlDbType.Bit),
                            new SqlParameter("TestFloat", System.Data.SqlDbType.Float),
                            new SqlParameter("TestChar50", System.Data.SqlDbType.Char, 50)
                        };
                    arrParms[0].Value = 56755;
                    arrParms[1].Value = 1;
                    arrParms[2].Value = 567.876;
                    arrParms[3].Value = "Test String";
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestHttpWebRequest ...");
                        TestHttpWebRequest(logger, loggingContext, httpWebRequest);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebRequest ...");
                        TestWebRequest(logger, loggingContext, webRequest);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestHttpClient ...");
                        TestHttpClient(logger, loggingContext, httpClient, _DetectedUri ?? new Uri(url));
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebClient ...");
                        TestWebClient(logger, loggingContext, webClient, _DetectedUri ?? new Uri(url));
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebClient ...");
                        TestWebClient(logger, loggingContext, webClient, _DetectedUri.OriginalString ?? url);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand using SqlHelperStatic ...");
                        TestSqlCommand(logger, loggingContext, sqlCommand, arrParms, true);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand without using SqlHelperStatic ...");
                        TestSqlCommand(logger, loggingContext, sqlCommand, arrParms);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand SqlHelperStatic and creating a SQL Exception ...");
                        TestSqlCommand(logger, loggingContext, sqlCommand, arrParms, true, true);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestDataTypes ...");
                        TestDataTypes(logger, loggingContext, 10.80m, "Test String", 456.9874, 5987);
                    }
                    catch { }

                    TestClass tc = new TestClass(logger, loggingContext);
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: using a class called TestClass() ...");
                        vAutoLogFunction.WriteLog("Test Class Value: " + tc.Test());
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: using a class called TestClass() generating an Exception ...");
                        vAutoLogFunction.WriteLog("Test Class Value: " + tc.Test(true));
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSuppresHeader ...");
                        TestSuppresHeader(logger, loggingContext, "TestSuppresHeader");
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestAutoLog ...");
                        TestAutoLog(logger, loggingContext);
                    }
                    catch { }

                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: Function Parameters ...");
                        TestParameters(logger, loggingContext, null, 1, "Test");
                    }
                    catch { }

                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: Retry Logic ...");
                        TestRetryLogic(logger, loggingContext);
                    }
                    catch { }

                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: AutoCoder ...");
                        TestAutoCoder(logger, loggingContext);
                    }
                    catch { }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestRetryLogic(ICommonLogger logger, ILoggingContext loggingContext)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { }))
            {
                try
                {
                    var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(2);
                    var response = httpClient.GetAsync(logger, loggingContext, "http://localhost:12345/api/nonexistent", 3, 1000).Result;
                }
                catch (Exception ex)
                {
                    vAutoLogFunction.WriteError("Caught expected exception after retries", ex);
                }
            }
        }

        private static void TestAutoCoder(ICommonLogger logger, ILoggingContext loggingContext)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { }))
            {
                try
                {
                    string testFile = "TestAutoCoder.cs";
                    string fileContent = @"
using System;
public class TestAutoCoder
{
    public void MyMethod()
    {
        Console.WriteLine(""Hello"");
    }
}";
                    File.WriteAllText(testFile, fileContent);

                    var codeCSharp = new AutoCoder.CodeCSharp(
                        new System.Collections.Specialized.StringCollection(),
                        new List<string>(),
                        new Dictionary<string, bool>(),
                        new Dictionary<string, bool>(),
                        ".",
                        false
                    );

                    codeCSharp.ProcessFile(new FileInfo(testFile), AutoCoder.CodeItems.AutoLog | AutoCoder.CodeItems.TryCatch | AutoCoder.CodeItems.Method);

                    string newContent = File.ReadAllText(testFile);
                    if (newContent.Contains("vAutoLogFunction"))
                    {
                        vAutoLogFunction.WriteLog("AutoCoder test passed.");
                    }
                    else
                    {
                        vAutoLogFunction.WriteError("AutoCoder test failed.");
                    }
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    vAutoLogFunction.WriteError("Exception in TestAutoCoder", ex);
                }
            }
        }
        private static void TestParameters(ICommonLogger logger, ILoggingContext loggingContext, object sender, int i, string str)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { sender, i, str }))
            {
                try
                {
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("sender = " + (sender == null ? "(null)" : sender.ToString()));
                    vAutoLogFunction.WriteLog("i = " + i.ToString());
                    vAutoLogFunction.WriteLog("str = " + str);
                    vAutoLogFunction.WriteLog(new string('-', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sender, i, str }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void TestWebRequest(ICommonLogger logger, ILoggingContext loggingContext, WebRequest _webRequest)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { _webRequest }))
            {
                try
                {
                    _webRequest.Timeout = 10;
                    WebResponse webResponse = _webRequest.GetResponse();
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", DataObjectDumper.Dump(webResponse, logger: logger));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    webResponse.Close();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _webRequest }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestHttpWebRequest(ICommonLogger logger, ILoggingContext loggingContext, HttpWebRequest _httpWebRequest)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { _httpWebRequest }))
            {
                try
                {
                    _httpWebRequest.Timeout = 10;
                    WebResponse webResponse = _httpWebRequest.GetResponse();
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", DataObjectDumper.Dump(webResponse, logger: logger));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    webResponse.Close();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _httpWebRequest }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private async static void TestHttpClient(ICommonLogger logger, ILoggingContext loggingContext, HttpClient httpClient, Uri uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { httpClient }))
            {
                try
                {
                    httpClient.Timeout = new TimeSpan(10 * 1000);
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(logger, loggingContext, uri.OriginalString, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);
                    httpResponseMessage.EnsureSuccessStatusCode();
                    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", DataObjectDumper.Dump(responseBody, logger: logger));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    httpResponseMessage.Dispose();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { httpClient }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestWebClient(ICommonLogger logger, ILoggingContext loggingContext, WebClientExtended webClientExtended, Uri uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { webClientExtended }))
            {
                try
                {
                    webClientExtended.Timeout = 10;
                    string responseBody = webClientExtended.DownloadString(logger, loggingContext, uri.OriginalString, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", DataObjectDumper.Dump(responseBody, logger: logger));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClientExtended }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestWebClient(ICommonLogger logger, ILoggingContext loggingContext, WebClientExtended webClientExtended, string uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { webClientExtended }))
            {
                try
                {
                    webClientExtended.Timeout = 10;
                    string responseBody = webClientExtended.DownloadString(logger, loggingContext, uri, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", DataObjectDumper.Dump(responseBody, logger: logger));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClientExtended }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestSqlCommand(ICommonLogger logger, ILoggingContext loggingContext, SqlCommand _sqlCommand, SqlParameter[] arrParms, bool bUseSqlHelperStatic = false, bool bThrowException = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { _sqlCommand, arrParms, bUseSqlHelperStatic }, null, false, bThrowException))
            {
                try
                {
                    if (bThrowException)
                        _sqlCommand.CommandText = "$" + _sqlCommand.CommandText;
                    if (bUseSqlHelperStatic)
                    {
                        if (_sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                            _sqlCommand.Connection.Open();
                        SqlDataReader reader = SqlHelperStatic.ExecuteReader(_sqlCommand.Connection, System.Data.CommandType.Text, _sqlCommand.CommandText, arrParms);
                        if (reader != null)
                        {
                            if (!reader.IsClosed)
                            {
                                if (reader.HasRows)
                                {
                                    vAutoLogFunction.WriteLog("Data Results ...");
                                    while (reader.Read())
                                    {
                                        vAutoLogFunction.WriteLogFormat("SERVERTIME:{0}, TESTINT:{1}, TESTBIT:{2}, TESTFLOAT:{3}, TESTCHAR50:{4}",
                                            reader.GetDateTime(0),
                                            reader.GetInt64(1),
                                            reader.GetBoolean(2),
                                            reader.GetDouble(3),
                                            reader.GetString(4));
                                    }
                                    vAutoLogFunction.WriteLog("Data Results ... End!");
                                }
                                reader.Close();
                            }
                        }
                        if (_sqlCommand.Connection.State == System.Data.ConnectionState.Open)
                            _sqlCommand.Connection.Close();
                    }
                    else
                    {
                        _sqlCommand.Parameters.AddRange(arrParms);
                        if (_sqlCommand.Connection.State != System.Data.ConnectionState.Open)
                            _sqlCommand.Connection.Open();
                        SqlDataReader reader = _sqlCommand.ExecuteReader();
                        _sqlCommand.Parameters.Clear();
                        if (reader != null)
                        {
                            if (!reader.IsClosed)
                            {
                                if (reader.HasRows)
                                {
                                    vAutoLogFunction.WriteLog("Data Results ...");
                                    while (reader.Read())
                                    {
                                        vAutoLogFunction.WriteLogFormat("SERVERTIME:{0}, TESTINT:{1}, TESTBIT:{2}, TESTFLOAT:{3}, TESTCHAR50:{4}",
                                            reader.GetDateTime(0),
                                            reader.GetInt64(1),
                                            reader.GetBoolean(2),
                                            reader.GetDouble(3),
                                            reader.GetString(4));
                                    }
                                    vAutoLogFunction.WriteLog("Data Results ... End!");
                                }
                                reader.Close();
                            }
                        }
                        if (_sqlCommand.Connection.State == System.Data.ConnectionState.Open)
                            _sqlCommand.Connection.Close();
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _sqlCommand, arrParms, bUseSqlHelperStatic }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void TestDataTypes(ICommonLogger logger, ILoggingContext loggingContext, decimal decValue, string sValue, double dValue, Int64 iValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { decValue, sValue, dValue, iValue }))
            {
                try
                {

                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { decValue, sValue, dValue, iValue }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void TestAutoLog(ICommonLogger logger, ILoggingContext loggingContext)
        {
            List<string> lstString = new List<string>() { "Value1", "Value2" };
            Dictionary<string, string> dicString = new Dictionary<string, string>() { { "Key", "Value" } };
            System.Collections.Concurrent.ConcurrentDictionary<string, string> dicString2 = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            string[] arrString = { "test", "test2" };
            int[] arrInt = { 0, 3 };

            dicString2.AddOrUpdate("Key1", "Value1", (key, oldValue) => "Value1");
            dicString2.AddOrUpdate("Key1", "Value2", (key, oldValue) => "Value2");
            TestAutoLogParms(logger, loggingContext, lstString, dicString, dicString2, arrString, arrInt);
        }
        private static void TestAutoLogParms(ICommonLogger logger, ILoggingContext loggingContext, List<string> lstString, Dictionary<string, string> dicString, System.Collections.Concurrent.ConcurrentDictionary<string, string> dicString2, string[] arrString, int[] arrInt)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { lstString, dicString, dicString2, arrString, arrInt }))
            {
                try
                {
                    vAutoLogFunction.WriteDebug(3, "Test Debug Message");
                    vAutoLogFunction.WriteLog("Test Info Message");
                    arrInt[0]++;
                    if (arrInt[0] < arrInt[1])
                        TestAutoLogParms(logger, loggingContext, lstString, dicString, dicString2, arrString, arrInt);
                    else
                    {
                        int y = 1;
                        int i = y / 0;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { lstString, dicString, dicString2, arrString, arrInt }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void TestSuppresHeader(ICommonLogger logger, ILoggingContext loggingContext, string message)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { message }, bSuppressFunctionDeclaration: false))
            {
                try
                {
                    vAutoLogFunction.WriteDebug(3, "Test Debug Message");
                    vAutoLogFunction.WriteLog("Test Info Message");
                    vAutoLogFunction.WriteLog(message);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { message }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void LogConfigData(ICommonLogger logger)
        {
            Assembly testApp = Assembly.GetEntryAssembly();
            System.Configuration.Configuration config = null;
            if (testApp != null)
            {
                config = ConfigurationManager.OpenExeConfiguration(testApp.Location);
            }
            string appConfig = config?.FilePath;
            FileInfo appConfigFileInfo = new FileInfo(appConfig);

            if (appConfigFileInfo.Exists)
            {
                try
                {
                    XmlConfigurator.ConfigureAndWatch(appConfigFileInfo);
                    logger.ConfigFile = appConfigFileInfo.FullName;
                    logger.Monitoring = true;

                    string appConfigtext = BusinessLogic.Configuration.RedactConfigFileContents(logger.ConfigFileXml, logger);

                    logger.Info("Base Directory: " + AppDomain.CurrentDomain.BaseDirectory);
                    logger.Info("Looking for configuration file at path: " + appConfigFileInfo.FullName);
                    logger.InfoFormat("Configuration File Contents:\r\n{0}", appConfigtext);
                    logger.InfoFormat("Using Settings from {0} file: START", appConfigFileInfo.Name);
                    if (logger.IsDebugEnabled)
                    {
                        logger.DebugFormat("ApplicationSettings.Logger: {0}", logger == null ? "Is Null" : "Is Set");
                        logger.DebugFormat("Debug Level: [*] (i.e., LogLevel) -> [{0}]", logger.LogLevel);
                        logger.DebugFormat("Monitoring: {0}", logger.Monitoring);
                        logger.DebugFormat("log4netLvl: {0}", logger.Level.DisplayName);
                        logger.Debug(new string('-', 80));
                        foreach (var vitem in logger.DebugLevels)
                        {
                            logger.DebugFormat("Debug Level: [{0}] -> [{1}]", vitem.Key, vitem.Value);
                        }
                        logger.Debug(new string('-', 80));
                        foreach (var vitem in LoggingUtils.DebugPrintLevel.OrderBy(x => x.Value))
                        {
                            logger.DebugFormat("Debug Printing Level: [{0}] -> [{1}]", vitem.Key, vitem.Value);
                        }
                        logger.Debug(new string('-', 80));

                        logger?.Debug("Written with ApplicationSettings.Logger.");
                    }
                    logger?.Info("Available Security Protocols ...");
                    SecurityProtocol.LogSecurityProtocol((CommonLogger)logger);
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Error Processing Config File {0}\n{1}", appConfigFileInfo.FullName, ex);
                }
            }
        }
    }
}
