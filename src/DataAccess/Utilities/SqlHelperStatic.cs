using AdvancedLogging.Constants;
using AdvancedLogging.Extensions;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Loggers;
using AdvancedLogging.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static AdvancedLogging.Utilities.LoggingUtils;

namespace AdvancedLogging.Utilities
{
    public abstract class SqlHelperStatic
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IDbConnection OpenNewConnection(string connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString }))
            {
                try
                {
                    SqlConnection connection;

                    connection = new SqlConnection(connectionString);
                    connection.Open();

                    return connection;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static ConcurrentDictionary<string, object> m_dicData = new ConcurrentDictionary<string, object>();

        public static ConcurrentDictionary<string, object> Data
        {
            get { return m_dicData; }
            set { m_dicData = value; }
        }

        #region ExecuteNonQuery
        /// <summary>
        /// Execute a SqlCommand (that returns no resultset) against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            int val = 0;
                            Stopwatch sw = null;
                            try
                            {
                                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                                cmd.Connection.InfoMessage += Connection_InfoMessage;
                                bConnection_InfoMessage = true;
                                cmd.StatementCompleted += Cmd_StatementCompleted;
                                bCmd_StatementCompleted = true;
                                sw = new Stopwatch();
                                sw?.Start();
                                val = cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                                sw?.Stop();
                                LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                                if (sw != null)
                                {
                                    LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                                }
                                cmd.Parameters.Clear();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            finally
                            {
                                if (bConnection_InfoMessage)
                                    cmd.Connection.InfoMessage -= Connection_InfoMessage;
                                if (bCmd_StatementCompleted)
                                    cmd.StatementCompleted -= Cmd_StatementCompleted;
                                CloseConnection(conn);
                            }
                            return val;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int ExecuteNonQuery(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    // this is potentially in a transaction so we cannot close the connection right away
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        int val = 0;
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, transaction.Connection, transaction, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            sw = new Stopwatch();
                            sw?.Start();
                            val = cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                        return val;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int ExecuteNonQuery(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, query, parameters }))
            {
                try
                {
                    var command = BuildQuery(connection, cmdType, query, parameters);
                    return command.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, query, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region ExecuteReader
        /// <summary>
        /// Execute a SqlCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  SqlDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public static SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    // NOTE: calling code is responsible for closing the SqlDataReader 
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        SqlDataReader rdr = null;
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, new SqlConnection(connectionString), null, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                            cmd.Dispose();
                        }
                        return rdr;
                    }
                    catch
                    {
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static SqlDataReader ExecuteReader(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    // NOTE: calling code is responsible for closing the SqlDataReader 
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        SqlDataReader rdr = null;
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, transaction.Connection, transaction, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                            cmd.Dispose();
                        }
                        return rdr;
                    }
                    catch
                    {
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static SqlDataReader ExecuteReader(IDbConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    // NOTE: calling code is responsible for closing the SqlDataReader 
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        SqlDataReader rdr = null;
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, (SqlConnection)connection, null, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                            cmd.Dispose();
                        }
                        return rdr;
                    }
                    catch
                    {
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static ISqlDataReaderHelper ExecuteReader(IDbConnection connection, string procName, params IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, procName, parameters }))
            {
                try
                {
                    IDbCommand command = BuildCommand(connection, procName, parameters);
                    ISqlDataReaderHelper sqlDataReaderHelper = new SqlDataReaderHelper(command.ExecuteReader(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql));
                    command.Dispose();
                    return sqlDataReaderHelper;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, procName, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static ISqlDataReaderHelper ExecuteReader(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, query, parameters }))
            {
                try
                {
                    var command = BuildQuery(connection, cmdType, query, parameters);
                    ISqlDataReaderHelper sqlDataReaderHelper = new SqlDataReaderHelper(command.ExecuteReader(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql));
                    command.Dispose();
                    return sqlDataReaderHelper;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, query, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region ExecuteReaderWithTimeOut
        /// <summary>
        /// Execute a SqlCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  SqlDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeOut">the timeout value for SQL function. -1 = default, 0 = infinite</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public static SqlDataReader ExecuteReaderWithTimeOut(string connectionString, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdType, cmdText, cmdTimeOut, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    // NOTE: calling code is responsible for closing the SqlDataReader 
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        SqlDataReader rdr = null;
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, new SqlConnection(connectionString), null, cmdType, cmdText, commandParameters, cmdTimeOut);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                            cmd.Dispose();
                        }
                        return rdr;
                    }
                    catch
                    {
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdType, cmdText, cmdTimeOut, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.The connection will timeout if the query doesn't complete in 30 seconds.
        /// </summary>
        /// <param name="connection">A SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public static SqlDataReader ExecuteReaderWithTimeOut(SqlConnection connection, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, cmdText, cmdTimeOut, commandParameters }))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                        if (cmdTimeOut != -1)
                        {
                            cmd.CommandTimeout = cmdTimeOut;
                        }
                        SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                        cmd.Parameters.Clear();
                        return rdr;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, cmdText, cmdTimeOut, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlDataReader ExecuteReaderWithTimeOut(SqlTransaction transaction, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdType, cmdText, cmdTimeOut, commandParameters }))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        PrepareCommand(cmd, transaction.Connection, transaction, cmdType, cmdText, commandParameters);
                        if (cmdTimeOut != -1)
                        {
                            cmd.CommandTimeout = cmdTimeOut;
                        }
                        SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                        cmd.Parameters.Clear();
                        return rdr;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdType, cmdText, cmdTimeOut, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion

        #region ExecuteScalarForProcedure
        public static object ExecuteScalarForProcedure(string connectionString, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    object val = string.Empty;
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            Stopwatch sw = null;
                            try
                            {
                                PrepareCommand(cmd, conn, null, CommandType.StoredProcedure, cmdText, commandParameters);
                                cmd.Connection.InfoMessage += Connection_InfoMessage;
                                bConnection_InfoMessage = true;
                                cmd.StatementCompleted += Cmd_StatementCompleted;
                                bCmd_StatementCompleted = true;
                                if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                                sw = new Stopwatch();
                                sw?.Start();
                                cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                                sw?.Stop();
                                LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                                if (sw != null)
                                {
                                    LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                                }
                                cmd.Parameters.Clear();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            finally
                            {
                                if (bConnection_InfoMessage)
                                    cmd.Connection.InfoMessage -= Connection_InfoMessage;
                                if (bCmd_StatementCompleted)
                                    cmd.StatementCompleted -= Cmd_StatementCompleted;
                                CloseConnection(conn);
                            }
                        }
                    }
                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static object ExecuteScalarForProcedure(SqlTransaction transaction, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    object val = string.Empty;
                    //using (SqlConnection conn = new SqlConnection(transaction))
                    //{
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, transaction.Connection, transaction, CommandType.StoredProcedure, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                    }
                    //}
                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static object ExecuteScalarForProcedure(IDbConnection connection, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    object val = string.Empty;
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, (SqlConnection)connection, null, CommandType.StoredProcedure, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                            cmd.Dispose();
                        }
                    }

                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region ExecuteAdapter
        //Written by vasu
        public static SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdText }))
            {
                try
                {
                    if (!IsValidConnectionString(connectionString))
                    {
                        (cmdText, connectionString) = (connectionString, cmdText);
                    }
                    SqlDataAdapter adr = new SqlDataAdapter(cmdText, connectionString);
                    adr.SelectCommand.CommandTimeout = 0;
                    return adr;
                }
                catch (SqlException ex)
                {
                    LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmdText, ex);
                    throw;
                }
                catch (Exception ex)
                {
                    LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmdText, ex);
                    throw;
                }
            }
        }

        //Written by vasu
        public static SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    try
                    {
                        // don't explicit open the connection. If that's done, ADO.NET expects you to close it explicitly too
                        // if only the connectionstring is set, when .Fill is executed, it will open and close the connection automatically
                        PrepareCommand(cmd, new SqlConnection(connectionString), null, CommandType.Text, cmdText, commandParameters);
                        cmd.Connection.InfoMessage += Connection_InfoMessage;
                        bConnection_InfoMessage = true;
                        cmd.StatementCompleted += Cmd_StatementCompleted;
                        bCmd_StatementCompleted = true;
                        SqlDataAdapter adr = new SqlDataAdapter(cmd);
                        adr.SelectCommand.CommandTimeout = 0;
                        return adr;
                    }
                    catch (SqlException ex)
                    {
                        LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                        // need to dispose connection if there is an error
                        CloseCommandConnection(cmd);
                        throw;
                    }
                    finally
                    {
                        if (bConnection_InfoMessage)
                            cmd.Connection.InfoMessage -= Connection_InfoMessage;
                        if (bCmd_StatementCompleted)
                            cmd.StatementCompleted -= Cmd_StatementCompleted;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region ExecuteScalar
        //Written by vasu

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        object val = string.Empty;
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            try
                            {
                                Stopwatch sw = null;
                                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                                cmd.Connection.InfoMessage += Connection_InfoMessage;
                                bConnection_InfoMessage = true;
                                cmd.StatementCompleted += Cmd_StatementCompleted;
                                bCmd_StatementCompleted = true;
                                sw = new Stopwatch();
                                sw?.Start();
                                val = cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                                sw?.Stop();
                                LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                                if (sw != null)
                                {
                                    LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                                }
                                cmd.Parameters.Clear();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            finally
                            {
                                if (bConnection_InfoMessage)
                                    cmd.Connection.InfoMessage -= Connection_InfoMessage;
                                if (bCmd_StatementCompleted)
                                    cmd.StatementCompleted -= Cmd_StatementCompleted;
                                CloseConnection(conn);
                            }
                            return val;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connection, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">a valid SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    object val = string.Empty;
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {
                            Stopwatch sw = null;
                            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            sw = new Stopwatch();
                            sw?.Start();
                            val = cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                        return val;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(transaction, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">a valid SqlTransaction</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    object val = string.Empty;
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        try
                        {
                            Stopwatch sw = null;
                            PrepareCommand(cmd, transaction.Connection, transaction, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            sw = new Stopwatch();
                            sw?.Start();
                            val = cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                        return val;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region ExecuteScalarWithTimeOut
        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeOut">the timeout value for SQL function. -1 = default, 0 = infinite</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalarWithTimeOut(string connectionString, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString, cmdType, cmdText, cmdTimeOut, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        object val = string.Empty;
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            try
                            {
                                Stopwatch sw = null;
                                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters, cmdTimeOut);
                                cmd.Connection.InfoMessage += Connection_InfoMessage;
                                bConnection_InfoMessage = true;
                                cmd.StatementCompleted += Cmd_StatementCompleted;
                                bCmd_StatementCompleted = true;
                                sw = new Stopwatch();
                                sw?.Start();
                                val = cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                                sw?.Stop();
                                LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                                if (sw != null)
                                {
                                    LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                                }
                                cmd.Parameters.Clear();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            finally
                            {
                                if (bConnection_InfoMessage)
                                    cmd.Connection.InfoMessage -= Connection_InfoMessage;
                                if (bCmd_StatementCompleted)
                                    cmd.StatementCompleted -= Cmd_StatementCompleted;
                                CloseConnection(conn);
                            }
                            return val;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connectionString, cmdType, cmdText, cmdTimeOut, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion
        #region InsertAndReturnIdentity
        /// <summary>
        /// This function is used for inserting records into a table and also 
        /// getting the identity of the inserted record
        /// </summary>
        /// <param name="oled">SqlCommand object</param>
        /// <param name="olec">SqlConnection object</param>
        /// <param name="trans">SqlTransaction object</param>
        /// <param name="cmdType">Cmd type e.g. stored procedure or text</param>
        /// <param name="cmdText">Command text, e.g. Select * from Products</param>
        /// <param name="cmdParms">SqlParameters to use in the command</param>
        public static long InsertAndReturnIdentity(string connString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connString, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    long val = 0;
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            Stopwatch sw = null;
                            try
                            {
                                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                                cmd.Connection.InfoMessage += Connection_InfoMessage;
                                bConnection_InfoMessage = true;
                                cmd.StatementCompleted += Cmd_StatementCompleted;
                                bCmd_StatementCompleted = true;
                                sw = new Stopwatch();
                                sw?.Start();
                                val = (long)cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                                sw?.Stop();
                                LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                                if (sw != null)
                                {
                                    LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                                }
                                cmd.Parameters.Clear();

                                try
                                {
                                    cmd.CommandText = "SELECT @@IDENTITY";
                                    cmd.CommandType = CommandType.Text;
                                    val = 0;
                                    if (cmd.ExecuteScalar() != null && !string.Empty.Equals(cmd.ExecuteScalar().ToString()))
                                        val = long.Parse(cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql).ToString());
                                    if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                                }
                                catch (SqlException ex)
                                {
                                    LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                    throw;
                                }
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            finally
                            {
                                if (bConnection_InfoMessage)
                                    cmd.Connection.InfoMessage -= Connection_InfoMessage;
                                if (bCmd_StatementCompleted)
                                    cmd.StatementCompleted -= Cmd_StatementCompleted;
                                CloseConnection(conn);
                            }
                        }
                    }
                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connString, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static long InsertAndReturnIdentity(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { transaction, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    long val = 0;

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, transaction.Connection, transaction, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            sw = new Stopwatch();
                            sw?.Start();
                            val = (long)cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();

                            try
                            {
                                cmd.CommandText = "SELECT @@IDENTITY";
                                cmd.CommandType = CommandType.Text;
                                val = 0;
                                if (cmd.ExecuteScalar() != null && !string.Empty.Equals(cmd.ExecuteScalar().ToString()))
                                    val = long.Parse(cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql).ToString());
                                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                    }

                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { transaction, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static long InsertAndReturnIdentity(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, cmdText, commandParameters }))
            {
                bool bConnection_InfoMessage = false;
                bool bCmd_StatementCompleted = false;
                try
                {
                    long val = 0;
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        Stopwatch sw = null;
                        try
                        {
                            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                            cmd.Connection.InfoMessage += Connection_InfoMessage;
                            bConnection_InfoMessage = true;
                            cmd.StatementCompleted += Cmd_StatementCompleted;
                            bCmd_StatementCompleted = true;
                            sw = new Stopwatch();
                            sw?.Start();
                            val = (long)cmd.ExecuteNonQuery(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, vAutoLogFunction, cmd, LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod]);
                            }
                            cmd.Parameters.Clear();

                            try
                            {
                                cmd.CommandText = "SELECT @@IDENTITY";
                                cmd.CommandType = CommandType.Text;
                                val = 0;
                                if (cmd.ExecuteScalar() != null && !string.Empty.Equals(cmd.ExecuteScalar().ToString()))
                                    val = long.Parse(cmd.ExecuteScalar(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql).ToString());
                                if (cmd.Connection.State != ConnectionState.Closed) cmd.Connection.Close();
                            }
                            catch (SqlException ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                            catch (Exception ex)
                            {
                                LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                                throw;
                            }
                        }
                        catch (SqlException ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LoggingUtils.LogDBError(MethodBase.GetCurrentMethod(), CommonLogger.GetParentFunction(), cmd, ex);
                            throw;
                        }
                        finally
                        {
                            if (bConnection_InfoMessage)
                                cmd.Connection.InfoMessage -= Connection_InfoMessage;
                            if (bCmd_StatementCompleted)
                                cmd.StatementCompleted -= Cmd_StatementCompleted;
                        }
                    }

                    return val;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, cmdText, commandParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion

        #region SqlEvents
        private static void Cmd_StatementCompleted(object sender, StatementCompletedEventArgs e)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sender, e }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    string strMessage = "";
                    if (sender is SqlCommand sqlCommand)
                    {
                        strMessage = sqlCommand.CommandText + ": ";
                    }
                    vAutoLogFunction.WriteDebugFormat(DebugPrintLevel[ConfigurationSetting.Log_SqlCommandResults], "{0}: {1}Rows Returned: {2}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), strMessage, e.RecordCount);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sender, e }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sender, e }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    vAutoLogFunction.WriteDebugFormat(DebugPrintLevel[ConfigurationSetting.Log_SqlCommandResults], "{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), e.Message);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sender, e }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion

        /// <summary>
        /// Prepare a command for execution
        /// </summary>
        /// <param name="cmd">SqlCommand object</param>
        /// <param name="conn">SqlConnection object</param>
        /// <param name="trans">SqlTransaction object</param>
        /// <param name="cmdType">Cmd type e.g. stored procedure or text</param>
        /// <param name="cmdText">Command text, e.g. Select * from Products</param>
        /// <param name="cmdParms">SqlParameters to use in the command</param>
        // private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms) {
#if __IOS__
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction? trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms, int iTimeOut = 0)
#else
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms, int iTimeOut = 0)
#endif
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { cmd, conn, trans, cmdType, cmdText, cmdParms, iTimeOut }))
            {
                try
                {
                    if (cmd == null)
                    {
                        throw new ArgumentException("Missing SqlCommand parameter named cmd");
                    }

                    if (conn != null)
                    {
                        if (conn.State != ConnectionState.Open)
                            conn.Open(ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql);
                        cmd.Connection = conn;
                    }

                    if (trans != null)
                        cmd.Transaction = trans;

                    cmd.CommandType = cmdType;
                    cmd.CommandText = cmdText;

                    if (cmdParms != null)
                    {
                        foreach (SqlParameter parm in cmdParms)
                            cmd.Parameters.Add(parm);
                    }
                    if (iTimeOut != -1)
                    {
                        cmd.CommandTimeout = iTimeOut;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { cmd, conn, trans, cmdType, cmdText, cmdParms, iTimeOut }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static IDbCommand BuildQuery(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, cmdType, query, parameters }))
            {
                try
                {
                    var command = new SqlCommand(query, (SqlConnection)connection) { CommandType = cmdType };
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            if (param.Value == null)
                            {
                                param.Value = DBNull.Value;
                            }

                            command.Parameters.Add(param);
                        }
                    }

                    return command;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, cmdType, query, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static IDbCommand BuildCommand(IDbConnection connection, string procName, params IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connection, procName, parameters }))
            {
                try
                {
                    var command = new SqlCommand(procName, (SqlConnection)connection) { CommandType = CommandType.StoredProcedure };
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            if (param.Value == null)
                            {
                                param.Value = DBNull.Value;
                            }

                            command.Parameters.Add(param);
                        }
                    }

                    return command;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { connection, procName, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static IDataParameter[] BuildParameterBlock(List<string> parameterName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { parameterName }))
            {
                try
                {
                    IDataParameter[] result = new SqlParameter[parameterName.Count];

                    for (int i = 0; i < parameterName.Count; i++)
                    {
                        result[i] = new SqlParameter
                        {
                            ParameterName = string.Empty
                        };
                        if (!parameterName[i].StartsWith("@"))
                        {
                            result[i].ParameterName += "@";
                        }
                        result[i].ParameterName += parameterName[i].Replace('[', '_').Replace(']', '_');
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { parameterName }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static IDataParameter[] CloneParameterBlock(IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { parameters }))
            {
                try
                {
                    IDataParameter[] result = null;
                    if (parameters != null)
                    {
                        result = new SqlParameter[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            result[i] = new SqlParameter
                            {
                                ParameterName = parameters[i].ParameterName,
                                Value = parameters[i].Value
                            };
                        }
                    }
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        #region QueryFormatter
        /// <summary>
        /// This function is used for replacing the characters which are keywords
        /// in sqlserver and replace with escape characters and return
        /// </summary>
        /// <param name="SqlQuery">string, This is Search Text</param>
        public static string QueryFormatter(string SqlQuery)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { SqlQuery }))
            {
                try
                {
                    if (SqlQuery != null)
                        return (SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''"));
                    else
                        return SqlQuery;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { SqlQuery }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// This function is used to replace and wild card search in search text that will be passed
        /// into SQL via a SQL Parameter and used in a LIKE clause.
        /// </summary>
        /// <param name="SearchText"></param>
        /// <returns></returns>
        public static string FormatSqlForLikeWithWildCards(string SearchText)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { SearchText }))
            {
                try
                {
                    string result = "";

                    if (SearchText != null)
                    {
                        result += SearchText.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("*", "%");
                    }

                    result = "%" + result + "%";

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { SearchText }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// This function is used for replacing the characters and wild card search  which are keywords
        /// in sqlserver and replace with escape characters and return
        /// </summary>
        /// <param name="SqlQuery">string, This is Search Text</param>

        public static string QueryWildCardFormatter(string SqlQuery)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { SqlQuery }))
            {
                try
                {

                    string tempSqlQuery = string.Empty;
                    if (SqlQuery != null)
                    {
                        bool firstQuote = SqlQuery.StartsWith("\"");
                        bool endQuote = SqlQuery.EndsWith("\"");

                        if (firstQuote && endQuote)
                        {
                            tempSqlQuery = SqlQuery;
                            SqlQuery = SqlQuery.Remove(0, 1);
                            if (SqlQuery.Length > 0)
                            {
                                SqlQuery = SqlQuery.Remove(SqlQuery.Length - 1, 1);
                                SqlQuery = (SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''"));
                            }
                            //else condition added by vinay for double quotes issue in search
                            else
                            {
                                SqlQuery = tempSqlQuery;
                                //commented by vinay for case 7899 
                                //Issue is with 5.2.2. Works as expected in 5.2.0.
                                //SqlQuery =  SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''") ;
                                SqlQuery = "%" + SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''") + "%";

                            }
                        }
                        else if (SqlQuery == "NULL")
                        {
                            SqlQuery = "";

                        }
                        else if (SqlQuery.IndexOf("*") == -1 && SqlQuery.Length > 0)
                        {
                            SqlQuery = SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''");
                        }

                        else if (SqlQuery.IndexOf("*") != -1 && SqlQuery.Length > 0)
                        {

                            SqlQuery = SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''").Replace("*", "%");
                        }

                        else
                        {
                            SqlQuery = "%";
                        }
                        //commented by vinay for case 7899 
                        //Issue is with 5.2.2. Works as expected in 5.2.0.
                        ////added by Srikanth.N on 09/29/2010 
                        if (SqlQuery.IndexOf("%") == -1 && SqlQuery != "")
                        {
                            SqlQuery = "%" + SqlQuery + "%";
                        }
                    }
                    return SqlQuery;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { SqlQuery }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string QueryFormatterLIKE(string SqlQuery)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { SqlQuery }))
            {
                try
                {
                    if (SqlQuery != null)
                        return (SqlQuery.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''"));
                    else
                        return SqlQuery;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { SqlQuery }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string ConvertInjectionCharacters(string SqlQuery)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { SqlQuery }))
            {
                try
                {
                    if (SqlQuery != null)
                        return (SqlQuery.Replace("'", "`"));
                    else
                        return SqlQuery;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { SqlQuery }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        #endregion

        private static void CloseConnection(SqlConnection conn)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { conn }))
            {
                try
                {
                    if (conn != null)
                    {
                        if (conn.State == ConnectionState.Open)
                        {
                            conn.Close();
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { conn }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void CloseCommandConnection(SqlCommand cmd)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { cmd }))
            {
                try
                {
                    if (cmd.Connection != null)
                    {
                        if (cmd.Connection.State == ConnectionState.Open)
                        {
                            cmd.Connection.Close();
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { cmd }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool IsPhoneNumber(string number)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { number }))
            {
                try
                {
                    if (string.IsNullOrEmpty(number.Trim()))
                        return true;
                    return Regex.Match(number, @"^(\+[0-9]{9})$").Success;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { number }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool IsEmailAddress(string address)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { address }))
            {
                try
                {
                    if (string.IsNullOrEmpty(address.Trim()))
                        return true;
                    var addr = new System.Net.Mail.MailAddress(address);
                    return (addr.Address == address);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { address }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool IsValidConnectionString(string connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { connectionString }))
            {
                try
                {
                    // Previous declaration of ExecuteAdapter had the second parameter as connectionString.
                    // MJH on March 16th 2020 2:58 PM Changed the parameter declaration to have connectionString first.
                    // Added code to validate connectionString is a valid connectionString and swap cmdText and connectionString if connection fails.
                    SqlConnection sqlConnection = new SqlConnection(connectionString);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }

        public static Int16 GetInt16Value(object dataField, Int16 _default = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToInt16(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static Int32 GetInt32Value(object dataField, Int32 _default = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToInt32(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static Int64 GetInt64Value(object dataField, Int64 _default = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToInt64(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static double GetDoubleValue(object dataField, double _default = 0.0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToDouble(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static decimal GetDecimalValue(object dataField, decimal _default = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToDecimal(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static byte GetByteValue(object dataField, byte _default = 0)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToByte(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static DateTime GetDateTimeValue(object dataField, DateTime _default)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return Convert.ToDateTime(dataField);
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string GetStringValue(object dataField, string _default = "")
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { dataField, _default }))
            {
                try
                {
                    if (dataField != System.DBNull.Value)
                    {
                        try
                        {
                            return dataField.ToString();
                        }
                        catch (Exception ex)
                        {
                            vAutoLogFunction.WriteErrorFormat("{0}: {1}", CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), ex.ToString());
                        }
                    }
                    return _default;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { dataField, _default }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string HtmlToPlainText(string html)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { html }))
            {
                try
                {
                    //const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
                    const string tagWhiteSpace = @"(>|$)(\W|)+<";//matches one or more (white space or line breaks) between '>' and '<'
                    const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
                    const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
                    var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
                    var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
                    var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

                    var text = html;
                    //Decode html specific characters
                    text = System.Net.WebUtility.HtmlDecode(text);
                    //Remove tag whitespace/line breaks
                    //text = tagWhiteSpaceRegex.Replace(text, "><");
                    //Replace <br /> with line breaks
                    text = lineBreakRegex.Replace(text, Environment.NewLine);
                    //Strip formatting
                    text = stripFormattingRegex.Replace(text, string.Empty);

                    return text;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { html }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string ReplaceLessThanCharacter(string html)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { html }))
            {
                try
                {
                    string text = html;
                    text = text.Replace("<<", "&lt;<");
                    text = text.Replace("<0", "&lt;0");
                    text = text.Replace("<1", "&lt;1");
                    text = text.Replace("<2", "&lt;2");
                    text = text.Replace("<3", "&lt;3");
                    text = text.Replace("<4", "&lt;4");
                    text = text.Replace("<5", "&lt;5");
                    text = text.Replace("<6", "&lt;6");
                    text = text.Replace("<7", "&lt;7");
                    text = text.Replace("<8", "&lt;8");
                    text = text.Replace("<9", "&lt;9");
                    text = text.Replace("<.", "&lt;.");
                    text = text.Replace("< ", "&lt; ");
                    string strlist = @"<a>,<a charset=,<a coords=,<a download=,<a href=,<a hreflang=,<a media=,<a name=,<a ping=,<a referrerpolicy=,<a rel=,<a rev=,<a shape=,<a target=,<a type=
                                       ,<a accesskey=,<a class=,<a contenteditable=,<a data-,<a dir=,<a draggable=,<a dropzone=,<a hidden=,<a id=,<a lang=,<a spellcheck=,<a style=,<a tabindex=
                                       ,<a title=,<a translate=,<a charset=,<a coords=,<a download=,<a href=,<a hreflang=,<a media=,<a name=,<a ping=,<a referrerpolicy=,<a rel=,<a rev=
                                       ,<a shape=,<a target=,<a type=,<a accesskey=,<a class=,<a contenteditable=,<a data-,<a dir=,<a draggable=,<a dropzone=,<a hidden=,<a id=,<a lang=
                                       ,<a spellcheck=,<a style=,<a tabindex=,<a title=,<a translate=,<a charset=,<a coords=,<a download=,<a href=,<a hreflang=,<a media=,<a name=
                                       ,<a ping=,<a referrerpolicy=,<a rel=,<a rev=,<a shape=,<a target=,<a type=,<a accesskey=,<a class=,<a contenteditable=,<a data-,<a dir=,<a draggable=,<a dropzone=
                                       ,<a hidden=,<a id=,<a lang=,<a spellcheck=,<a style=,<a tabindex=,<a title=,<a translate=,<a charset=,<a coords=,<a download=,<a href=,<a hreflang=,<a media=
                                       ,<a name=,<a ping=,<a referrerpolicy=,<a rel=,<a rev=,<a shape=,<a target=,<a type=,<a accesskey=,<a class=,<a contenteditable=,<a data-,<a dir=,<a draggable=
                                       ,<a dropzone=,<a hidden=,<a id=,<a lang=,<a spellcheck=,<a style=,<a tabindex=,<a title=,<a translate=,<abbr,<acronym,<address,<applet,<area,<article,<aside,<audio,<b>
                                       ,<b accesskey=,<b class=,<b contenteditable=,<b data-,<b dir=,<b draggable=,<b dropzone=,<b hidden=,<b id=,<b lang=,<b spellcheck=,<b style=,<b tabindex=,<b title=
                                       ,<b translate=,<b accesskey=,<b class=,<b contenteditable=,<b data-,<b dir=,<b draggable=,<b dropzone=,<b hidden=,<b id=,<b lang=,<b spellcheck=,<b style=,<b tabindex=
                                       ,<b title=,<b translate=,<b accesskey=,<b class=,<b contenteditable=,<b data-,<b dir=,<b draggable=,<b dropzone=,<b hidden=,<b id=,<b lang=,<b spellcheck=,<b style=
                                       ,<b tabindex=,<b title=,<b translate=,<b accesskey=,<b class=,<b contenteditable=,<b data-,<b dir=,<b draggable=,<b dropzone=,<b hidden=,<b id=,<b lang=,<b spellcheck=
                                       ,<b style=,<b tabindex=,<b title=,<b translate=,<base,<basefont,<bdi,<bdo,<big,<blockquote,<body,<br,<button,<canvas,<caption,<center,<cite,<code,<col,<colgroup,<datalist,<dd
                                       ,<del,<details,<dfn,<dialog,<dir=,<div,<dl,<dt,<em,<embed,<fieldset,<figcaption,<figure,<font,<footer,<form,<frame,<frameset,<h1,<h2,<h3,<h4,<h5,<h6,<head,<header,<hr,<html,<i>
                                       ,<i accesskey=,<i class=,<i contenteditable=,<i data-,<i dir=,<i draggable=,<i dropzone=,<i hidden=,<i id=,<i lang=,<i spellcheck=,<i style=,<i tabindex=,<i title=
                                       ,<i translate=,<i accesskey=,<i class=,<i contenteditable=,<i data-,<i dir=,<i draggable=,<i dropzone=,<i hidden=,<i id=,<i lang=,<i spellcheck=,<i style=,<i tabindex=
                                       ,<i title=,<i translate=,<i accesskey=,<i class=,<i contenteditable=,<i data-,<i dir=,<i draggable=,<i dropzone=,<i hidden=,<i id=,<i lang=,<i spellcheck=,<i style=
                                       ,<i tabindex=,<i title=,<i translate=,<i accesskey=,<i class=,<i contenteditable=,<i data-,<i dir=,<i draggable=,<i dropzone=,<i hidden=,<i id=,<i lang=,<i spellcheck=
                                       ,<i style=,<i tabindex=,<i title=,<i translate=,<iframe,<img,<input,<ins,<kbd,<label,<legend,<li,<link,<main,<map,<mark,<meta,<meter,<nav,<noframes,<noscript,<object,<ol
                                       ,<optgroup,<option,<output,<p>,<p align=,<p accesskey=,<p class=,<p contenteditable=,<p data-,<p dir=,<p draggable=,<p dropzone=,<p hidden=,<p id=,<p lang=,<p spellcheck=
                                       ,<p style=,<p tabindex=,<p title=,<p translate=,<p align=,<p accesskey=,<p class=,<p contenteditable=,<p data-,<p dir=,<p draggable=,<p dropzone=,<p hidden=,<p id=
                                       ,<p lang=,<p spellcheck=,<p style=,<p tabindex=,<p title=,<p translate=,<p align=,<p accesskey=,<p class=,<p contenteditable=,<p data-,<p dir=,<p draggable=,<p dropzone=
                                       ,<p hidden=,<p id=,<p lang=,<p spellcheck=,<p style=,<p tabindex=,<p title=,<p translate=,<p align=,<p accesskey=,<p class=,<p contenteditable=,<p data-,<p dir=
                                       ,<p draggable=,<p dropzone=,<p hidden=,<p id=,<p lang=,<p spellcheck=,<p style=,<p tabindex=,<p title=,<p translate=,<param,<pre,<progress,<q>,<q cite=,<q accesskey=
                                       ,<q class=,<q contenteditable=,<q data-,<q dir=,<q draggable=,<q dropzone=,<q hidden=,<q id=,<q lang=,<q spellcheck=,<q style=,<q tabindex=,<q title=,<q translate=
                                       ,<q cite=,<q accesskey=,<q class=,<q contenteditable=,<q data-,<q dir=,<q draggable=,<q dropzone=,<q hidden=,<q id=,<q lang=,<q spellcheck=,<q style=,<q tabindex=
                                       ,<q title=,<q translate=,<q cite=,<q accesskey=,<q class=,<q contenteditable=,<q data-,<q dir=,<q draggable=,<q dropzone=,<q hidden=,<q id=,<q lang=,<q spellcheck=
                                       ,<q style=,<q tabindex=,<q title=,<q translate=,<q cite=,<q accesskey=,<q class=,<q contenteditable=,<q data-,<q dir=,<q draggable=,<q dropzone=,<q hidden=,<q id=
                                       ,<q lang=,<q spellcheck=,<q style=,<q tabindex=,<q title=,<q translate=,<rp,<rt,<ruby,<s>,<s accesskey=,<s class=,<s contenteditable=,<s data-,<s dir=,<s draggable=
                                       ,<s dropzone=,<s hidden=,<s id=,<s lang=,<s spellcheck=,<s style=,<s tabindex=,<s title=,<s translate=,<s accesskey=,<s class=,<s contenteditable=,<s data-,<s dir=
                                       ,<s draggable=,<s dropzone=,<s hidden=,<s id=,<s lang=,<s spellcheck=,<s style=,<s tabindex=,<s title=,<s translate=,<s accesskey=,<s class=,<s contenteditable=
                                       ,<s data-,<s dir=,<s draggable=,<s dropzone=,<s hidden=,<s id=,<s lang=,<s spellcheck=,<s style=,<s tabindex=,<s title=,<s translate=,<s accesskey=,<s class=
                                       ,<s contenteditable=,<s data-,<s dir=,<s draggable=,<s dropzone=,<s hidden=,<s id=,<s lang=,<s spellcheck=,<s style=,<s tabindex=,<s title=,<s translate=,<samp,<script
                                       ,<section,<select,<small,<source,<span,<strike,<strong,<style=,<sub,<summary,<sup,<table,<tbody,<td,<textarea,<tfoot,<th,<thead,<time,<title=,<tr,<track,<tt,<u>,<u accesskey=
                                       ,<u class=,<u contenteditable=,<u data-,<u dir=,<u draggable=,<u dropzone=,<u hidden=,<u id=,<u lang=,<u spellcheck=,<u style=,<u tabindex=,<u title=,<u translate=
                                       ,<u accesskey=,<u class=,<u contenteditable=,<u data-,<u dir=,<u draggable=,<u dropzone=,<u hidden=,<u id=,<u lang=,<u spellcheck=,<u style=,<u tabindex=,<u title=
                                       ,<u translate=,<u accesskey=,<u class=,<u contenteditable=,<u data-,<u dir=,<u draggable=,<u dropzone=,<u hidden=,<u id=,<u lang=,<u spellcheck=,<u style=,<u tabindex=
                                       ,<u title=,<u translate=,<u accesskey=,<u class=,<u contenteditable=,<u data-,<u dir=,<u draggable=,<u dropzone=,<u hidden=,<u id=,<u lang=,<u spellcheck=,<u style=
                                       ,<u tabindex=,<u title=,<u translate=,<ul,<var,<video,<wbr,<!--,</a,</b,</abbr,</acronym,</address,</applet,</area,</article,</aside,</audio,</b,</base,</basefont,</bdi,</bdo
                                       ,</big,</blockquote,</body,</br,</button,</canvas,</caption,</center,</cite,</code,</col,</colgroup,</datalist,</dd,</del,</details,</dfn,</dialog,</dir,</div,</dl,</dt,</em
                                       ,</embed,</fieldset,</figcaption,</figure,</font,</footer,</form,</frame,</frameset,</h1,</h2,</h3,</h4,</h5,</h6,</head,</header,</hr,</html,</i,</iframe,</img,</input,</ins
                                       ,</kbd,</label,</legend,</li,</link,</main,</map,</mark,</p,</param,</pre,</progress,</q,</rp,</rt,</ruby,</s,</samp,</script,</section,</select,</small,</source,</span,</strike
                                       ,</strong,</style,</sub,</summary,</sup,</table,</tbody,</td,</textarea,</tfoot,</th,</thead,</time,</title,</tr,</track,</tt,</u,</ul,</var,</video,</wbr,-->";
                    string[] listArray = strlist.Split(',');
                    int startIndex = 0;
                    int endIndex = 0;
                    int lenght = 0;
                    var list = new List<Tuple<string, string>>();
                    int count = 0;
                    string endTag = ">";

                    foreach (string startTag in listArray)
                    {
                        if (text.Contains(startTag))
                        {
                            startIndex = text.IndexOf(startTag);
                            endIndex = text.IndexOf(endTag, startIndex);
                            lenght = (endIndex - startIndex) + 1;
                            while (startIndex >= 0 && endIndex > 0 && lenght > 0)
                            {
                                string t = text.Substring(startIndex, endIndex - startIndex + endTag.Length);
                                list.Add(Tuple.Create(string.Concat("@HTMLTag", Convert.ToString(count), "."), t));
                                text = text.Replace(t, string.Concat("@HTMLTag", Convert.ToString(count), "."));

                                startIndex = text.IndexOf(startTag);
                                if (startIndex != -1)
                                {
                                    break;
                                }
                            }
                            count++;
                        }
                    }
                    text = text.Replace("<", "&lt;");
                    foreach (var item in list)
                    {
                        text = text.Replace(item.Item1.ToString(), item.Item2.ToString());
                    }
                    return text;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { html }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string GetStringMultipleParameters(string name, string lstString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, lstString }))
            {
                try
                {
                    string queryParams = string.Empty;
                    lstString = lstString.Replace(" ", "");
                    List<string> lstParams = lstString.Split(',').ToList();
                    for (int j = 0; j < lstParams.Count; j++)
                    {
                        queryParams += name + lstParams[j] + ",";
                    }
                    queryParams = queryParams.Substring(0, queryParams.Length - 1);
                    return queryParams;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, lstString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlParameter[] SetStringMultipleParameters(SqlParameter[] parameters, string lstString, string codes)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { parameters, lstString, codes }))
            {
                try
                {
                    lstString = lstString.Replace(" ", "");
                    List<string> lstParameterNames = lstString.Split(',').ToList();
                    List<string> lstParameterValue = codes.Split(',').ToList();
                    parameters = new SqlParameter[lstParameterNames.Count];
                    for (int i = 0; i < lstParameterNames.Count; i++)
                    {
                        parameters[i] = new SqlParameter
                        {
                            ParameterName = string.Empty
                        };
                        if (!lstParameterNames[i].StartsWith("@"))
                        {
                            parameters[i].ParameterName += "@" + lstParameterNames[i];
                        }
                        parameters[i].ParameterName += lstParameterNames[i];
                        parameters[i].Value = lstParameterValue[i];
                    }
                    return parameters;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { parameters, lstString, codes }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool GenerateParameterizedIN(string[] inputLists, out string strInClause, ref List<SqlParameter> lstSqlParameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { inputLists, lstSqlParameters }))
            {
                System.IO.FileInfo fiTemp = null;
                try
                {
                    strInClause = "";
                    int iIndex = 1;
                    fiTemp = new System.IO.FileInfo(System.IO.Path.GetTempFileName());

                    foreach (string list in inputLists)
                    {
                        if (strInClause.Length > 0)
                        {
                            strInClause += ", ";
                        }

                        string strFieldName = "@" + list + "_" + fiTemp.Name.Replace(fiTemp.Extension, "") + iIndex.ToString();
                        strInClause += strFieldName;
                        lstSqlParameters.Add(new SqlParameter(strFieldName, list));
                        iIndex++;
                    }
                    return (lstSqlParameters.Count > 0);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { inputLists, lstSqlParameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
                finally
                {
                    if (fiTemp != null)
                    {
                        if (fiTemp.Exists)
                            fiTemp.Delete();
                    }
                }
            }
        }

        public static string[] ConvertDelimitedStringToArray(string inputString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { inputString }))
            {
                try
                {
                    inputString = inputString.Replace(';', ',');
                    inputString = inputString.TrimEnd(',');
                    string[] inputList = inputString.Split(',');
                    for (int i = 0; i < inputList.Length; i++)
                    {
                        inputList[i] = inputList[i].Trim();
                    }
                    return inputList;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { inputString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlParameter CreateSqlParameter(string ParameterName, object value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { ParameterName, value }))
            {
                try
                {
                    SqlParameter newParameter = new SqlParameter(ParameterName, value);
                    return newParameter;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { ParameterName, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlParameter CreateSqlParameter(string ParameterName, object value, SqlDbType dbType)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { ParameterName, value, dbType }))
            {
                try
                {
                    SqlParameter newParameter = new SqlParameter(ParameterName, dbType)
                    {
                        Value = value
                    };
                    return newParameter;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { ParameterName, value, dbType }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlParameter CreateSqlParameter(string ParameterName, object value, SqlDbType dbType, int size)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { ParameterName, value, dbType, size }))
            {
                try
                {
                    SqlParameter newParameter = new SqlParameter(ParameterName, dbType, size)
                    {
                        Value = value
                    };
                    return newParameter;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { ParameterName, value, dbType, size }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}