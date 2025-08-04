using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using AdvancedLogging.Interfaces;

namespace AdvancedLogging.Utilities
{
    public class DataObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;
        private readonly int _maxLevels;
        private readonly ICommonLogger _logger;


        private DataObjectDumper(int indentSize, int maxLevels, ICommonLogger logger)
        {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
            _maxLevels = maxLevels;
            _logger = logger;
        }

        public static string Dump(object element, int indentSize = 2, int maxLevels = -1, ICommonLogger logger = null)
        {
            var instance = new DataObjectDumper(indentSize, maxLevels, logger);
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
                    if (_maxLevels > 0 && (_level + 1) > _maxLevels)
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

            if (args != null && args.Length > 0)
                value = string.Format(value, args);

            if (_logger != null)
            {
                _logger.Debug(space + value);
            }
            else
            {
                _stringBuilder.AppendLine(space + value);
            }
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
