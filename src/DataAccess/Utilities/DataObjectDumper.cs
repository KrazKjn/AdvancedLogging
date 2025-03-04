using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace AdvancedLogging.Utilities
{
    public class DataObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;

        private static int m_iMaxLevels = -1;
        public static int Maxlevels
        {
            get { return m_iMaxLevels; }
            set { m_iMaxLevels = value; }
        }

        private static bool m_bWriteToDebug = false;
        public static bool WriteToDebug
        {
            get { return m_bWriteToDebug; }
            set { m_bWriteToDebug = value; }
        }

        private static string m_strOutFile = "";
        public static string OutFile
        {
            get { return m_strOutFile; }
            set { m_strOutFile = value; }
        }

        private DataObjectDumper(int indentSize)
        {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static void Reset()
        {
            m_iMaxLevels = -1;
        }
        public static string Dump(object element)
        {
            return Dump(element, 2);
        }
        public static string Dump(object element, int indentSize, int _levelMax = -1, bool _writeDebug = false, string _strOutFile = "")
        {
            m_iMaxLevels = _levelMax;
            m_bWriteToDebug = _writeDebug;
            m_strOutFile = _strOutFile;
            return Dump(element, indentSize);
        }

        public static string Dump(object element, int indentSize)
        {
            var instance = new DataObjectDumper(indentSize);
            return instance.DumpElement(element);
        }
        private string DumpElement(object element)
        {
            if (element == null || element is ValueType || element is string)
            {
                Write(FormatValue(element));
            }
            else
            {
                var objectType = element.GetType();
                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    if (m_iMaxLevels > 0 && (_level + 1) > m_iMaxLevels)
                    {
                        return _stringBuilder.ToString();
                    }
                    Write("{{{0}}}", objectType.FullName);
                    _hashListOfFoundElements.Add(element.GetHashCode());
                    _level++;
                }

                if (element is IEnumerable enumerableElement)
                {
                    foreach (object item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            _level++;
                            DumpElement(item);
                            _level--;
                        }
                        else
                        {
                            if (!AlreadyTouched(item))
                                DumpElement(item);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                        }
                    }
                    if (element is WebHeaderCollection webHeaderCollection)
                    {
                        foreach (string k in webHeaderCollection.Keys)
                            Write("{0}: {1}", k, webHeaderCollection[k]);
                    }
                    if (element is CookieCollection cookieCollection)
                    {
                        for (int i = 0; i < cookieCollection.Count; i++)
                            Write("{0}({1}): {2}", cookieCollection[i].Name, cookieCollection[i].Expires, cookieCollection[i].Value);
                    }
                }
                else
                {
                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var memberInfo in members)
                    {
                        var fieldInfo = memberInfo as FieldInfo;
                        var propertyInfo = memberInfo as PropertyInfo;

                        if (fieldInfo == null && propertyInfo == null)
                            continue;

                        var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                        object value;
                        if (type.FullName == "System.Reflection.MethodBase" && memberInfo.Name == "DeclaringMethod")
                        {
                            value = memberInfo.ToString();
                        }
                        else
                        {
                            try
                            {
                                value = fieldInfo != null
                                                   ? fieldInfo.GetValue(element)
                                                   : propertyInfo.GetValue(element, null);

                                // This was an attempt to get more data when there are errors using the above code.
                                // Needs more research

                                //if (value == null)
                                //    value = propertyInfo.GetType().GetProperties().Where(p => p.GetIndexParameters().Length == 0);
                            }
                            catch (Exception ex)
                            {
                                if (ex.InnerException == null)
                                    value = ex.Message;
                                else
                                    value = ex.InnerException.Message;
                            }
                        }
                        if (type.IsValueType || type == typeof(string))
                        {
                            Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                        }
                        else
                        {
                            var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                            Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                            var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                            _level++;
                            if (!alreadyTouched)
                                DumpElement(value);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                            _level--;
                        }
                    }
                }

                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    _level--;
                }
            }

            return _stringBuilder.ToString();
        }

        private bool AlreadyTouched(object value)
        {
            if (value == null)
                return false;

            var hash = value.GetHashCode();
            for (var i = 0; i < _hashListOfFoundElements.Count; i++)
            {
                if (_hashListOfFoundElements[i] == hash)
                    return true;
            }
            return false;
        }

        private void Write(string value, params object[] args)
        {
            var space = new string(' ', _level * _indentSize);

            if (args != null)
                value = string.Format(value, args);

            if (WriteToDebug)
            {
                System.Diagnostics.Debug.WriteLine(space + value);
            }
            if (OutFile.Length > 0)
            {
                System.IO.StreamWriter sw = System.IO.File.AppendText(OutFile);
                sw.WriteLine(space + value);
                sw.Close();
            }
            _stringBuilder.AppendLine(space + value);
        }

        private string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime dt)
                return dt.ToShortDateString();

            if (o is string)
                return string.Format("\"{0}\"", o);

            if (o is char ch && ch == '\0')
                return string.Empty;

            if (o is ValueType)
                return (o.ToString());

            if (o is IEnumerable)
                return ("...");

            return ("{ }");
        }
    }
}
