namespace AdvancedLogging.Interfaces
{
    using System;
    using System.Xml;

    public interface ISqlDataReaderHelper : IDisposable
    {
        int GetOrdinal(string column);

        Guid GetGuid(string column);

        Guid? GetGuidN(string column);

        string GetString(string column);

        bool GetBoolean(string column);

        bool? GetBooleanN(string column);

        byte GetByte(string column);

        byte? GetByteN(string column);

        int GetInt(string column);

        int? GetIntN(string column);

        int GetInt16(string column);

        long GetLong(string column);

        long? GetLongN(string column);

        decimal GetDecimal(string column);

        decimal? GetDecimalN(string column);

        double GetDouble(string column);

        double? GetDoubleN(string column);

        float GetFloat(string column);

        DateTime GetDateTime(string column);

        DateTime? GetDateTimeN(string column);

        DateTime GetDate(string column);

        DateTime? GetDateN(string column);

        DateTime GetLocalDateTime(string column);

        DateTime? GetLocalDateTimeN(string column);

        XmlDocument GetXmlDocument(string column);

        byte[] GetBinary(string column);

        bool ColumnNameExists(string column);

        bool Read();

        bool HasRows { get; }

        bool NextResult();

        void Close();

        int FieldCount { get; }

        string GetName(int i);

        Type GetFieldType(int i);
    }
}
