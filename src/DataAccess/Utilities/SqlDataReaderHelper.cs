using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace AdvancedLogging.Utilities
{

    public class SqlDataReaderHelper : ISqlDataReaderHelper
    {
        private readonly SqlDataReader _reader;
        private Dictionary<string, int> _columns;

        public SqlDataReaderHelper(IDataReader reader)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { reader }))
            {
                try
                {
                    _reader = (SqlDataReader)reader;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { reader }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public int FieldCount => _reader.FieldCount;

        public int GetOrdinal(string columnName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { columnName }))
            {
                try
                {
                    return GetOrdinal(columnName, false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { columnName }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public string GetName(int i)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { i }))
            {
                try
                {
                    string retVal = _reader.GetName(i);
                    if (string.IsNullOrEmpty(retVal)) { retVal = i.ToString(); }
                    return retVal;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { i }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Type GetFieldType(int i)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { i }))
            {
                try
                {
                    return _reader.GetFieldType(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { i }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool ColumnNameExists(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    return GetOrdinal(column, false) >= 0;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Guid GetGuid(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return _reader.IsDBNull(i) ? Guid.Empty : _reader.GetGuid(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Guid? GetGuidN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    Guid? result = null;
                    if (!_reader.IsDBNull(i))
                    {
                        result = _reader.GetGuid(i);
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public string GetString(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? null : _reader.GetString(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool GetBoolean(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i >= 0 && !_reader.IsDBNull(i) && _reader.GetBoolean(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool? GetBooleanN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? (bool?)null : _reader.GetBoolean(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public byte GetByte(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return Convert.ToByte(i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetByte(i));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public byte? GetByteN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return Convert.ToByte(i < 0 || _reader.IsDBNull(i) ? (int?)null : _reader.GetByte(i));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public int GetInt(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetInt32(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public int? GetIntN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? (int?)null : _reader.GetInt32(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public int GetInt16(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetInt16(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public long GetLong(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetInt64(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public long? GetLongN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? (long?)null : _reader.GetInt64(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public double GetDouble(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetDouble(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public double? GetDoubleN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? (double?)null : _reader.GetDouble(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public decimal GetDecimal(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetDecimal(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public decimal? GetDecimalN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? (decimal?)null : _reader.GetDecimal(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public float GetFloat(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    return i < 0 || _reader.IsDBNull(i) ? 0 : _reader.GetFloat(i);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime GetDateTime(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    return GetUtc(column);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime? GetDateTimeN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    return GetUtcN(column);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime GetDate(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    var result = i < 0 || _reader.IsDBNull(i) ? DateTime.MinValue : _reader.GetDateTime(i);
                    result = DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime? GetDateN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    DateTime? result = null;
                    if (i >= 0 && !_reader.IsDBNull(i))
                    {
                        result = DateTime.SpecifyKind(_reader.GetDateTime(i), DateTimeKind.Unspecified);
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime GetUtc(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    var result = i < 0 || _reader.IsDBNull(i) ? DateTime.MinValue : _reader.GetDateTime(i);
                    result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime? GetUtcN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    DateTime? result = null;
                    if (i >= 0 && !_reader.IsDBNull(i))
                    {
                        result = DateTime.SpecifyKind(_reader.GetDateTime(i), DateTimeKind.Utc);
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime GetLocalDateTime(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    var result = i < 0 || _reader.IsDBNull(i) ? DateTime.MinValue : _reader.GetDateTime(i);
                    result = DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public DateTime? GetLocalDateTimeN(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    DateTime? result = null;
                    if (i >= 0 && !_reader.IsDBNull(i))
                    {
                        result = DateTime.SpecifyKind(_reader.GetDateTime(i), DateTimeKind.Unspecified);
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public XmlDocument GetXmlDocument(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    XmlDocument result = null;
                    var i = GetOrdinal(column, true);
                    var sx = i < 0 || _reader.IsDBNull(i) ? null : _reader.GetSqlXml(i);
                    if (sx != null)
                    {
                        result = new XmlDocument();
                        result.Load(sx.CreateReader());
                    }

                    return result;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public byte[] GetBinary(string column)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { column }))
            {
                try
                {
                    var i = GetOrdinal(column, true);
                    var sb = i < 0 || _reader.IsDBNull(i) ? null : _reader.GetSqlBinary(i);
                    return sb.IsNull ? null : sb.Value;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { column }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool Read()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    return _reader.Read();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool HasRows => _reader.HasRows;

        public bool NextResult()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    // clear the column name cache.
                    _columns = null;
                    return _reader.NextResult();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void Close()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    // clear the column name cache.
                    _columns = null;
                    _reader.Close();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    _reader.Dispose();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        protected int GetOrdinal(string columnName, bool throwIfColumnMissing)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { columnName, throwIfColumnMissing }))
            {
                try
                {
                    if (_columns == null)
                    {
                        _columns = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                        for (var fieldIndex = 0; fieldIndex < FieldCount; fieldIndex++)
                        {
                            var fieldName = GetName(fieldIndex);
                            if (!_columns.ContainsKey(fieldName))
                            {
                                _columns.Add(fieldName, fieldIndex);
                            }
                        }
                    }

                    if (_columns.ContainsKey(columnName))
                    {
                        return _columns[columnName];
                    }

                    if (throwIfColumnMissing)
                    {
                        throw new Exception($"db.InvalidColumn: The column named '{columnName}' doesn't exist.");
                    }

                    return -1;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { columnName, throwIfColumnMissing }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
