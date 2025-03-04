using AdvancedLogging.Loggers;
using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace AdvancedLogging.Utilities
{
    public class ObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;

#if DEBUG
        private readonly List<string> m_lstBreakOn = new List<string>() { "Params:", "Item:", "QueryString:", "Form:", "Headers:", "ServerVariables:", "Files:", "Cookies:" };
#endif
        private static int m_iMaxLevels = -1;
        public static int Maxlevels
        {
            get { return m_iMaxLevels; }
            set { m_iMaxLevels = value; }
        }
        private static ICommonLogger m_Logger = null;
        public static ICommonLogger Logger
        {
            get { return m_Logger; }
            set { m_Logger = value; }
        }
#if __IOS__		
        private static AutoLogFunction? m_AutoLogger = default!;
        public static AutoLogFunction? AutoLogger
#else
        private static AutoLogFunction m_AutoLogger = null;
        public static AutoLogFunction AutoLogger
#endif
        {
            get { return m_AutoLogger; }
            set { m_AutoLogger = value; }
        }

        private ObjectDumper(int indentSize)
        {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static void Reset()
        {
            m_iMaxLevels = -1;
#if __IOS__		
            m_Logger = default!;
            m_AutoLogger = default!;
#else
            m_Logger = null;
            m_AutoLogger = null;
#endif
        }
        public static string Dump(object element)
        {
            return Dump(element, 2);
        }
        public static string Dump(object element, Log4NetLogger _Logger, int _levelMax = -1)
        {
            m_Logger = _Logger;
            m_iMaxLevels = _levelMax;
            return Dump(element, 2);
        }
        public static string Dump(object element, AutoLogFunction _AutoLogger, int _levelMax = -1)
        {
            m_AutoLogger = _AutoLogger;
            m_iMaxLevels = _levelMax;
            return Dump(element, 2);
        }

        public static string Dump(object element, int indentSize)
        {
            var instance = new ObjectDumper(indentSize);
            return instance.DumpElement(element);
        }
        public static string Dump(object element, int indentSize, Log4NetLogger _Logger)
        {
            var instance = new ObjectDumper(indentSize);
            m_Logger = _Logger;
            return instance.DumpElement(element);
        }
        public static string Dump(object element, int indentSize, AutoLogFunction _AutoLogger)
        {
            var instance = new ObjectDumper(indentSize);
            m_AutoLogger = _AutoLogger;
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
                        var response = new System.Text.StringBuilder();
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

#if __IOS__		
                        var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo?.PropertyType;
                        object? value = null;
                        if (type?.FullName == "System.Reflection.MethodBase" && memberInfo.Name == "DeclaringMethod")
#else
                        var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                        object value = null;
                        if (type.FullName == "System.Reflection.MethodBase" && memberInfo.Name == "DeclaringMethod")
#endif
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
#if DEBUG
                        //System.Diagnostics.Debug.Assert(!memberInfo.Name.Equals("TypeHandle"));
#endif
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

            if (args != null && args.Length > 0)
                value = string.Format(value, args);

            //#if DEBUG
            //if (m_lstBreakOn.Any(value.Contains))
            //{
            //    //System.Diagnostics.Debugger.Break();
            //    System.Diagnostics.Debug.WriteLine("Break Point");
            //}
            //#endif

            System.Diagnostics.Debug.WriteLine(space + value);
            if (m_Logger == null && m_AutoLogger == null)
                _stringBuilder.AppendLine(space + value);
            else
            {
                if (m_AutoLogger == null)
#if __IOS__		
                    m_Logger?.Debug(space + value);
#else
                    m_Logger.Debug(space + value);
#endif
                else
                    m_AutoLogger.WriteDebug(space + value);
            }
        }

#if __IOS__		
        private string FormatValue(object? o)
#else
        private string FormatValue(object o)
#endif
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
