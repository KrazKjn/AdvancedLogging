using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace AdvancedLogging.TestExtensions
{
    public class QueryTestHelper
    {
        public static bool CompareQueryResultsOneRowOnly(ISqlHelper sqlHelper, string connectionString, string query1, string query2, IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query1, query2, parameters }))
            {
                try
                {
                    bool returnValue = true;
                    int rowCount = 0;
                    string returnMessage = "";

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        Dictionary<string, System.Type> columnInfo = new Dictionary<string, System.Type>();
                        List<string> columnInfo1 = new List<string>();
                        List<List<string>> query1Results = new List<List<string>>();
                        List<string> columnNames = new List<string>();
                        int columnCount;
                        int currentRow = 0;

                        try
                        {
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query1, parameters))
                            {
                                columnCount = resultSet.FieldCount;
                                for (int i = 0; i < resultSet.FieldCount; i++)
                                {
                                    columnInfo1.Add(resultSet.GetName(i));
                                }
                                while (resultSet.Read())
                                {
                                    List<string> row = new List<string>();
                                    row = GetColumnValue(row, resultSet, columnCount, columnInfo1);
                                    query1Results.Add(row);
                                }
                            }
                            rowCount = query1Results.Count;
                            if (rowCount == 0)
                            {
                                throw new Exception("No rows returned!");
                            }
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query2, sqlHelper.CloneParameterBlock(parameters)))
                            {
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    columnInfo.Add(columnName, resultSet.GetFieldType(i));
                                    columnNames.Add(columnName);
                                }
                                if (columnCount != resultSet.FieldCount)
                                {
                                    throw new Exception("Field Count does not match");
                                }
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    if (columnNames[i] != columnName)
                                    {
                                        throw new Exception("Field Name does not match");
                                    }
                                    if (columnInfo[columnName] != resultSet.GetFieldType(i))
                                    {
                                        throw new Exception("Field Type does not match");
                                    }
                                }
                                while (resultSet.Read())
                                {
                                    List<string> expectedRow = query1Results[currentRow++];
                                    List<string> actualRow = new List<string>();

                                    for (int i = 0; i < columnCount; i++)
                                    {
                                        actualRow = GetColumnValue(actualRow, resultSet, columnCount, columnInfo1);
                                        if (expectedRow[i] != actualRow[i])
                                        {
                                            throw new Exception("A column value does not match");
                                        }
                                    }
                                }
                            }
                            if (currentRow != query1Results.Count)
                            {
                                throw new Exception("Row Counts do not match");
                            }
                        }
                        catch (Exception ex)
                        {
                            returnMessage = ex.Message;
                            returnValue = false;
                        }
                    }
                    if (rowCount > 1)
                    {
                        returnMessage = string.Format("{0} row(s) returned, only 1 row expected!", rowCount);
                        throw new Exception(returnMessage);
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query1, query2, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool CompareQueryResultsOneRowOnly(ISqlHelper sqlHelper, string connectionString, string query1, string query2, IDataParameter[] parameters, out string returnMessage)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query1, query2, parameters }))
            {
                try
                {
                    bool returnValue = true;
                    returnMessage = "Queries matched with 1 row returned.";
                    int rowCount = 0;

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        Dictionary<string, System.Type> columnInfo = new Dictionary<string, System.Type>();
                        List<string> columnInfo1 = new List<string>();
                        List<List<string>> query1Results = new List<List<string>>();
                        List<string> columnNames = new List<string>();
                        int columnCount;
                        int currentRow = 0;

                        try
                        {
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query1, parameters))
                            {
                                columnCount = resultSet.FieldCount;
                                for (int i = 0; i < resultSet.FieldCount; i++)
                                {
                                    columnInfo1.Add(resultSet.GetName(i));
                                }
                                while (resultSet.Read())
                                {
                                    List<string> row = new List<string>();
                                    row = GetColumnValue(row, resultSet, columnCount, columnInfo1);
                                    query1Results.Add(row);
                                }
                            }
                            rowCount = query1Results.Count;
                            if (rowCount == 0)
                            {
                                throw new Exception("No rows returned!");
                            }
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query2, sqlHelper.CloneParameterBlock(parameters)))
                            {
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    columnInfo.Add(columnName, resultSet.GetFieldType(i));
                                    columnNames.Add(columnName);
                                }
                                if (columnCount != resultSet.FieldCount)
                                {
                                    throw new Exception("Field Count does not match");
                                }
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    if (columnNames[i] != columnName)
                                    {
                                        throw new Exception("Field Name does not match");
                                    }
                                    if (columnInfo[columnName] != resultSet.GetFieldType(i))
                                    {
                                        throw new Exception("Field Type does not match");
                                    }
                                }
                                while (resultSet.Read())
                                {
                                    List<string> expectedRow = query1Results[currentRow++];
                                    List<string> actualRow = new List<string>();

                                    for (int i = 0; i < columnCount; i++)
                                    {
                                        actualRow = GetColumnValue(actualRow, resultSet, columnCount, columnInfo1);
                                        if (expectedRow[i] != actualRow[i])
                                        {
                                            throw new Exception("A column value does not match");
                                        }
                                    }
                                }
                            }
                            if (currentRow != query1Results.Count)
                            {
                                throw new Exception("Row Counts do not match");
                            }
                        }
                        catch (Exception ex)
                        {
                            returnMessage = ex.Message;
                            returnValue = false;
                        }
                    }
                    if (rowCount > 1)
                    {
                        returnMessage = string.Format("{0} row(s) returned, only 1 row expected!", rowCount);
                        throw new Exception(returnMessage);
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query1, query2, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool CompareQueryResults(ISqlHelper sqlHelper, string connectionString, string query1, string query2, IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query1, query2, parameters }))
            {
                try
                {
                    bool returnValue = true;
                    string returnMessage = "";

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        Dictionary<string, System.Type> columnInfo = new Dictionary<string, System.Type>();
                        List<string> columnInfo1 = new List<string>();
                        List<List<string>> query1Results = new List<List<string>>();
                        List<string> columnNames = new List<string>();
                        int columnCount;
                        int currentRow = 0;

                        try
                        {
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query1, parameters))
                            {
                                columnCount = resultSet.FieldCount;
                                for (int i = 0; i < resultSet.FieldCount; i++)
                                {
                                    columnInfo1.Add(resultSet.GetName(i));
                                }
                                while (resultSet.Read())
                                {
                                    List<string> row = new List<string>();
                                    row = GetColumnValue(row, resultSet, columnCount, columnInfo1);
                                    query1Results.Add(row);
                                }
                            }
                            if (query1Results.Count == 0)
                            {
                                throw new Exception("No rows returned!");
                            }
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query2, sqlHelper.CloneParameterBlock(parameters)))
                            {
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    columnInfo.Add(columnName, resultSet.GetFieldType(i));
                                    columnNames.Add(columnName);
                                }
                                if (columnCount != resultSet.FieldCount)
                                {
                                    throw new Exception("Field Count does not match");
                                }
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    if (columnNames[i] != columnName)
                                    {
                                        throw new Exception("Field Name does not match");
                                    }
                                    if (columnInfo[columnName] != resultSet.GetFieldType(i))
                                    {
                                        throw new Exception("Field Type does not match");
                                    }
                                }
                                while (resultSet.Read())
                                {
                                    List<string> expectedRow = query1Results[currentRow++];
                                    List<string> actualRow = new List<string>();

                                    for (int i = 0; i < columnCount; i++)
                                    {
                                        actualRow = GetColumnValue(actualRow, resultSet, columnCount, columnInfo1);
                                        if (expectedRow[i] != actualRow[i])
                                        {
                                            throw new Exception("A column value does not match");
                                        }
                                    }
                                }
                            }
                            if (currentRow != query1Results.Count)
                            {
                                throw new Exception("Row Counts do not match");
                            }
                        }
                        catch (Exception ex)
                        {
                            returnMessage = ex.Message;
                            returnValue = false;
                        }
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query1, query2, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool CompareQueryResults(ISqlHelper sqlHelper, string connectionString, string query1, string query2, IDataParameter[] parameters, out string returnMessage)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query1, query2, parameters }))
            {
                try
                {
                    bool returnValue = true;
                    returnMessage = "";

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        Dictionary<string, System.Type> columnInfo = new Dictionary<string, System.Type>();
                        List<string> columnInfo1 = new List<string>();
                        List<List<string>> query1Results = new List<List<string>>();
                        List<string> columnNames = new List<string>();
                        int columnCount;
                        int currentRow = 0;

                        try
                        {
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query1, parameters))
                            {
                                columnCount = resultSet.FieldCount;
                                for (int i = 0; i < resultSet.FieldCount; i++)
                                {
                                    columnInfo1.Add(resultSet.GetName(i));
                                }
                                while (resultSet.Read())
                                {
                                    List<string> row = new List<string>();
                                    row = GetColumnValue(row, resultSet, columnCount, columnInfo1);
                                    query1Results.Add(row);
                                }
                            }
                            if (query1Results.Count == 0)
                            {
                                throw new Exception("No rows returned!");
                            }
                            using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query2, sqlHelper.CloneParameterBlock(parameters)))
                            {
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    columnInfo.Add(columnName, resultSet.GetFieldType(i));
                                    columnNames.Add(columnName);
                                }
                                if (columnCount != resultSet.FieldCount)
                                {
                                    throw new Exception("Field Count does not match");
                                }
                                for (int i = 0; i < columnCount; i++)
                                {
                                    string columnName = resultSet.GetName(i);
                                    if (columnNames[i] != columnName)
                                    {
                                        throw new Exception("Field Name does not match");
                                    }
                                    if (columnInfo[columnName] != resultSet.GetFieldType(i))
                                    {
                                        throw new Exception("Field Type does not match");
                                    }
                                }
                                while (resultSet.Read())
                                {
                                    List<string> expectedRow = query1Results[currentRow++];
                                    List<string> actualRow = new List<string>();

                                    for (int i = 0; i < columnCount; i++)
                                    {
                                        actualRow = GetColumnValue(actualRow, resultSet, columnCount, columnInfo1);
                                        if (expectedRow[i] != actualRow[i])
                                        {
                                            throw new Exception("A column value does not match");
                                        }
                                    }
                                }
                            }
                            if (currentRow != query1Results.Count)
                            {
                                throw new Exception("Row Counts do not match");
                            }
                        }
                        catch (Exception ex)
                        {
                            returnMessage = ex.Message;
                            returnValue = false;
                        }
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query1, query2, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool CompareQueryResults(ISqlHelper sqlHelper, string connectionString, string query, IDataParameter[] parameters, List<string> expectedResults, char delimiter)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query, parameters, expectedResults, delimiter }))
            {
                try
                {
                    bool returnValue = true;

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        int rowNumber = 0;
                        List<string> columnInfo = new List<string>();

                        using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query, parameters))
                        {
                            for (int i = 0; i < resultSet.FieldCount; i++)
                            {
                                columnInfo.Add(resultSet.GetName(i));
                            }
                            returnValue = expectedResults[rowNumber].Split(delimiter).GetLength(0) == resultSet.FieldCount;

                            while (returnValue && resultSet.Read())
                            {
                                if (rowNumber <= expectedResults.Count)
                                {
                                    string[] rowData = expectedResults[rowNumber++].Split(delimiter);
                                    if (rowData.GetLength(0) == resultSet.FieldCount)
                                    {
                                        for (int i = 0; i < resultSet.FieldCount; i++)
                                        {
                                            string expectedValue = rowData[i];
                                            string columnName = columnInfo[i];
                                            string actualValue = resultSet.GetString(columnName);
                                            if (expectedValue != actualValue)
                                            {
                                                returnValue = false;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        returnValue = false;
                                    }
                                }
                                else
                                {
                                    returnValue = false;
                                }
                            }
                            returnValue = returnValue && (expectedResults.Count == rowNumber);
                        }
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query, parameters, expectedResults, delimiter }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogQuery(log4net.ILog logger, string queryID, string query, IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logger, queryID, query, parameters }))
            {
                try
                {
                    logger.Info(new string('=', 80));
                    logger.InfoFormat("Query ID: {0}", queryID);
                    logger.InfoFormat("{0}\nQuery:\n{1}\n{0}", new string('-', 80), query);
                    if (parameters == null || parameters.Length == 0)
                    {
                        if (logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("No Parameters:");
                        else
                            vAutoLogFunction.WriteWarnFormat("No Parameters:");
                    }
                    else
                    {
                        logger.Info("Parameters:");
                        foreach (IDataParameter item in parameters)
                        {
                            logger.InfoFormat("\t{0}({1})  => '{2}'", item.ParameterName, item.DbType.ToString(), item.Value);
                        }
                    }
                    logger.Info(new string('=', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { logger, queryID, query, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogQuery(log4net.ILog logger, string queryID, ISqlHelper sqlHelper, string connectionString, string query1, string query2, IDataParameter[] parameters, bool runQuery = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { logger, queryID, sqlHelper, connectionString, query1, query2, parameters, runQuery }))
            {
                try
                {
                    logger.Info(new string('=', 80));
                    logger.InfoFormat("Query ID: {0}", queryID);
                    logger.InfoFormat("{0}\nQuery 1:\n{1}\n{0}\nQuery 2:\n{2}\n{0}", new string('-', 80), query1, query2);
                    if (parameters == null || parameters.Length == 0)
                    {
                        if (logger.IsDebugEnabled)
                            vAutoLogFunction.WriteDebugFormat("No Parameters:");
                        else
                            vAutoLogFunction.WriteWarnFormat("No Parameters:");
                    }
                    else
                    {
                        logger.Info("Parameters:");
                        foreach (IDataParameter item in parameters)
                        {
                            logger.InfoFormat("\t{0}({1})  => '{2}'", item.ParameterName, item.DbType.ToString(), item.Value);
                        }
                    }
                    if (runQuery)
                    {
                        bool returnValue = CompareQueryResults(sqlHelper, connectionString, query1, query2, parameters);
                        string equalStatus = returnValue ? "equal" : "not equal";
                        logger.InfoFormat("{0}\nResults were {1}", new string('-', 80), equalStatus);

                    }

                    logger.Info(new string('=', 80));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { logger, queryID, sqlHelper, connectionString, query1, query2, parameters, runQuery }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LoadData(string tableName, ISqlHelper sqlHelper, string connectionString, List<string> columns, List<string> data, char delimiter)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { tableName, sqlHelper, connectionString, columns, data, delimiter }))
            {
                try
                {
                    string sqlInsertCommand = "INSERT INTO " + tableName + " (" + string.Join(",", columns) + ") VALUES (@" + string.Join(", @", columns).Replace('[', '_').Replace(']', '_') + ")";

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        foreach (string row in data)
                        {
                            IDataParameter[] parameters = sqlHelper.BuildParameterBlock(columns);

                            int i = 0;
                            foreach (string item in row.Split(delimiter))
                            {
                                parameters[i++].Value = item;
                            }
                            try
                            {
                                sqlHelper.ExecuteNonQuery(connection, CommandType.Text, sqlInsertCommand, parameters);
                            }
                            catch (Exception e)
                            {
                                if (ApplicationSettings.LogToDebugWindow)
                                    Debug.WriteLine("Error: " + e.Message);
                                throw new Exception(e.Message);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { tableName, sqlHelper, connectionString, columns, data, delimiter }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static List<List<string>> QueryAsList(ISqlHelper sqlHelper, string connectionString, string query, IDataParameter[] parameters)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { sqlHelper, connectionString, query, parameters }))
            {
                try
                {
                    List<List<string>> results = new List<List<string>>();

                    using (IDbConnection connection = sqlHelper.OpenNewConnection(connectionString))
                    {
                        List<string> columnInfo = new List<string>();

                        using (ISqlDataReaderHelper resultSet = sqlHelper.ExecuteReader(connection, CommandType.Text, query, parameters))
                        {
                            for (int i = 0; i < resultSet.FieldCount; i++)
                            {
                                columnInfo.Add(resultSet.GetName(i));
                            }

                            while (resultSet.Read())
                            {
                                List<string> row = new List<string>();
                                for (int i = 0; i < resultSet.FieldCount; i++)
                                {
                                    string columnValue = string.Empty;
                                    switch (resultSet.GetFieldType(i).Name)
                                    {
                                        case "String":
                                            columnValue = resultSet.GetString(columnInfo[i]);
                                            break;
                                        case "UInt16":
                                        case "UInt32":
                                        case "Int16":
                                        case "Int32":
                                            columnValue = Convert.ToString(resultSet.GetInt(columnInfo[i]));
                                            break;
                                        case "Guid":
                                            columnValue = Convert.ToString(resultSet.GetGuid(columnInfo[i]));
                                            break;
                                        case "Boolean":
                                            columnValue = Convert.ToString(resultSet.GetBoolean(columnInfo[i]));
                                            break;
                                        case "Byte":
                                            columnValue = Convert.ToString(resultSet.GetByte(columnInfo[i]));
                                            break;
                                        case "Long":
                                            columnValue = Convert.ToString(resultSet.GetLong(columnInfo[i]));
                                            break;
                                        case "Double":
                                            columnValue = Convert.ToString(resultSet.GetDouble(columnInfo[i]));
                                            break;
                                        case "Float":
                                            columnValue = Convert.ToString(resultSet.GetFloat(columnInfo[i]));
                                            break;
                                        case "DateTime":
                                            columnValue = Convert.ToString(resultSet.GetDateTime(columnInfo[i]));
                                            break;
                                        case "Date":
                                            columnValue = Convert.ToString(resultSet.GetDate(columnInfo[i]));
                                            break;
                                        default:
                                            break;
                                    }
                                    row.Add(columnValue);
                                }
                                results.Add(row);
                            }
                        }
                    }

                    return results;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { sqlHelper, connectionString, query, parameters }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static List<string> GetColumnValue(List<string> row, ISqlDataReaderHelper resultSet, int columnCount, List<string> columnInfo1)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { row, resultSet, columnCount, columnInfo1 }))
            {
                try
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        string columnValue = string.Empty;
                        string typeName = resultSet.GetFieldType(i).Name;
                        switch (typeName)
                        {
                            case "String":
                                columnValue = resultSet.GetString(columnInfo1[i]);
                                break;
                            case "Int16":
                                columnValue = Convert.ToString(resultSet.GetInt16(columnInfo1[i]));
                                break;
                            case "UInt16":
                            case "UInt32":
                            case "Int32":
                                columnValue = Convert.ToString(resultSet.GetInt(columnInfo1[i]));
                                break;
                            case "Guid":
                                columnValue = Convert.ToString(resultSet.GetGuid(columnInfo1[i]));
                                break;
                            case "Boolean":
                                columnValue = Convert.ToString(resultSet.GetBoolean(columnInfo1[i]));
                                break;
                            case "Byte":
                                columnValue = Convert.ToString(resultSet.GetByte(columnInfo1[i]));
                                break;
                            case "Long":
                            case "Int64":
                                columnValue = Convert.ToString(resultSet.GetLong(columnInfo1[i]));
                                break;
                            case "Double":
                                columnValue = Convert.ToString(resultSet.GetDouble(columnInfo1[i]));
                                break;
                            case "Float":
                                columnValue = Convert.ToString(resultSet.GetFloat(columnInfo1[i]));
                                break;
                            case "DateTime":
                                columnValue = Convert.ToString(resultSet.GetDateTime(columnInfo1[i]));
                                break;
                            case "Date":
                                columnValue = Convert.ToString(resultSet.GetDate(columnInfo1[i]));
                                break;
                            default:
                                break;
                        }
                        row.Add(columnValue);
                    }
                    return row;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}