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

namespace AdvancedLogging.TestConsoleApp
{
    class Program
    {
        static void Main() // string[] args
        {
            ApplicationSettings.Logger = new Log4NetLogger("TestConsoleApp");
            SecurityProtocol.EnableAllTlsSupport();
            LogConfigData();
            RunAllTests();

            ApplicationSettings.Logger = new SeriLogger();
            LogConfigData();
            RunAllTests();
        }
        private static void RunAllTests()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    //string url = "https://live.com";
                    string url = "https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-9.0&pivots=without-bff-pattern";
                    vAutoLogFunction.WriteLog("Testing: VerifyUrl ...");
                    if (Utils.VerifyUrl(url, out Uri _DetectedUri))
                    {
                        Debug.WriteLine(_DetectedUri.ToString());
                    }
                    try
                    {
                        dynamic dt = "Test".ToType("Test".GetType());

                        dt.Log(0, vAutoLogFunction.Logger);
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

                    WebClientExt webClient = new WebClientExt()
                    {
                        Credentials = System.Net.CredentialCache.DefaultCredentials,
                        Timeout = 10
                    };

                    //DataConnectionDialog dataConnectionDialog = new DataConnectionDialog();

                    string userName = "";
                    string password = "";
                    string name = null;

                    CredentialsDialog dialog = new CredentialsDialog("SQL Credentials");
                    if (name != null) dialog.AlwaysDisplay = true; // prevent an infinite loop
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
                        TestHttpWebRequest(httpWebRequest);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebRequest ...");
                        TestWebRequest(webRequest);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestHttpClient ...");
                        TestHttpClient(httpClient, _DetectedUri ?? new Uri(url));
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebClient ...");
                        TestWebClient(webClient, _DetectedUri ?? new Uri(url));
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestWebClient ...");
                        TestWebClient(webClient, _DetectedUri.OriginalString ?? url);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand using SqlHelperStatic ...");
                        TestSqlCommand(sqlCommand, arrParms, true);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand without using SqlHelperStatic ...");
                        TestSqlCommand(sqlCommand, arrParms);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestSqlCommand SqlHelperStatic and creating a SQL Exception ...");
                        TestSqlCommand(sqlCommand, arrParms, true, true);
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestDataTypes ...");
                        TestDataTypes(10.80m, "Test String", 456.9874, 5987);
                    }
                    catch { }

