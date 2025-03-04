using AdvancedLogging.Constants;
using AdvancedLogging.Logging;
using AdvancedLogging.Loggers;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.Remoting.Lifetime;

namespace AdvancedLogging.Extensions
{
    /// <summary>
    /// Data extensions adding retry functionality.
    /// </summary>

    public static class DataExtensions
    {
        public static void Open(this IDbConnection _IDbConnection, int _Retries, int _RetryWaitMS)
        {
            Open((SqlConnection)_IDbConnection, _Retries, _RetryWaitMS);
        }
        public static void Open(this SqlConnection _SqlConnection, int _Retries, int _RetryWaitMS)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlConnection, _Retries, _RetryWaitMS }))
            {
                try
                {
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
                            sw = new Stopwatch();
                            sw?.Start();
                            _SqlConnection.Open();
                            sw?.Stop();
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, "", LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlConnection, _Retries, _RetryWaitMS }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), "", ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            /*
                             * To increate the time out, we would need to create a new SqlConnection object.
                             */
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlConnection.Open: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut.");
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlConnection.Open: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut.");
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlConnection, _Retries, _RetryWaitMS }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), "", ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.Open: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.Open: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                    }
                    return;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlConnection, _Retries, _RetryWaitMS }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int ExecuteNonQuery(this IDbConnection _IDbConnection, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteNonQuery((SqlCommand)_IDbConnection.CreateCommand(), _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int ExecuteNonQuery(this SqlConnection _SqlConnection, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteNonQuery((SqlCommand)_SqlConnection.CreateCommand(), _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int ExecuteNonQuery(this IDbCommand _IIDbCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteNonQuery((SqlCommand)_IIDbCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int ExecuteNonQuery(this SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlCommand.ExecuteNonQuery();
#if DEBUG
                            bool bDebugTestTimoutException = false;
                            if (bDebugTestTimoutException)
                            {
                                SqlCommand scTest = new SqlCommand
                                {
                                    Connection = _SqlCommand.Connection,
                                    CommandText = "SELECT * FROM WkOrders ORDER BY WORKORDER",
                                    CommandType = CommandType.Text,
                                    CommandTimeout = 1
                                };
                                if (ApplicationSettings.LogToDebugWindow)
                                    Debug.WriteLine(scTest.ExecuteScalar());
                            }
#endif
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteNonQuery: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteNonQuery: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteNonQuery: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteNonQuery: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static SqlDataReader ExecuteReader(this IDbConnection _IDbConnection, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteReader((SqlCommand)_IDbConnection.CreateCommand(), _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static SqlDataReader ExecuteReader(this SqlConnection _SqlConnection, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteReader((SqlCommand)_SqlConnection.CreateCommand(), _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static SqlDataReader ExecuteReader(this IDbCommand _IDbCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteReader((SqlCommand)_IDbCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static SqlDataReader ExecuteReader(this SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    SqlDataReader rc = null;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlCommand.ExecuteReader();
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static SqlDataReader ExecuteReader(this IDbConnection _IDbConnection, CommandBehavior _CommandBehavior, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteReader((SqlCommand)_IDbConnection.CreateCommand(), _CommandBehavior, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static SqlDataReader ExecuteReader(this SqlCommand _SqlCommand, CommandBehavior _CommandBehavior, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand, _CommandBehavior, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    SqlDataReader rc = null;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlCommand.ExecuteReader(_CommandBehavior);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _CommandBehavior, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _CommandBehavior, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static object ExecuteScalar(this IDbConnection _IDbConnection, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return ExecuteScalar((SqlCommand)_IDbConnection.CreateCommand(), _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static object ExecuteScalar(this SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    object rc = null;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlCommand.ExecuteScalar();
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteNonQuery: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlCommand.ExecuteReader: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int Fill(this IDbDataAdapter _IDbDataAdapter, DataSet _dataSet, SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return Fill((SqlDataAdapter)_IDbDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataSet _dataSet, SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.Fill(_dataSet);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int Fill(this IDbDataAdapter _IDbDataAdapter, DataSet _dataSet, string sourcetTable, SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return Fill((SqlDataAdapter)_IDbDataAdapter, _dataSet, sourcetTable, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataSet _dataSet, string sourcetTable, SqlCommand _SqlCommand, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.Fill(_dataSet, sourcetTable);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, sourcetTable, _SqlCommand, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int Fill(this IDbDataAdapter _IDbDataAdapter, DataTable _dataTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return Fill((SqlDataAdapter)_IDbDataAdapter, _dataTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataTable _dataTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, _dataTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.Fill(_dataTable);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlDataAdapter.SelectCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlDataAdapter.SelectCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlDataAdapter.SelectCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlDataAdapter.SelectCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlDataAdapter.SelectCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        //public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataSet dataSet, int startRecord, int maxRecords, string srcTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0);

        public static int Fill(this IDbDataAdapter _IDbDataAdapter, DataSet _dataSet, string sourcetTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return Fill((SqlDataAdapter)_IDbDataAdapter, _dataSet, sourcetTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataSet _dataSet, string sourcetTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, _dataSet, sourcetTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.Fill(_dataSet, sourcetTable);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlDataAdapter.SelectCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlDataAdapter.SelectCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, sourcetTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlDataAdapter.SelectCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, sourcetTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlDataAdapter.SelectCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlDataAdapter.SelectCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, sourcetTable, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int Fill(this IDbDataAdapter _IDbDataAdapter, DataSet _dataSet, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return Fill((SqlDataAdapter)_IDbDataAdapter, _dataSet, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static int Fill(this SqlDataAdapter _SqlDataAdapter, DataSet _dataSet, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, _dataSet, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }))
            {
                try
                {
                    int rc = 0;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
#if DEBUG
                            if (ApplicationSettings.Logger != null)
                            {
                                if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                                {
                                    _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                    _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                                }
                            }
#endif
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.Fill(_dataSet);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlDataAdapter.SelectCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlDataAdapter.SelectCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlDataAdapter.SelectCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlDataAdapter.SelectCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlDataAdapter.SelectCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, _dataSet, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        //public static int Fill(this SqlDataAdapter _SqlDataAdapter, int startRecord, int maxRecords, params DataTable[] dataTables, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0);
        //public static DataTable[] FillSchema(this SqlDataAdapter _SqlDataAdapter, DataSet dataSet, SchemaType schemaType, string srcTable, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0);
        //public static DataTable[] FillSchema(this SqlDataAdapter _SqlDataAdapter, DataSet dataSet, SchemaType schemaType, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0);
        public static DataTable FillSchema(this IDbDataAdapter _IDbDataAdapter, DataTable dataTable, SchemaType schemaType, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            return FillSchema((SqlDataAdapter)_IDbDataAdapter, dataTable, schemaType, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement);
        }

        public static DataTable FillSchema(this SqlDataAdapter _SqlDataAdapter, DataTable dataTable, SchemaType schemaType, int _Retries, int _RetryWaitMS, int _iAutoTimeoutIncrement = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlDataAdapter, dataTable, schemaType }))
            {
                try
                {
                    DataTable rc = null;
                    bool bSuccess = true;
                    // Add 1 so that we process the original request + the _Retries
                    for (int i = 0; i < (_Retries + 1); i++)
                    {
                        Stopwatch sw = null;
                        try
                        {
                            sw = new Stopwatch();
                            sw?.Start();
                            rc = _SqlDataAdapter.FillSchema(dataTable, schemaType);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(_SqlDataAdapter.SelectCommand.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, _SqlDataAdapter.SelectCommand, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            return rc;
                        }
                        catch (SqlException ex) when (ex.Number == -2)  // -2 is a sql timeout
                        {
                            if (i == (_Retries - 1))
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, dataTable, schemaType, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            // handle timeout
                            if (_iAutoTimeoutIncrement > 0)
                            {
                                _SqlDataAdapter.SelectCommand.CommandTimeout += _iAutoTimeoutIncrement;
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... TimedOut, Increased to :" + _SqlDataAdapter.SelectCommand.CommandTimeout.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (i == (_Retries - 1) || ex.Message.IsSqlNonRetryError())
                            {
                                vAutoLogFunction.LogFunction(new { _SqlDataAdapter, dataTable, schemaType, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, ex);
                                LoggingUtils.LogDBError(System.Reflection.MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), _SqlDataAdapter.SelectCommand, ex, vAutoLogFunction.LogPrefix);
                                throw;
                            }
                            if (ex.Message.Contains("current state is closed"))
                            {
                                _SqlDataAdapter.SelectCommand.Connection.Open();
                            }
                            if (bSuccess)
                            {
                                bSuccess = false;
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ...", ex);
                            }
                            else
                                vAutoLogFunction.WriteLog("SqlDataAdapter.Fill: Retrying (" + (i + 1).ToString() + "/" + _Retries.ToString() + " ... (Error: " + ex.Message + ")");
                        }
#if DEBUG
                        if (ApplicationSettings.Logger != null)
                        {
                            if (ApplicationSettings.Logger.IsDebugEnabled && ApplicationSettings.Logger.LogLevel >= LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL])
                            {
                                _SqlDataAdapter.SelectCommand.PropertiesToString(vAutoLogFunction.LogPrefix);
                                _SqlDataAdapter.SelectCommand.ParametersToString(vAutoLogFunction.LogPrefix);
                            }
                        }
#endif
                        if (_RetryWaitMS > 250)
                        {
                            DateTime dt = DateTime.Now.AddMilliseconds(_RetryWaitMS);
                            while (dt > DateTime.Now && ApplicationSettings.IsRunning)
                            {
                                System.Threading.Thread.Sleep(250);
                            }
                        }
                        else
                            System.Threading.Thread.Sleep(_RetryWaitMS);
                        _SqlDataAdapter.SelectCommand.Parameters?.Reset();
                    }
                    return rc;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlDataAdapter, dataTable, schemaType, _Retries, _RetryWaitMS, _iAutoTimeoutIncrement }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static object PropertiesToString(this IDbConnection _IDbConnection, string _LogPrefix = "")
        {
            return PropertiesToString((SqlCommand)_IDbConnection.CreateCommand(), _LogPrefix);
        }

        public static string PropertiesToString(this SqlCommand _SqlCommand, string _LogPrefix = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand }))
            {
                try
                {
                    vAutoLogFunction.LogPrefix = _LogPrefix;
                    string strOutput = "";
                    var propInfo = _SqlCommand.GetType().GetProperties();
                    foreach (var item in propInfo)
                    {
                        if (item.CanRead)
                        {
                            try
                            {
                                LoggingUtils.WriteDebug(item.Name + " = " + item.GetValue(_SqlCommand, null));
                                if (strOutput.Length > 0)
                                    strOutput += Environment.NewLine;
                                strOutput += item.Name + " = " + item.GetValue(_SqlCommand, null);
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.WriteError("Error Printing Property [" + item.Name + "] = " + item.GetValue(_SqlCommand, null) + " ...", ex);
                            }
                        }
                    }
                    return strOutput;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static string ParametersToString(this SqlCommand _SqlCommand, string _LogPrefix = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlCommand }))
            {
                try
                {
                    vAutoLogFunction.LogPrefix = _LogPrefix;
                    string strOutput = "";
                    if (_SqlCommand.Parameters != null)
                    {
                        foreach (SqlParameter sp in _SqlCommand.Parameters)
                        {
                            if (strOutput.Length > 0)
                                strOutput += Environment.NewLine;
                            strOutput += "Parameter Name: " + sp.ParameterName;
                            LoggingUtils.WriteDebug("Parameter Name: " + sp.ParameterName);
                            var propInfo = sp.GetType().GetProperties();
                            foreach (var item in propInfo)
                            {
                                if (item.CanRead)
                                {
                                    if (item.Name == "ParameterName")
                                        continue;
                                    try
                                    {
                                        LoggingUtils.WriteDebug("\t" + item.Name + " = " + item.GetValue(sp, null));
                                        if (strOutput.Length > 0)
                                            strOutput += Environment.NewLine;
                                        strOutput += "\t" + item.Name + " = " + item.GetValue(sp, null);
                                    }
                                    catch (Exception ex)
                                    {
                                        LoggingUtils.WriteError("\tError Printing Property [" + item.Name + "] = " + item.GetValue(sp, null) + " ...", ex);
                                        if (strOutput.Length > 0)
                                            strOutput += Environment.NewLine;
                                        strOutput += "\tError Printing Property [" + item.Name + "] = " + item.GetValue(sp, null) + " ... Error: " + ex.ToString();
                                    }
                                }
                            }
                        }
                    }
                    return strOutput;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlCommand, _LogPrefix }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static void Reset(this SqlParameterCollection _SqlParameterCollection)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _SqlParameterCollection }))
            {
                try
                {
                    ILease lease = (ILease)_SqlParameterCollection.InitializeLifetimeService();
                    if (lease.CurrentState == LeaseState.Initial)
                    {
                        lease.InitialLeaseTime = TimeSpan.FromMinutes(5);
                        lease.SponsorshipTimeout = TimeSpan.FromMinutes(2);
                        lease.RenewOnCallTime = TimeSpan.FromMinutes(2);
                        lease.Renew(new TimeSpan(0, 5, 0));
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _SqlParameterCollection }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string SqlDbTypeToString(this SqlParameter _SqlParameter)
        {
            try
            {
                switch (_SqlParameter.SqlDbType)
                {
                    case SqlDbType.Char:
                    case SqlDbType.NChar:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                        return _SqlParameter.SqlDbType.ToString() + "(" + _SqlParameter.Size.ToString("#,##0") + ")";
                    default:
                        return _SqlParameter.SqlDbType.ToString();
                }
            }
            catch (Exception exOuter)
            {
                LoggingUtils.WriteError("Error in SqlDbTypeToString:", exOuter);
                throw;
            }
        }
    }
}