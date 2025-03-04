using AdvancedLogging.Logging;
using System;
using System.Diagnostics;

namespace AdvancedLogging.Extensions
{

    /// <summary>
    /// Extension helper methods for strings
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public static class StringExtensions
    {
        /// <summary>
        /// Formats a string using the <paramref name="format"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        /// <returns>A string with the format placeholders replaced by the args.</returns>
        public static string Sub(this string format, params object[] args)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { format, args }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    return string.Format(format, args);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { format, args }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified text is numeric digits.
        /// </summary>
        /// <param name="text"></param>
        /// <returns> <c>true</c> if the specified text is numeric digits; otherwise, <c>false</c>.</returns>
        public static bool IsNumericDigits(this string text)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { text }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    if (text == System.Text.RegularExpressions.Regex.Replace(text, "[^0-9]", "") && text.Trim() != string.Empty)
                        return true;
                    else
                        return false;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { text }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified error is allows for a retry.
        /// </summary>
        /// <param name="_ErrorMessage"></param>
        /// <returns> <c>true</c> if the specified error allows for a retry; otherwise, <c>false</c>.</returns>
        public static bool IsSqlRetryError(this string _ErrorMessage)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _ErrorMessage }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    return (_ErrorMessage.Contains("Timeout expired") ||
                        _ErrorMessage.Contains("The Bulk Insert operation of SQL Server Destination has timed out.") ||
                        _ErrorMessage.Contains("Rerun the query") ||
                        _ErrorMessage.Contains("Login failed due to timeout; the connection has been closed.") ||
                        _ErrorMessage.Contains("This may have been caused by client or server login timeout expiration."));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _ErrorMessage }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified error does not allow for a retry.
        /// </summary>
        /// <param name="_ErrorMessage"></param>
        /// <returns> <c>true</c> if the specified error does not allow for a retry; otherwise, <c>false</c>.</returns>
        public static bool IsSqlNonRetryError(this string _ErrorMessage)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { _ErrorMessage }, bSuppressFunctionDeclaration: true))
            {
                try
                {
                    return (_ErrorMessage.Contains("Incorrect syntax") ||
                        _ErrorMessage.Contains("Could not find") ||
                        _ErrorMessage.Contains("is not a parameter for procedure") ||
                        _ErrorMessage.Contains("has no parameters and arguments were supplied") ||
                        _ErrorMessage.Contains("An insufficient number of arguments were supplied for the procedure or function") ||
                        (_ErrorMessage.Contains("Procedure or function") && _ErrorMessage.Contains("has too many arguments specified.")) ||
                        (_ErrorMessage.Contains("Procedure or function") && _ErrorMessage.Contains("expects parameter") && _ErrorMessage.Contains("which was not supplied.")));
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { _ErrorMessage }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Converts a byte array to a Hexidecimal string.
        /// </summary>
        /// <param name="_bytes"></param>
        /// <returns></returns>
        public static string ToStringHex(this byte[] _bytes)
        {
            return "&H" + BitConverter.ToString(_bytes).Replace("-", string.Empty);
        }
    }
}