                    TestClass tc = new TestClass();
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
                        TestSuppresHeader("TestSuppresHeader");
                    }
                    catch { }
                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: TestAutoLog ...");
                        TestAutoLog();
                    }
                    catch { }

                    try
                    {
                        vAutoLogFunction.WriteLog("Testing: Function Parameters ...");
                        TestParameters(null, 1, "Test");
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
        private static void TestParameters(object sender, int i, string str)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sender, i, str }))
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
        private static void TestWebRequest(WebRequest _webRequest)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _webRequest }))
            {
                try
                {
                    _webRequest.Timeout = 10;
                    WebResponse webResponse = _webRequest.GetResponse(ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", Utilities.ObjectDumper.Dump(webResponse));
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

        private static void TestHttpWebRequest(HttpWebRequest _httpWebRequest)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _httpWebRequest }))
            {
                try
                {
                    _httpWebRequest.Timeout = 10;
                    WebResponse webResponse = _httpWebRequest.GetResponse(ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", Utilities.ObjectDumper.Dump(webResponse));
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

        private async static void TestHttpClient(HttpClient httpClient, Uri uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { httpClient }))
            {
                try
                {
                    httpClient.Timeout = new TimeSpan(10 * 1000);
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(uri.OriginalString, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);
                    httpResponseMessage.EnsureSuccessStatusCode();
                    // Read and display the response content
                    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", Utilities.ObjectDumper.Dump(responseBody));
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

        private static void TestWebClient(WebClientExt webClientExt, Uri uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webClientExt }))
            {
                try
                {
                    webClientExt.Timeout = 10;
                    string responseBody = webClientExt.DownloadString(uri, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", Utilities.ObjectDumper.Dump(responseBody));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClientExt }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestWebClient(WebClientExt webClientExt, string uri)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { webClientExt }))
            {
                try
                {
                    webClientExt.Timeout = 10;
                    string responseBody = webClientExt.DownloadString(uri, ApplicationSettings.MaxAutoRetriesHttp, ApplicationSettings.AutoRetrySleepMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp);

                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLog("This is NOT Debug Code.  This is a TEST at INFO Level.");
                    vAutoLogFunction.WriteLog(new string('-', 80));
                    vAutoLogFunction.WriteLogFormat("Web Data: {0}", Utilities.ObjectDumper.Dump(responseBody));
                    vAutoLogFunction.WriteLog(new string('-', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { webClientExt }, MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void TestSqlCommand(SqlCommand _sqlCommand, SqlParameter[] arrParms, bool bUseSqlHelperStatic = false, bool bThrowException = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _sqlCommand, arrParms, bUseSqlHelperStatic }, null, null, false, bThrowException))
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
        private static void TestDataTypes(decimal decValue, string sValue, double dValue, Int64 iValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { decValue, sValue, dValue, iValue }))
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
        private static void TestAutoLog()
        {
            List<string> lstString = new List<string>() { "Value1", "Value2" };
            Dictionary<string, string> dicString = new Dictionary<string, string>() { { "Key", "Value" } };
            System.Collections.Concurrent.ConcurrentDictionary<string, string> dicString2 = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            string[] arrString = { "test", "test2" };
            int[] arrInt = { 0, 3 };

            dicString2.AddOrUpdate("Key1", "Value1", (key, oldValue) => "Value1");
            dicString2.AddOrUpdate("Key1", "Value2", (key, oldValue) => "Value2");
            TestAutoLogParms(lstString, dicString, dicString2, arrString, arrInt);
        }
        private static void TestAutoLogParms(List<string> lstString, Dictionary<string, string> dicString, System.Collections.Concurrent.ConcurrentDictionary<string, string> dicString2, string[] arrString, int[] arrInt)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { lstString, dicString, dicString2, arrString, arrInt }))
            {
                try
                {
                    vAutoLogFunction.WriteDebug(3, "Test Debug Message");
                    vAutoLogFunction.WriteLog("Test Info Message");
                    arrInt[0]++;
                    if (arrInt[0] < arrInt[1])
                        TestAutoLogParms(lstString, dicString, dicString2, arrString, arrInt);
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
        private static void TestSuppresHeader(string message)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { message }, bSuppressFunctionDeclaration: false))
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
        private static void LogConfigData()
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
                    ApplicationSettings.Logger.ConfigFile = appConfigFileInfo.FullName;
                    ApplicationSettings.Logger.Monitoring = true;

                    string appConfigtext = BusinessLogic.Configuration.RedactConfigFileContents(ApplicationSettings.Logger.ConfigFileXml, ApplicationSettings.Logger);

                    ApplicationSettings.Logger.Info("Base Directory: " + AppDomain.CurrentDomain.BaseDirectory);
                    ApplicationSettings.Logger.Info("Looking for configuration file at path: " + appConfigFileInfo.FullName);
                    ApplicationSettings.Logger.InfoFormat("Configuration File Contents:\r\n{0}", appConfigtext);
                    ApplicationSettings.Logger.InfoFormat("Using Settings from {0} file: START", appConfigFileInfo.Name);
                    if (ApplicationSettings.Logger.IsDebugEnabled)
                    {
                        ApplicationSettings.Logger.DebugFormat("ApplicationSettings.Logger: {0}", ApplicationSettings.Logger == null ? "Is Null" : "Is Set");
                        ApplicationSettings.Logger.DebugFormat("Debug Level: [*] (i.e., LogLevel) -> [{0}]", ApplicationSettings.Logger.LogLevel);
                        ApplicationSettings.Logger.DebugFormat("Monitoring: {0}", ApplicationSettings.Logger.Monitoring);
                        ApplicationSettings.Logger.DebugFormat("log4netLvl: {0}", ApplicationSettings.Logger.Level.DisplayName);
                        ApplicationSettings.Logger.Debug(new string('-', 80));
                        foreach (var vitem in ApplicationSettings.Logger.DebugLevels)
                        {
                            ApplicationSettings.Logger.DebugFormat("Debug Level: [{0}] -> [{1}]", vitem.Key, vitem.Value);
                        }
                        ApplicationSettings.Logger.Debug(new string('-', 80));
                        foreach (var vitem in LoggingUtils.DebugPrintLevel.OrderBy(x => x.Value))
                        {
                            ApplicationSettings.Logger.DebugFormat("Debug Printing Level: [{0}] -> [{1}]", vitem.Key, vitem.Value);
                        }
                        ApplicationSettings.Logger.Debug(new string('-', 80));

                        ApplicationSettings.Logger?.Debug("Written with ApplicationSettings.Logger.");
                    }
                    ApplicationSettings.Logger?.Info("Available Security Protocols ...");
                    SecurityProtocol.LogSecurityProtocol((CommonLogger)ApplicationSettings.Logger);
                }
                catch (Exception ex)
                {
                    ApplicationSettings.Logger.ErrorFormat("Error Processing Config File {0}\n{1}", appConfigFileInfo.FullName, ex);
                }
            }
        }
    }
    //bool TryGetDataConnectionStringFromUser(out string outConnectionString)
    //{
    //    using (var dialog = new DataConnectionDialog())
    //    {
    //        // If you want the user to select from any of the available data sources, do this:
    //        DataSource.AddStandardDataSources(dialog);

    //        // OR, if you want only certain data sources to be available
    //        // (e.g. only SQL Server), do something like this instead: 
    //        dialog.DataSources.Add(DataSource.SqlDataSource);
    //        dialog.DataSources.Add(DataSource.SqlFileDataSource);
    //    …

    //    // The way how you show the dialog is somewhat unorthodox; `dialog.ShowDialog()`
    //    // would throw a `NotSupportedException`. Do it this way instead:
    //    DialogResult userChoice = DataConnectionDialog.Show(dialog);

    //        // Return the resulting connection string if a connection was selected:
    //        if (userChoice == DialogResult.OK)
    //        {
    //            outConnectionString = dialog.ConnectionString;
    //            return true;
    //        }
    //        else
    //        {
    //            outConnectionString = null;
    //            return false;
    //        }
    //    }
    //}
}
