using AdvancedLogging.Constants;
using AdvancedLogging.Enumerations;
using AdvancedLogging.Logging;
using AdvancedLogging.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace AdvancedLogging.Utilities
{
    public class SecurityProtocol
    {
        private static SecurityProtocolTypeCustom m_spAvailableSecurityProtocols = SecurityProtocolTypeCustom.SystemDefault;

        public static SecurityProtocolTypeCustom AvailableSecurityProtocols
        {
            get { return m_spAvailableSecurityProtocols; }
            set { m_spAvailableSecurityProtocols = value; }
        }

        public static void EnableAllTlsSupport(bool bForceRetry = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { bForceRetry }))
            {
                try
                {
                    if (AvailableSecurityProtocols != (SecurityProtocolTypeCustom)ServicePointManager.SecurityProtocol || bForceRetry)
                    {
                        List<SecurityProtocolTypeCustom> arrSecurityProtocols = new List<SecurityProtocolTypeCustom>() { SecurityProtocolTypeCustom.Tls,
                            SecurityProtocolTypeCustom.Tls11,
                            SecurityProtocolTypeCustom.Tls12,
                            SecurityProtocolTypeCustom.Tls13,
                            SecurityProtocolTypeCustom.Ssl3 };

                        string strErrorMsg = "";
                        Exception exLast = null;
                        AvailableSecurityProtocols = SecurityProtocolTypeCustom.SystemDefault;
                        foreach (SecurityProtocolTypeCustom sp in arrSecurityProtocols)
                        {
                            try
                            {
                                ServicePointManager.SecurityProtocol = (SecurityProtocolType)sp;
                                AvailableSecurityProtocols |= sp;
                                vAutoLogFunction.WriteDebugFormat(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] + 2, "SecurityProtocol {0} is Available.", sp.ToString());
#if DEBUG
                                LogToNT(string.Format("SecurityProtocol {0} is Available.", sp.ToString()));
#endif
                            }
                            catch (System.NotSupportedException)
                            {
                                // Ignore this Exception since the system does not support the selected SecurityProtocolType
                                vAutoLogFunction.WriteErrorFormat("SecurityProtocol {0} is NOT Available.", sp.ToString());
#if DEBUG
                                LogToNT(string.Format("SecurityProtocol {0} is NOT Available.", sp.ToString()));
#endif
                            }
                            catch (Exception ex)
                            {
                                exLast = ex;
                                if (strErrorMsg.Length > 0)
                                    strErrorMsg += Environment.NewLine;
                                strErrorMsg = "Enable " + sp.ToString() + " failed." + Environment.NewLine + ex.Message;
                                vAutoLogFunction.WriteError(strErrorMsg);
                            }
                        }
                        try
                        {
                            ServicePointManager.SecurityProtocol = (SecurityProtocolType)AvailableSecurityProtocols;
                            vAutoLogFunction.WriteDebug(LoggingUtils.DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] + 2, "SecurityProtocol is Set to Max Available.");
                        }
                        catch (Exception ex)
                        {
                            exLast = ex;
                            if (strErrorMsg.Length > 0)
                                strErrorMsg += Environment.NewLine;
                            strErrorMsg = "Enable Max Value of " + ((int)AvailableSecurityProtocols).ToString() + " failed." + Environment.NewLine + ex.Message;
                            vAutoLogFunction.WriteError(strErrorMsg);
                        }
                        if (strErrorMsg != "")
                        {
                            try
                            {
                                using (EventLog eventLog = new EventLog("Application"))
                                {
                                    eventLog.Source = "Application";
                                    StackTrace t = new StackTrace();
                                    StackFrame[] arrFrames = t.GetFrames();
                                    string strMsg = "Process Info:" + Environment.NewLine;
                                    for (int i = arrFrames.Length - 1; i >= 0; i--)
                                    {
                                        if (!CommonLogger.FunctionFullName(arrFrames[i].GetMethod()).Contains("ctor"))
                                            strMsg += CommonLogger.FunctionFullName(arrFrames[i].GetMethod()) + Environment.NewLine;
                                    }
                                    eventLog.WriteEntry(strMsg + strErrorMsg + Environment.NewLine + exLast?.ToString(), EventLogEntryType.Error, 1, 1);
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { bForceRetry }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void LogSecurityProtocol(CommonLogger Logger = null)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { Logger }))
            {
                try
                {
                    if (ServicePointManager.SecurityProtocol == (SecurityProtocolType)SecurityProtocolTypeCustom.SystemDefault)
                    {
                        vAutoLogFunction.WriteLog("Security Protocol: System Default is enabled.");
                    }
                    else
                    {
                        List<SecurityProtocolTypeCustom> arrSecurityProtocols = new List<SecurityProtocolTypeCustom>() { SecurityProtocolTypeCustom.Tls,
                           SecurityProtocolTypeCustom.Tls11,
                           SecurityProtocolTypeCustom.Tls12,
                           SecurityProtocolTypeCustom.Tls13,
                           SecurityProtocolTypeCustom.Ssl3 };
                        foreach (SecurityProtocolTypeCustom sp in arrSecurityProtocols)
                        {
                            try
                            {
                                if ((ServicePointManager.SecurityProtocol & (SecurityProtocolType)sp) == (SecurityProtocolType)sp)
                                {
                                    vAutoLogFunction.WriteLogFormat("Security Protocol: {0} is enabled.", sp.ToString());
                                }
                                else
                                {
                                    vAutoLogFunction.WriteLogFormat("Security Protocol: {0} not enabled.", sp.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                vAutoLogFunction.WriteErrorFormat(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { Logger }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void EnableTlsSupport(SecurityProtocolTypeCustom secprotocols)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { secprotocols }))
            {
                try
                {
                    if (ServicePointManager.SecurityProtocol != (SecurityProtocolType)secprotocols)
                    {
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)secprotocols;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { secprotocols }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        public static void EnableDefaultTlsSupport()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
            {
                try
                {
                    if (ServicePointManager.SecurityProtocol != (SecurityProtocolType)SecurityProtocolTypeCustom.SystemDefault)
                    {
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)SecurityProtocolTypeCustom.SystemDefault;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        private static void LogToNT(string strMsg)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { strMsg }))
            {
                try
                {
                    StackTrace t = new StackTrace();
                    StackFrame[] arrFrames = t.GetFrames();
                    string strCallPath = "Process Info:" + Environment.NewLine;
                    for (int i = arrFrames.Length - 1; i >= 0; i--)
                    {
                        if (!CommonLogger.FunctionFullName(arrFrames[i].GetMethod()).Contains("ctor"))
                            strCallPath += CommonLogger.FunctionFullName(arrFrames[i].GetMethod()) + Environment.NewLine;
                    }

                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application";
                        eventLog.WriteEntry(strMsg, EventLogEntryType.Information, 0, 0);
                        eventLog.WriteEntry(strCallPath, EventLogEntryType.Information, 0, 0);
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { strMsg }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
