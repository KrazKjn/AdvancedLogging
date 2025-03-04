using AdvancedLogging.Constants;
using AdvancedLogging.Extensions;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Loggers;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using static AdvancedLogging.Utilities.LoggingUtils;

namespace AdvancedLogging.Utilities
{
    public class SqlHelper : ISqlHelper
    {
        private readonly Dictionary<string, SqlConnection> cachedConnections = new Dictionary<string, SqlConnection>();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public IDbConnection OpenNewConnection(string connectionString)
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

        ~SqlHelper()
        {
            foreach (string strKey in cachedConnections.Keys)
            {
                if (cachedConnections[strKey].State == ConnectionState.Open)
                {
                    cachedConnections[strKey].Close();
                }
            }
        }

        private ConcurrentDictionary<string, object> m_dicData = new ConcurrentDictionary<string, object>();

        public ConcurrentDictionary<string, object> Data
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
        public int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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

        public int ExecuteNonQuery(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public int ExecuteNonQuery(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
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
        public SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public SqlDataReader ExecuteReader(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public SqlDataReader ExecuteReader(IDbConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public ISqlDataReaderHelper ExecuteReader(IDbConnection connection, string procName, params IDataParameter[] parameters)
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

        public ISqlDataReaderHelper ExecuteReader(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
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
        public SqlDataReader ExecuteReaderWithTimeOut(string connectionString, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
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
                            //if (cmd.Connection.State != ConnectionState.Open) cmd.Connection.Open();
                            sw = new Stopwatch();
                            sw?.Start();
                            rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection, ApplicationSettings.MaxAutoRetriesSql, ApplicationSettings.AutoRetrySleepMsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql);
                            sw?.Stop();
                            LoggingUtils.LogSQLOutData(cmd.Parameters, vAutoLogFunction);
                            if (sw != null)
                            {
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        /// <param name="conn">A SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the results</returns>
        public SqlDataReader ExecuteReaderWithTimeOut(SqlConnection connection, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
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

        public SqlDataReader ExecuteReaderWithTimeOut(SqlTransaction transaction, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
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
        public object ExecuteScalarForProcedure(string connectionString, string cmdText, params SqlParameter[] commandParameters)
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
                                    LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public object ExecuteScalarForProcedure(SqlTransaction transaction, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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

        public object ExecuteScalarForProcedure(IDbConnection connection, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText)
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
        public SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText, params SqlParameter[] commandParameters)
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
        public object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                    LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public object ExecuteScalar(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public object ExecuteScalar(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public object ExecuteScalarWithTimeOut(string connectionString, CommandType cmdType, string cmdText, int cmdTimeOut, params SqlParameter[] commandParameters)
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
                                    LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public long InsertAndReturnIdentity(string connString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                    LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public long InsertAndReturnIdentity(SqlTransaction transaction, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        public long InsertAndReturnIdentity(SqlConnection connection, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
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
                                LoggingUtils.ProcessStopWatch(ref sw, CommonLogger.FunctionFullName(MethodBase.GetCurrentMethod()), cmd);
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
        private void Cmd_StatementCompleted(object sender, StatementCompletedEventArgs e)
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

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
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
        // private void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms) {
        private void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms, int iTimeOut = 0)
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

        public IDbCommand BuildQuery(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters)
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

        public IDbCommand BuildCommand(IDbConnection connection, string procName, params IDataParameter[] parameters)
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

        public IDataParameter[] BuildParameterBlock(List<string> parameterName)
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

        public IDataParameter[] CloneParameterBlock(IDataParameter[] parameters)
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
        public string QueryFormatter(string SqlQuery)
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
        /// This function is used for replacing the characters and wild card search  which are keywords
        /// in sqlserver and replace with escape characters and return
        /// </summary>
        /// <param name="SqlQuery">string, This is Search Text</param>
        public string QueryWildCardFormatter(string SqlQuery)
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

        public string QueryFormatterLIKE(string SqlQuery)
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

        public string ConvertInjectionCharacters(string SqlQuery)
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

        private void CloseConnection(SqlConnection conn)
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

        private void CloseCommandConnection(SqlCommand cmd)
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

        public bool IsPhoneNumber(string number)
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

        public bool IsEmailAddress(string address)
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

        public bool IsValidConnectionString(string connectionString)
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

        public Int16 GetInt16Value(object dataField, Int16 _default = 0)
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

        public Int32 GetInt32Value(object dataField, Int32 _default = 0)
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

        public Int64 GetInt64Value(object dataField, Int64 _default = 0)
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

        public double GetDoubleValue(object dataField, double _default = 0.0)
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

        public decimal GetDecimalValue(object dataField, decimal _default = 0)
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

        public byte GetByteValue(object dataField, byte _default = 0)
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

        public DateTime GetDateTimeValue(object dataField, DateTime _default)
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

        public string GetStringValue(object dataField, string _default = "")
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

        public string HtmlToPlainText(string html)
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
    }
}
