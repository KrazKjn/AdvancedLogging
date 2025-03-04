using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AdvancedLogging.Interfaces
{
    public interface ISqlHelper
    {
        IDbConnection OpenNewConnection(string connectionString);

        ISqlDataReaderHelper ExecuteReader(IDbConnection connection, string procName, params IDataParameter[] parameters);

        ISqlDataReaderHelper ExecuteReader(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters);

        int ExecuteNonQuery(IDbConnection connection, CommandType cmdType, string query, params IDataParameter[] parameters);

        IDbCommand BuildCommand(IDbConnection connection, string procName, params IDataParameter[] parameters);

        IDbCommand BuildQuery(IDbConnection connection, System.Data.CommandType cmdType, string query, params IDataParameter[] parameters);

        IDataParameter[] BuildParameterBlock(List<string> parameterName);

        IDataParameter[] CloneParameterBlock(IDataParameter[] parameters);

        int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters);

        object ExecuteScalarForProcedure(string connectionString, string cmdText, params SqlParameter[] commandParameters);

        int ExecuteNonQuery(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters);

        SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters);

        SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText);

        SqlDataAdapter ExecuteAdapter(string connectionString, string cmdText, params SqlParameter[] commandParameters);

        object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters);

        long InsertAndReturnIdentity(string connString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters);
    }
}
