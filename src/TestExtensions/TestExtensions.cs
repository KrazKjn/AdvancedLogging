using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using AdvancedLogging.Logging;

namespace AdvancedLogging.TestExtensions
{
    public static class TestExtensions
    {
        public static string PropertyDiff<T>(this T self, T to, params string[] ignore)
            where T : class
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { self, to, ignore }))
            {
                try
                {
                    var sb = new StringBuilder();
                    if (CheckForNullEquality(self, to, sb, string.Empty))
                    {
                        return sb.ToString();
                    }

                    var type = typeof(T);

                    foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (ignore.Contains(pi.Name))
                        {
                            continue;
                        }

                        object selfValue = type.GetProperty(pi.Name)?.GetValue(self, null);

                        object toValue = type.GetProperty(pi.Name)?.GetValue(to, null);

                        if (CheckForNullEquality(selfValue, toValue, sb, pi.Name))
                        {
                            continue;
                        }

                        Debug.Assert(selfValue != null, "selfValue != null");
                        var propertyType = selfValue.GetType();

                        if ((propertyType.IsPrimitive || propertyType == typeof(string) || propertyType == typeof(Guid) || propertyType.IsEnum) && !selfValue.Equals(toValue))
                        {
                            sb.Append($"Property {pi.Name} does not match. Expected: {selfValue} but was: {toValue}.\r\n");
                        }
                        else if (propertyType == typeof(DateTime))
                        {
                            Debug.Assert(toValue != null, "toValue != null");
                            if (((DateTime)selfValue).ToString("MMM dd, yyyy HH:mm") != ((DateTime)toValue).ToString("MMM dd, yyyy HH:mm"))
                            {
                                sb.Append($"Property {pi.Name} does not match. Expected: {selfValue} but was: {toValue}.\r\n");
                            }
                        }
                    }

                    return sb.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { self, to, ignore }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string AreEquals<T>(this IEnumerable<T> self, IEnumerable<T> to, params string[] ignore)
            where T : class
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { self, to, ignore }))
            {
                try
                {
                    var sb = new StringBuilder();
                    if (CheckForNullEquality(self, to, sb, string.Empty))
                    {
                        return sb.ToString();
                    }

                    var selfCount = self.Count();
                    var toCount = to.Count();

                    if (selfCount != toCount)
                    {
                        sb.Append($"Property length is not equal. self equals {selfCount}, to equals {toCount}");
                        return sb.ToString();
                    }

                    for (var i = 0; i < selfCount; i++)
                    {
                        string temp = PropertyDiff(self.ElementAt(i), to.ElementAt(i), ignore);

                        if (temp != string.Empty)
                        {
                            sb.AppendLine($"Objects at {i} are not equal.");
                            sb.AppendLine($"Diff equals: {temp}");
                        }
                    }

                    return sb.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { self, to, ignore }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static bool CheckForNullEquality(object expected, object actual, StringBuilder sb, string propertyName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { expected, actual, sb, propertyName }))
            {
                try
                {
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        propertyName += " ";
                    }

                    if (expected == null && actual != null)
                    {
                        sb.Append($"{propertyName}Expected is null, Actual is not.\r\n");
                        return true;
                    }

                    if (actual != null || expected == null)
                    {
                        return actual == null;
                    }

                    sb.Append($"{propertyName}Expected is not null, Actual is.\r\n");
                    return true;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { expected, actual, sb, propertyName }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
