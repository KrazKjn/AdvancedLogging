using AdvancedLogging.Constants;
using AdvancedLogging.Events;
using AdvancedLogging.Logging;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Models;
using AdvancedLogging.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using static AdvancedLogging.Constants.ConfigurationSetting;
using static AdvancedLogging.Utilities.LoggingUtils;
using System.Text;

namespace AdvancedLogging.Loggers
{
    /// <summary>
    /// Abstract logger class implementing the ICommonLogger interface.
    /// Provides common logging functionality and configuration management.
    /// </summary>
    public abstract class CommonLogger : ICommonLogger
    {
        private Dictionary<string, int> _debugLevels = new Dictionary<string, int>();
        /// <summary>
        /// Dictionary to store debug levels for different components or modules.
        /// </summary>
        public Dictionary<string, int> DebugLevels
        {
            get { return _debugLevels; }
            set { _debugLevels = value; }
        }

        private ConcurrentDictionary<string, string> _monitoredSettings = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// Concurrent dictionary to store monitored settings.
        /// </summary>
        public ConcurrentDictionary<string, string> MonitoredSettings
        {
            get { return _monitoredSettings; }
            set { _monitoredSettings = value; }
        }

        private ConcurrentDictionary<string, bool> _isPassword = null;
        /// <summary>
        /// Concurrent dictionary to store password settings.
        /// </summary>
        public ConcurrentDictionary<string, bool> IsPassword
        {
            get
            {
                if (_isPassword == null)
                {
                    _isPassword = new ConcurrentDictionary<string, bool>();
                    _isPassword.AddOrUpdate("ConnString", true, (ExistingKey, oldValue) => true);
                    _isPassword.AddOrUpdate("Password", true, (ExistingKey, oldValue) => true);
                    _isPassword.AddOrUpdate("Pwd", true, (ExistingKey, oldValue) => true);
                }
                return _isPassword;
            }
            set
            {
                _isPassword = value;
            }
        }

        #region Private fields

        /// <summary>
        /// Shared logging level for the logger.
        /// </summary>
        protected SharedLevel LogLevelShared = SharedLevel.Info;

        /// <summary>
        /// Notice message for dynamic logging.
        /// </summary>
        protected readonly string DynamicLoggingNotice = "<-- DYNAMIC LOGGING --> ";

        /// <summary>
        /// Path to the configuration file.
        /// </summary>
        private string _configFile = "";

        /// <summary>
        /// File system watcher to monitor changes in the configuration file.
        /// </summary>
        private FileSystemWatcher _configFileWatcher = null;

        /// <summary>
        /// Flag indicating whether monitoring is enabled.
        /// </summary>
        private bool _monitoring = false;

        /// <summary>
        /// Information about the configuration file.
        /// </summary>
        private FileInfo _configFileInfo = null;

        /// <summary>
        /// Log level for the logger.
        /// </summary>
        private int _logLevel = 0;

        /// <summary>
        /// XML document representing the configuration file.
        /// </summary>
        private XmlDocument _xmlConfig = null;

        /// <summary>
        /// Threshold in SECONDS for auto logging long running SQL commands.
        /// </summary>
        private double _autoLogSQLThreshold = 10.0;

        #endregion

        /// <summary>
        /// Event triggered when the configuration file changes.
        /// </summary>
        public event EventHandler<ConfigFileChangedEventArgs> ConfigFileChanged;

        /// <summary>
        /// Event triggered when a configuration setting changes.
        /// </summary>
        public event EventHandler<ConfigSettingChangedEventArgs> ConfigSettingChanged;

        /// <summary>
        /// Initializes a new instance of the CommonLogger class.
        /// </summary>
        public CommonLogger()
        {
            DynamicLoggingNotice = LoggingUtils.Logger?.MonitoredSettings.GetOrAdd(ConfigurationSetting.Log_DynamicLoggingNotice, DynamicLoggingNotice);
        }

        #region Implementation of ILoggerWrapper

        /// <summary>
        /// Theshold in SECONDS for Auto Logging long running SQL Commands.
        /// </summary>
        public double AutoLogSQLThreshold
        {
            get
            {
                return _autoLogSQLThreshold;
            }
            set
            {
                _autoLogSQLThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether monitoring is enabled.
        /// </summary>
        public bool Monitoring
        {
            get
            {
                return _configFileWatcher != null && _configFileWatcher.EnableRaisingEvents;
            }
            set
            {
                _monitoring = value;
                ToggleConfigMonitoring(value);
            }
        }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public int LogLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        private string _logFile = "";
        /// <summary>
        /// Gets or sets the log file.
        /// </summary>
        public string LogFile
        {
            get
            {
                return _logFile;
            }
            set
            {
                if (string.IsNullOrEmpty(_logFile))
                {
                    SetLogFile(value);
                }
                else
                {
                    if (!ReplaceLogFile(_logFile, value))
                        SetLogFile(value);
                }
                _logFile = value;
            }
        }

        /// <summary>
        /// Gets or sets the configuration file path.
        /// </summary>
        public string ConfigFile
        {
            get { return _configFile; }
            set
            {
                _configFile = value;
                ToggleConfigMonitoring(_monitoring);
                if (!Monitoring)
                    ReadConfigSettings();
            }
        }
        /// <summary>
        /// Gets or sets the shared logging level for the logger.
        /// </summary>
        public virtual SharedLevel Level
        {
            get
            {
                if (LogLevelShared == null)
                    LogLevelShared = SharedLevel.Info;
                return LogLevelShared;
            }
            set
            {
                LogLevelShared = value;
                ToggleLogging(LogLevelShared);
            }
        }

        /// <summary>
        /// Gets the contents of the configuration file.
        /// </summary>
        public string ConfigFileContents
        {
            get
            {
#if __IOS__
                XmlDocument xDoc = new XmlDocument();
                if (_xmlConfig == null)
                    return "";
                xDoc.LoadXml(_xmlConfig.OuterXml);
                return xDoc.ToString();
#else
                return XDocument.Parse(_xmlConfig.OuterXml).ToString();
#endif
            }
        }

        /// <summary>
        /// Gets the configuration file as an XML document.
        /// </summary>
        public XmlDocument ConfigFileXml
        {
            get
            {
                return _xmlConfig;
            }
        }

        /// <summary>
        /// Gets the configuration file as an XML document.
        /// </summary>
        public XmlDocument ConfigFileXmlDocument
        {
            get
            {
                return _xmlConfig;
            }
        }

        /// <summary>
        /// Event handler for changes in the configuration file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileInfo fi = new FileInfo(e.FullPath);
            if (_configFileInfo == null || fi.LastWriteTime != _configFileInfo.LastWriteTime)
            {
                if (ToLog(2))
                    DebugFormat("Watcher_Changed: {0}", e.FullPath);
                ReadConfigSettings();
                if (ConfigFileChanged != null)
                {
                    ConfigFileChangedEventArgs cfce = new ConfigFileChangedEventArgs(e.FullPath, e.ChangeType);
                    ConfigFileChanged(this, cfce);
                }
                _configFileInfo = fi;
            }
        }
        private void UpdateSetting<T>(string value, string newKey, string propertyName, Action<T> updateAction, Func<string, T, bool> compareFunc, Func<string, T> parseFunc)
        {
            if (!string.IsNullOrEmpty(value))
            {
                T parsedValue = parseFunc(value);
                T previousValue = (T)this.GetType().GetProperty(propertyName).GetValue(this, null);
                if (!compareFunc(value, previousValue))
                {
                    updateAction(parsedValue);
                    MonitoredSettings.AddOrUpdate(newKey, value, (k, oldValue) => value);
                    ConfigSettingChanged?.Invoke(this, new ConfigSettingChangedEventArgs(newKey, previousValue.ToString(), parsedValue.ToString(), true, false));
                }
            }
        }

        private int HandleIntegerSetting(string nodeValue, string settingName, int current, bool bExisting, bool bIsPassword)
        {
            int setting = current;

            if (!string.IsNullOrEmpty(nodeValue) && int.TryParse(nodeValue, out int iVal) && setting != iVal)
            {
                int previousValue = setting;
                setting = iVal;
                MonitoredSettings.AddOrUpdate(settingName, nodeValue, (key, oldValue) => nodeValue);

                ConfigSettingChanged?.Invoke(this, new ConfigSettingChangedEventArgs(
                    settingName, previousValue.ToString(), iVal.ToString(), bExisting, bIsPassword));
            }
            return setting;
        }

        private void HandleStringSetting(string nodeValue, string settingName, bool bExisting, bool bIsPassword)
        {
            if (!string.IsNullOrEmpty(nodeValue))
            {
                string current = MonitoredSettings.GetOrAdd(settingName, nodeValue);
                if (current != nodeValue)
                {
                    MonitoredSettings.AddOrUpdate(settingName, nodeValue, (key, oldValue) => nodeValue);

                    ConfigSettingChanged?.Invoke(this, new ConfigSettingChangedEventArgs(
                        settingName, current, nodeValue, bExisting, bIsPassword));
                }
            }
        }

        private void HandleDebugLevels(string nodeValue, bool bExisting, bool bIsPassword)
        {
            if (!string.IsNullOrEmpty(nodeValue))
            {
                string[] items = nodeValue.Split(';');
                MonitoredSettings.AddOrUpdate(Common_DebugLevels, nodeValue, (key, oldValue) => nodeValue);
                if (ConfigSettingChanged != null)
                {
                    string current = "";
                    foreach (string key in DebugLevels.Keys)
                    {
                        if (key != "*")
                        {
                            if (current.Length > 0)
                                current += ";";
                            current += key + ":" + DebugLevels[key].ToString();
                        }
                    }
                    if (current != nodeValue)
                    {
                        ConfigSettingChangedEventArgs csc = new ConfigSettingChangedEventArgs(Common_DebugLevels, current, nodeValue, bExisting, bIsPassword);
                        ConfigSettingChanged(this, csc);
                    }
                }
                DebugLevels.Clear();
                foreach (string item in items)
                {
                    string[] arrData = item.Split(':');
                    if (int.TryParse(arrData[1], out int i))
                    {
                        DebugLevels.Add(arrData[0], i);
                    }
                }
            }
        }

        private void HandleIgnoreFunctions(string nodeValue, bool bExisting, bool bIsPassword)
        {
            if (!string.IsNullOrEmpty(nodeValue))
            {
                string[] items = nodeValue.Split(';');
                MonitoredSettings.AddOrUpdate(Common_IgnoreFunctions, nodeValue, (key, oldValue) => nodeValue);
                string current = "";
                string @new = "";
                if (ConfigSettingChanged != null)
                {
                    foreach (string key in IgnoreLogging.Keys)
                    {
                        if (current.Length > 0)
                            current += ";";
                        current += key + ":" + IgnoreLogging[key].ToString();
                    }
                }
                foreach (string item in items)
                {
                    IgnoreLogging.AddOrUpdate(item, true, (key, oldValue) => true);
                }
                if (ConfigSettingChanged != null)
                {
                    foreach (string item in IgnoreLogging.Keys)
                    {
                        if (@new.Length > 0)
                            @new += ";";
                        @new += item + ":" + IgnoreLogging[item].ToString();
                    }
                    if (current != @new)
                    {
                        ConfigSettingChangedEventArgs csc = new ConfigSettingChangedEventArgs(Common_IgnoreFunctions, current, @new, bExisting, bIsPassword);
                        ConfigSettingChanged(this, csc);
                    }
                }
            }
        }

        private void UpdateSetting(string settingName, ref bool setting, bool newValue, string nodeValue, bool bExisting, bool bIsPassword)
        {
            if (setting != newValue)
            {
                bool previousValue = setting;
                setting = newValue;
                MonitoredSettings.AddOrUpdate(settingName, nodeValue, (key, oldValue) => nodeValue);

                ConfigSettingChanged?.Invoke(this, new ConfigSettingChangedEventArgs(
                    settingName, previousValue.ToString(), newValue.ToString(), bExisting, bIsPassword));
            }
        }

        private bool HandleLogSettings(string nodeValue, string settingName, bool current, bool bExisting, bool bIsPassword)
        {
            bool setting = current;

            if (!string.IsNullOrEmpty(nodeValue))
            {
                if (int.TryParse(nodeValue, out int iVal))
                {
                    UpdateSetting(settingName, ref setting, iVal != 0, nodeValue, bExisting, bIsPassword);
                }
                else if (bool.TryParse(nodeValue, out bool bVal))
                {
                    UpdateSetting(settingName, ref setting, bVal, nodeValue, bExisting, bIsPassword);
                }
                else
                {
                    setting = true;
                }
            }
            return setting;
        }

        private void ReadConfigSettings()
        {
            if (ToLog(2))
                DebugFormat("ReadConfigSettings: {0}", ConfigFile);
            _xmlConfig = new XmlDocument();
            XmlNodeList xmlAppSettings;
            bool bLoaded = false;
            int iCount = 0;
            while (!bLoaded && iCount < 5)
            {
                try
                {
                    _xmlConfig.Load(ConfigFile);
                    bLoaded = true;
                }
                catch (Exception ex)
                {
                    iCount++;
                    if (iCount >= 5)
                        ErrorFormat("Error Loading Config File {0}", ex.ToString());
                    else
                        System.Threading.Thread.Sleep(500);
                }
            }
            try
            {
                // reading LogFile key value to set LogPath.LogFile  and LogPath.LoggingThreshold properties
                xmlAppSettings = _xmlConfig.SelectNodes("/configuration/appSettings/add");
                foreach (XmlNode xmlSetting in xmlAppSettings)
                {
                    string nodeKey = xmlSetting.Attributes["key"].Value;
                    string nodeValue = xmlSetting.Attributes["value"].Value;

                    bool bExisting = MonitoredSettings.ContainsKey(nodeKey);
                    bool bIsPassword = false;

                    if (xmlSetting.Attributes["key"] != null && xmlSetting.Attributes["value"] != null)
                    {
                        if (IsUnEvaluated(nodeKey, nodeValue))
                        {
                            Error(string.Format("Template Value Found!  Item: [{0}] - Value: [{1}].", nodeKey, nodeValue));
                        }
                        else
                        {
                            string xmlData = "";

                            if (IsConnectionString(nodeKey, nodeValue))
                            {
#if !__IOS__
                                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(nodeValue)
                                {
                                    Password = "**************"
                                };
                                xmlData = xmlData.Replace(nodeValue, scsb.ToString());
                                bIsPassword = true;
#endif
                            }
                            if (nodeKey.ToLower() == "PassWord".ToLower())
                            {
                                xmlData = xmlData.Replace(xmlSetting.Attributes["value"].OuterXml, xmlSetting.Attributes["value"].OuterXml.Replace(xmlSetting.Attributes["value"].InnerText, "********"));
                                bIsPassword = true;
                            }
                            if (nodeKey.ToLower() == "RptServerDBPassWord".ToLower())
                            {
                                xmlData = xmlData.Replace(xmlSetting.Attributes["value"].OuterXml, xmlSetting.Attributes["value"].OuterXml.Replace(xmlSetting.Attributes["value"].InnerText, "********"));
                                bIsPassword = true;
                            }
                        }
                    }

                    switch (nodeKey.ToLower())
                    {
                        case Common_LogFile_Lower:
                            UpdateSetting(nodeValue, Common_LogFile, "LogFile", val => LogFile = val, (val, prev) => LogFile == val, val => val);
                            break;
                        case Common_LoggingThreshold_Lower:
                            UpdateSetting(nodeValue, Common_LoggingThreshold, "Level", val => Level = SharedLevel.GetLevelByName(nodeValue), (val, prev) => Level.ToString() == val, val => SharedLevel.GetLevelByName(val));
                            break;
                        case Common_AutoLogSQLThreshold_Lower:
                            UpdateSetting(nodeValue, Common_AutoLogSQLThreshold, "AutoLogSQLThreshold", val => AutoLogSQLThreshold = double.Parse(nodeValue), (val, prev) => AutoLogSQLThreshold == double.Parse(val), val => double.Parse(val));
                            break;
                        case Common_MaxFunctionTimeThreshold_Lower:
                            LoggingUtils.MaxFunctionTimeThreshold = HandleIntegerSetting(nodeValue, Common_MaxFunctionTimeThreshold, LoggingUtils.MaxFunctionTimeThreshold, bExisting, bIsPassword);
                            break;
                        case Common_LogLevel_Lower:
                            UpdateSetting(nodeValue, Common_LogLevel, "LogLevel", val => LogLevel = int.Parse(nodeValue), (val, prev) => LogLevel == int.Parse(nodeValue), val => int.Parse(nodeValue));
                            break;
                        case Common_AllowLoging_Lower:
                            UpdateSetting(nodeValue, Common_AllowLoging, "AllowLogging", val => AllowLogging = bool.Parse(nodeValue), (val, prev) => AllowLogging == bool.Parse(val), val => bool.Parse(val));
                            break;
                        case Common_IISSoapLogging_Lower:
                            HandleStringSetting(nodeValue, Common_IISSoapLogging, bExisting, bIsPassword);
                            break;
                        case Common_DebugLevels_Lower:
                            HandleDebugLevels(nodeValue, bExisting, bIsPassword);
                            break;
                        case Common_EnableDebugCode_Lower:
                            UpdateSetting(nodeValue, Common_EnableDebugCode, "EnableDebugCode", val => EnableDebugCode = bool.Parse(nodeValue), (val, prev) => EnableDebugCode == bool.Parse(val), val => bool.Parse(val));
                            break;
                        case Common_IgnoreFunctions_Lower:
                            HandleIgnoreFunctions(nodeValue, bExisting, bIsPassword);
                            break;
                        case Log_ToConsole_Lower:
                            ApplicationSettings.LogToConsole = HandleLogSettings(nodeValue, Log_ToConsole, ApplicationSettings.LogToConsole, bExisting, bIsPassword);
                            break;
                        case Log_ToDebugWindow_Lower:
                            ApplicationSettings.LogToDebugWindow = HandleLogSettings(nodeValue, Log_ToDebugWindow, ApplicationSettings.LogToDebugWindow, bExisting, bIsPassword);
                            break;
                        case Common_AutoTimeoutIncrementMsHttp_Lower:
                            ApplicationSettings.AutoTimeoutIncrementMsHttp = HandleIntegerSetting(nodeValue, Common_AutoTimeoutIncrementMsHttp, ApplicationSettings.AutoTimeoutIncrementMsHttp, bExisting, bIsPassword);
                            break;
                        case Common_AutoTimeoutIncrementSecondsSql_Lower:
                            ApplicationSettings.AutoTimeoutIncrementSecondsSql = HandleIntegerSetting(nodeValue, Common_AutoTimeoutIncrementSecondsSql, ApplicationSettings.AutoTimeoutIncrementSecondsSql, bExisting, bIsPassword);
                            break;
                        case Log_FunctionHeaderMethod_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod] = HandleIntegerSetting(nodeValue, Log_FunctionHeaderMethod, DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod], bExisting, bIsPassword);
                            break;
                        case Log_FunctionHeaderConstructor_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor] = HandleIntegerSetting(nodeValue, Log_FunctionHeaderConstructor, DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderConstructor], bExisting, bIsPassword);
                            break;
                        case Log_ComplexParameterValues_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues] = HandleIntegerSetting(nodeValue, Log_ComplexParameterValues, DebugPrintLevel[ConfigurationSetting.Log_ComplexParameterValues], bExisting, bIsPassword);
                            break;
                        case Log_SqlCommand_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_SqlCommand] = HandleIntegerSetting(nodeValue, Log_SqlCommand, DebugPrintLevel[ConfigurationSetting.Log_SqlCommand], bExisting, bIsPassword);
                            break;
                        case Log_SqlParameters_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_SqlParameters] = HandleIntegerSetting(nodeValue, Log_SqlParameters, DebugPrintLevel[ConfigurationSetting.Log_SqlParameters], bExisting, bIsPassword);
                            break;
                        case Log_SqlCommandResults_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_SqlCommandResults] = HandleIntegerSetting(nodeValue, Log_SqlCommandResults, DebugPrintLevel[ConfigurationSetting.Log_SqlCommandResults], bExisting, bIsPassword);
                            break;
                        case Log_MemberTypeInformation_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_MemberTypeInformation] = HandleIntegerSetting(nodeValue, Log_MemberTypeInformation, DebugPrintLevel[ConfigurationSetting.Log_MemberTypeInformation], bExisting, bIsPassword);
                            break;
                        case Log_DumpComplexParameterValues_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues] = HandleIntegerSetting(nodeValue, Log_DumpComplexParameterValues, DebugPrintLevel[ConfigurationSetting.Log_DumpComplexParameterValues], bExisting, bIsPassword);
                            break;
                        case Log_DebugDumpSQL_Lower:
                            DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL] = HandleIntegerSetting(nodeValue, Log_DebugDumpSQL, DebugPrintLevel[ConfigurationSetting.Log_DebugDumpSQL], bExisting, bIsPassword);
                            break;
                        default:
#if DEBUG
                            if (nodeKey.ToLower().StartsWith("autolog:"))
                            {
                                WarnFormat("Setting Not Handled! [{0}]", nodeKey);
                            }
#endif
                            if (MonitoredSettings.ContainsKey(nodeKey))
                            {
                                if (!string.IsNullOrEmpty(MonitoredSettings[nodeKey]))
                                {
                                    if (MonitoredSettings[nodeKey] != nodeValue)
                                    {
                                        string MonitoredSettingPrevious = MonitoredSettings[nodeKey];
                                        MonitoredSettings.AddOrUpdate(nodeKey, nodeValue, (key, oldValue) => nodeValue);
                                        if (ConfigSettingChanged != null)
                                        {
                                            ConfigSettingChangedEventArgs csc = new ConfigSettingChangedEventArgs(nodeKey, MonitoredSettingPrevious, nodeValue, bExisting, bIsPassword);
                                            ConfigSettingChanged(this, csc);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                ToggleLogging(LogLevelShared);
                if (DebugLevels.ContainsKey("*"))
                    DebugLevels["*"] = LogLevel;
                else
                    DebugLevels.Add("*", LogLevel);
            }
            catch (Exception ex)
            {
                ErrorFormat("Error Loading Config File {0}", ex.ToString());
            }
        }
        #endregion

        public bool IsAllEnabled => (Level == SharedLevel.All); // int.MinValue

        public bool IsDebugEnabled => Level <= SharedLevel.Debug || (Level == SharedLevel.All); // 3000

        public bool IsInfoEnabled => Level <= SharedLevel.Info || (Level == SharedLevel.All); // 4000

        public bool IsWarnEnabled => Level <= SharedLevel.Warn || (Level == SharedLevel.All); // 6000

        public bool IsErrorEnabled => Level <= SharedLevel.Error || (Level == SharedLevel.All); // 7000

        public bool IsFatalEnabled => Level <= SharedLevel.Fatal || (Level == SharedLevel.All); // 11000

        public abstract bool IsRemoting { get; }
        public abstract bool IsLoggingToConsole { get; }
        public abstract bool IsLoggingToDebugWindow { get; }

        #region Implementation of ILog

        #region Implementation of Debug
        public abstract void Debug(object message);
        public abstract void Debug(object message, Exception exception);
        public abstract void DebugFormat(string format, params object[] args);
        public abstract void DebugFormat(string format, object arg0);
        public abstract void DebugFormat(string format, object arg0, object arg1);
        public abstract void DebugFormat(string format, object arg0, object arg1, object arg2);
        public abstract void DebugFormat(IFormatProvider provider, string format, params object[] args);
        // --------------------------------------------------------------------------------------

        #region Debug Helpers
        /// <summary>
        /// Logs a debug message with a prefix and an exception.
        /// </summary>
        /// <param name="logPrefix">The prefix for the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void DebugPrefix(string logPrefix, object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Debug(logPrefix + string.Format("{0}", message));
            else
                Debug(exception, logPrefix + string.Format("{0}", message));
#else
            Debug(logPrefix + message, exception);
#endif
        }

        /// <summary>
        /// Logs a debug message with a prefix, function name, and an exception.
        /// </summary>
        /// <param name="logPrefix">The prefix for the log message.</param>
        /// <param name="functionName">The function name to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void DebugPrefix(string logPrefix, string functionName, object message, Exception exception)
        {
#if __IOS__
            if (exception == null)
                Debug(string.Format(Constants.LOGGING_FORMAT, logPrefix, functionName, message));
            else
                Debug(exception, string.Format(Constants.LOGGING_FORMAT, logPrefix, functionName, message));
#else
            Debug(string.Format(LogFormats.LOGGING_FORMAT, logPrefix, functionName, message), exception);
#endif
        }
        #endregion Debug Helpers

        /// <summary>
        /// Logs a debug message with a specified level.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        public bool Debug(Int32 level, object message)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(string.Format("{0}", message));
#else
                        DebugFormat("{0}", message);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            DebugFormat(LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(string.Format("{0}", message));
#else
                            DebugFormat("{0}", message);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Logs a debug message with a specified level and prefix.
        /// </summary>
        /// <param name="level">The debug level.</param>
        /// <param name="logPrefix">The prefix for the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>True if the message was logged, otherwise false.</returns>
        public bool DebugPrefix(Int32 level, string logPrefix, object message)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(logPrefix + Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format(logPrefix + Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(logPrefix + LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(logPrefix + LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(string.Format(logPrefix + "{0}", message));
#else
                        DebugFormat(logPrefix + "{0}", message);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format(logPrefix + Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            DebugFormat(logPrefix + LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(string.Format(logPrefix + "{0}", message));
#else
                            DebugFormat(logPrefix + "{0}", message);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool Debug(Int32 level, object message, Exception exception)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(exception, string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    Debug(string.Format(LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(exception, string.Format("{0}", message));
#else
                        Debug(string.Format("{0}", message), exception);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(exception, string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            Debug(string.Format(LogFormats.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(exception, string.Format("{0}", message));
#else
                            Debug(string.Format("{0}", message), exception);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }
        public bool DebugPrefix(Int32 level, string logPrefix, object message, Exception exception)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(logPrefix + Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(exception, string.Format(logPrefix + Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(logPrefix + LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    Debug(string.Format("{0}{1}{2}: {3}", logPrefix, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(exception, string.Format(logPrefix + "{0}", message));
#else
                        Debug(string.Format("{0}{1}", logPrefix, message), exception);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(exception, string.Format(logPrefix + Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            Debug(string.Format("{0}{1}{2}: {3}", logPrefix, detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(exception, string.Format(logPrefix + "{0}", message));
#else
                            Debug(string.Format("{0}{1}", logPrefix, message), exception);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string format, params object[] args)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), args));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(string.Format(format, args));
#else
                        DebugFormat(format, args);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), args));
#else
                            DebugFormat(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(string.Format(format, args));
#else
                            DebugFormat(format, args);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string format, object arg0)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), arg0));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
                        DebugFormat(format, arg0);
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), arg0));
#else
                            DebugFormat(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(string.Format(format, arg0));
#else
                            DebugFormat(format, arg0);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string format, object arg0, object arg1)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, FunctionFullName(GetParentFunction()), detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), arg0, arg1));
#else
                DebugFormat(string.Format("{0}{1}: ", FunctionFullName(GetParentFunction()), detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0, arg1);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string format, object arg0, object arg1, object arg2)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(string.Format(string.Format(Constants.LOGGING_FORMAT, FunctionFullName(GetParentFunction()), detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", format), arg0, arg1, arg2));
#else
                DebugFormat(string.Format("{0}{1}: ", FunctionFullName(GetParentFunction()), detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0, arg1, arg2);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, IFormatProvider provider, string format, params object[] args)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + new SystemStringFormat(provider, format, args));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(provider, string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(new SystemStringFormat(provider, format, args).ToString());
#else
                        DebugFormat(provider, format, args);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + new SystemStringFormat(provider, format, args));
#else
                            DebugFormat(provider, string.Format("{0}{1}: ", detectedFunction, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(new SystemStringFormat(provider, format, args).ToString());
#else
                            DebugFormat(provider, format, args);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }
        // --------------------------------------------------------------------------------------
        public bool Debug(Int32 level, string functionName, object message, Exception exception)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                //if (detectedCriteria.Length > 0)
                //    DebugFormat(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                //Debug(string.Format(Constants.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message), exception);
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(exception, string.Format(Constants.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    Debug(string.Format(LogFormats.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(exception, string.Format("{0}: {1}", functionName, message));
#else
                        Debug(string.Format("{0}: {1}", functionName, message), exception);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(exception, string.Format(Constants.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            Debug(string.Format(LogFormats.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(exception, string.Format("{0}: {1}", functionName, message));
#else
                            Debug(string.Format("{0}: {1}", functionName, message), exception);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }
        public bool DebugPrefix(Int32 level, string logPrefix, string functionName, object message, Exception exception)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                //if (detectedCriteria.Length > 0)
                //    DebugFormat(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                //Debug(string.Format(Constants.LOGGING_FORMAT, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message), exception);
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(exception, string.Format("{0}{1}{2}: {3}", logPrefix, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                    DebugFormat(logPrefix + LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    Debug(string.Format("{0}{1}{2}: {3}", logPrefix, functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(exception, string.Format(Constants.LOGGING_FORMAT, logPrefix, functionName, message));
#else
                        Debug(string.Format(LogFormats.LOGGING_FORMAT, logPrefix, functionName, message), exception);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(exception, string.Format("{0}{1}{2}: {3}", logPrefix, functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "", message));
#else
                            Debug(string.Format("{0}{1}{2}: {3}", logPrefix, functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "", message), exception);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(exception, string.Format(Constants.LOGGING_FORMAT, logPrefix, functionName, message));
#else
                            Debug(string.Format(LogFormats.LOGGING_FORMAT, logPrefix, functionName, message), exception);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string functionName, string format, params object[] args)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                //if (detectedCriteria.Length > 0)
                //    DebugFormat(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                //DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format, args);

                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
                    Debug(string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), args);
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
                    DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                }
                else
                {
                    if (ToLog(level))
                    {
#if __IOS__
                        Debug(string.Format(string.Format("{0}{1}: ", functionName, format), args));
#else
                        DebugFormat(string.Format("{0}: ", functionName) + format, args);
#endif
                    }
                    else
                    {
                        if (detectedFunction.Length == 0)
                            detectedFunction = FunctionFullName(GetParentFunction());
                        if (level >= DebugPrintLevel[ConfigurationSetting.Log_FunctionHeaderMethod])
                        {
#if __IOS__
                            Debug(string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), args);
#else
                            DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                        }
                        else
                        {
#if __IOS__
                            Debug(string.Format(string.Format("{0}{1}: ", functionName, format), args));
#else
                            DebugFormat(string.Format("{0}: ", functionName) + format, args);
#endif
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string functionName, string format, object arg0)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), arg0);
#else
                DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormatPrefix(Int32 level, string logPrefix, string functionName, string format, object arg0)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(logPrefix + string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(logPrefix + LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(logPrefix + string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), arg0);
#else
                DebugFormat(logPrefix + string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string functionName, string format, object arg0, object arg1)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), arg0, arg1);
#else
                DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0, arg1);
#endif
                return true;
            }
            return false;
        }
        public bool DebugFormatPrefix(Int32 level, string logPrefix, string functionName, string format, object arg0, object arg1)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(logPrefix + string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(logPrefix + LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(logPrefix + string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), arg0, arg1);
#else
                DebugFormat(logPrefix + string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0, arg1);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string functionName, string format, object arg0, object arg1, object arg2)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(string.Format(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format), arg0, arg1, arg2);
#else
                DebugFormat(string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, arg0, arg1, arg2);
#endif
                return true;
            }
            return false;
        }

        public bool DebugFormat(Int32 level, string functionName, IFormatProvider provider, string format, params object[] args)
        {
            if (ToLog(level, out string detectedCriteria, out string detectedFunction, out int detectedLevel))
            {
                if (detectedCriteria.Length > 0)
                {
#if __IOS__
                    Debug(string.Format(Constants.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction));
#else
                    DebugFormat(LogFormats.DEBUG_LOGGING_FORMAT, detectedCriteria, DynamicLoggingNotice, detectedFunction);
#endif
                }
#if __IOS__
                Debug(new SystemStringFormat(provider, string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? Constants.DETAILED_LOGGING : "") + format, args).ToString());
#else
                DebugFormat(provider, string.Format("{0}{1}: ", functionName, detectedCriteria.Length > 0 ? LogFormats.DETAILED_LOGGING : "") + format, args);
#endif
                return true;
            }
            return false;
        }
        // --------------------------------------------------------------------------------------
        #endregion Implementation of Debug

        #region Implementation of Base Functions
        public abstract void Error(object message);
        public abstract void Error(object message, Exception exception);
        public abstract void ErrorFormat(string format, params object[] args);
        public abstract void ErrorFormat(string format, object arg0);
        public abstract void ErrorFormat(string format, object arg0, object arg1);
        public abstract void ErrorFormat(string format, object arg0, object arg1, object arg2);
        public abstract void ErrorFormat(IFormatProvider provider, string format, params object[] args);
        public abstract void Fatal(object message);
        public abstract void Fatal(object message, Exception exception);
        public abstract void FatalFormat(string format, params object[] args);
        public abstract void FatalFormat(string format, object arg0);
        public abstract void FatalFormat(string format, object arg0, object arg1);
        public abstract void FatalFormat(string format, object arg0, object arg1, object arg2);
        public abstract void FatalFormat(IFormatProvider provider, string format, params object[] args);
        public abstract void Info(object message);
        public abstract void Info(object message, Exception exception);
        public abstract void InfoFormat(string format, params object[] args);
        public abstract void InfoFormat(string format, object arg0);
        public abstract void InfoFormat(string format, object arg0, object arg1);
        public abstract void InfoFormat(string format, object arg0, object arg1, object arg2);
        public abstract void InfoFormat(IFormatProvider provider, string format, params object[] args);
        public abstract void Warn(object message);
        public abstract void Warn(object message, Exception exception);
        public abstract void WarnFormat(string format, params object[] args);
        public abstract void WarnFormat(string format, object arg0);
        public abstract void WarnFormat(string format, object arg0, object arg1);
        public abstract void WarnFormat(string format, object arg0, object arg1, object arg2);
        public abstract void WarnFormat(IFormatProvider provider, string format, params object[] args);
        #endregion Implementation of Base Functions

        #endregion Implementation of ILog
        /// <summary>
        /// Constructs the full name of a method, including its declaring type and method name.
        /// </summary>
        /// <param name="method">
        /// The <see cref="MethodBase"/> instance representing the method.
        /// </param>
        /// <returns>
        /// A string containing the full name of the method in the format "Namespace.ClassName.MethodName".
        /// </returns>
        public static string FunctionFullName(MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name;
        }

        /// <summary>
        /// Constructs a full path representation of the functions in the given stack frames.
        /// Combines type and method details for all stack frames into a single string.
        /// </summary>
        /// <param name="frames">
        /// An array of <see cref="StackFrame"/> objects representing the current call stack.
        /// </param>
        /// <returns>
        /// A string containing the full path of functions in the stack frames, 
        /// formatted as "Namespace.ClassName.MethodName" for each frame.
        /// </returns>
        public static string FunctionFullPath(StackFrame[] frames)
        {
            var fullNameBuilder = new StringBuilder();

            // Reverse frames in-place
            Array.Reverse(frames);
            foreach (var vItem in frames)
            {
                try
                {
                    var method = vItem.GetMethod();
                    if (method == null) continue;

                    // Skip processing for certain namespaces
                    var declaringTypeFullName = method.DeclaringType?.FullName;
                    if (declaringTypeFullName != null && (declaringTypeFullName.StartsWith("System.") ||
                                                          declaringTypeFullName.StartsWith("Microsoft.") ||
                                                          declaringTypeFullName.StartsWith("ASP.")))
                    {
                        continue;
                    }

                    // Get function name or constructor name
                    var functionName = method.IsConstructor ? method.DeclaringType?.Name : method.Name;

                    // Append declaring type and function name to the path
                    if (fullNameBuilder.Length > 0)
                    {
                        fullNameBuilder.Append(".");
                    }
                    else if (declaringTypeFullName != null)
                    {
                        fullNameBuilder.Append(declaringTypeFullName).Append(".");
                    }
                    fullNameBuilder.Append(functionName);
                }
                catch (Exception ex)
                {
                    // Log the exception if needed
                    if (ShouldLogToDebugWindow())
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            return fullNameBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the parent function (caller) of the current method from the call stack.
        /// </summary>
        /// <returns>
        /// A <see cref="MethodBase"/> object representing the parent function,
        /// or null if the parent function cannot be determined.
        public static MethodBase GetParentFunction()
        {
            StackTrace st = new StackTrace(2);
            StackFrame frame = st.GetFrame(0);
            return frame?.GetMethod();
        }

        /// <summary>
        /// Checks if the specified function name exists in the current call stack.
        /// Utilizes stack frames to evaluate if the function is part of the code path.
        /// </summary>
        /// <param name="functionName">
        /// The name of the function to search for in the call stack.
        /// </param>
        /// <param name="detectedFunction">
        /// Outputs the detected function name in the stack if a match is found.
        /// </param>
        /// <returns>
        /// Returns true if the function name exists in the current call stack; otherwise, false.
        public bool InCodePathOf(string functionName, out string detectedFunction)
        {
            return InCodePathOf(functionName, new StackTrace().GetFrames(), out detectedFunction);
        }

        /// <summary>
        /// Checks if the given function name matches any function in the provided stack frames.
        /// Supports exact matching, wildcard matching (prefix, suffix, and substring).
        /// </summary>
        /// <param name="functionName">
        /// The name of the function to match. Can include wildcards (*).
        /// </param>
        /// <param name="frames">
        /// The stack frames representing the current call stack.
        /// </param>
        /// <param name="detectedFunction">
        /// Outputs the detected function name in the stack if a match is found.
        /// </param>
        /// <returns>
        /// Returns true if the function name matches a function in the stack frames; otherwise, false.
        /// </returns>
        public bool InCodePathOf(string functionName, StackFrame[] frames, out string detectedFunction)
        {
            // Normalize inputs to ensure case-insensitivity
            functionName = functionName.ToLower();
            detectedFunction = FullPath(frames).ToLower();

            // Remove asterisks for wildcard matching
            string trimmedFunctionName = functionName.Trim('*');

            // Handle different wildcard cases
            if (functionName.StartsWith("*") && functionName.EndsWith("*"))
            {
                // Match substring
                if (detectedFunction.Contains(trimmedFunctionName))
                    return true;
            }
            else if (functionName.StartsWith("*"))
            {
                // Match suffix
                if (detectedFunction.EndsWith(trimmedFunctionName))
                    return true;
            }
            else if (functionName.EndsWith("*"))
            {
                // Match prefix
                if (detectedFunction.StartsWith(trimmedFunctionName))
                    return true;
            }
            else
            {
                // Exact match or partial match if no wildcards
                if (detectedFunction == functionName || detectedFunction.Contains(functionName))
                    return true;
            }

            // No match found
            detectedFunction = "";
            return false;
        }

        /// <summary>
        /// Constructs a full path representation of the functions in the given stack frames.
        /// Reverses the order of the stack frames and processes each frame to generate a
        /// string representing the full code path, excluding certain system and framework methods.
        /// </summary>
        /// <param name="frames">
        /// An array of <see cref="StackFrame"/> objects representing the current call stack.
        /// </param>
        /// <returns>
        /// A string containing the full path of functions, formatted as "Namespace.ClassName.MethodName",
        /// with methods from system or framework namespaces (e.g., System, Microsoft, ASP) excluded.
        public string FullPath(StackFrame[] frames)
        {
            string fullName = "";

            // Reverse the stack frames for processing
            Array.Reverse(frames);
            foreach (var frame in frames)
            {
                try
                {
                    MethodBase method = frame.GetMethod();
                    if (method == null) continue;

                    // Determine whether to process this method based on declaring type
                    bool processMethod = method.DeclaringType != null &&
                                         !(method.DeclaringType.FullName.StartsWith("System.") ||
                                           method.DeclaringType.FullName.StartsWith("Microsoft.") ||
                                           method.DeclaringType.FullName.StartsWith("ASP."));

                    if (!processMethod) continue;

                    // Get the function name
                    string functionName = method.IsConstructor && method.DeclaringType != null
                        ? method.DeclaringType.Name
                        : method.Name;

                    // Build the full name
                    if (fullName.Length > 0)
                        fullName += ".";
                    else if (method.DeclaringType != null)
                        fullName = method.DeclaringType.FullName + ".";

                    fullName += functionName;
                }
                catch (Exception ex)
                {
                    // Log exceptions if required
                    if (ShouldLogToDebugWindow())
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            return fullName;
        }

        /// <summary>
        /// Determines whether debug logging is enabled in the current context.
        /// </summary>
        /// <returns>
        /// Returns true if debug logging is enabled; otherwise, false.
        /// </returns>
        public bool LoggingDebug()
        {
            return IsDebugEnabled;
        }

        /// <summary>
        /// Checks for Proper Log Level
        /// </summary>
        /// <param name="level"></param>
        /// <returns>
        /// Returns true if logging is enabled enabled for the level; otherwise, false.
        /// </returns>
        public bool ToLog(int level)
        {
            try
            {
                if (IsDebugEnabled)
                {
                    return (LogLevel >= level);
                }
            }
            catch (Exception ex)
            {
                if (ShouldLogToDebugWindow())
                    System.Diagnostics.Debug.WriteLine("ToLog: Error: " + ex.ToString());
                Error("ToLog: Error: " + ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// Checks for Proper Log Level or Function Log Level
        /// </summary>
        /// <param name="level"></param>
        /// <param name="detectedCriteria"></param>
        /// <param name="detectedFunction"></param>
        /// <returns></returns>
        public bool ToLog(int level, out string detectedCriteria, out string detectedFunction, out int detectedLevel)
        {
            detectedCriteria = "";
            detectedFunction = "";
            detectedLevel = 0;

            try
            {
                if (IsDebugEnabled)
                {
                    int iValue = 0;

                    if (DebugLevels.Count > 0)
                    {
                        System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                        StackFrame[] arrFrames = t.GetFrames();
                        foreach (var vItem in DebugLevels)
                        {
                            if (vItem.Key == "*")
                                continue;
                            if (InCodePathOf(vItem.Key, arrFrames, out detectedFunction))
                            {
                                detectedLevel = vItem.Value;
                                iValue = vItem.Value;
                                detectedCriteria = vItem.Key + ":" + iValue.ToString();
                                break;
                            }
                        }
                        if (iValue == 0)
                        {
                            if (DebugLevels.ContainsKey("*"))
                                iValue = DebugLevels["*"];
                        }
                    }
                    if (iValue == 0)
                    {
                        return (LogLevel >= level);
                    }
                    if (iValue < 0)
                    {
                        return false;
                    }
                    if (LogLevel >= iValue)
                    {
                        detectedCriteria = "";
                        detectedFunction = "";
                        return (LogLevel >= level);
                    }
                    return (iValue >= level);
                }
            }
            catch (Exception ex)
            {
                if (ShouldLogToDebugWindow())
                    System.Diagnostics.Debug.WriteLine("ToLog: Error: " + ex.ToString());
                Error("ToLog: Error: " + ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// Toggle all appenders to this specified level 
        /// </summary>
        /// <param name="level"></param>
        public abstract void ToggleLogging(SharedLevel level);

        /// <summary>
        /// Toggle all appenders to this specified level
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public abstract SharedLevel ToggleLogging(string logLevel);

        public abstract SharedLevel LoggingLevel();

        public abstract void SetLogFile(string fileName);

        public abstract string GetLogFile();

        public abstract bool ReplaceLogFile(string currentFileName, string fileName);

        private void ToggleConfigMonitoring(bool enable)
        {
#if !__IOS__
            if (_configFileWatcher != null)
            {
                _configFileWatcher.EnableRaisingEvents = enable;
                if (!enable)
                    _configFileWatcher = null;
            }

            if (!enable)
                return;

            FileInfo fi = null;
            if (_configFileWatcher == null)
            {
                if (string.IsNullOrEmpty(_configFile))
                    throw new FileNotFoundException("ConfigFile not set before Enabling ConfigMonitoring.");

                // Create a new FileSystemWatcher and set its properties.
                _configFileWatcher = new FileSystemWatcher();
                if (_configFileWatcher != null)
                {
                    fi = new FileInfo(_configFile);
                    _configFileWatcher.Path = fi.DirectoryName;

                    // Watch for changes in LastAccess and LastWrite times, and
                    // the renaming of files or directories.
                    _configFileWatcher.NotifyFilter = NotifyFilters.CreationTime
                                            | NotifyFilters.LastWrite
                                            | NotifyFilters.FileName
                                            | NotifyFilters.DirectoryName
                                            | NotifyFilters.Size;

                    // Only watch text files.
                    _configFileWatcher.Filter = fi.Name;

                    // Add event handlers.
                    _configFileWatcher.Changed += Watcher_Changed;
                    _configFileWatcher.Created += Watcher_Changed;
                    _configFileWatcher.Deleted += Watcher_Changed;
                    _configFileWatcher.Renamed += Watcher_Changed;

                    // Begin watching.
                    _configFileWatcher.EnableRaisingEvents = true;
                }
            }
            if (fi != null)
                ReadConfigSettings();
#endif
        }
        /// <summary>
        /// Checks for template configuration values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="checkForEmpty"></param>
        /// <returns></returns>
        public bool IsUnEvaluated(string name, string value, bool checkForEmpty = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value, checkForEmpty }, bSuppressFunctionDeclaration: true))
            {
                if (string.IsNullOrEmpty(value))
                {
                    vAutoLogFunction.WriteWarnFormat("Application Setting [{0}] is an Empty/Blank value.", name);
                    if (checkForEmpty)
                        return true;
                }
                value = value.Trim();
                if (value.StartsWith("%") && value.EndsWith("%"))
                    return true;
                else if (value.StartsWith("#{") && value.EndsWith("}"))
                    return true;
                return false;
            }
        }
        /// <summary>
        /// Checks for a valid Connection String.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsConnectionString(string name, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }, bSuppressFunctionDeclaration: true))
            {
                if (IsUnEvaluated(name, value))
                    return false;
                if (value.Contains(';') && value.Contains('='))
                {
                    try
                    {
                        SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(value);
                        return true;
                    }
                    catch
                    {
                        vAutoLogFunction.WriteErrorFormat("Value [{0}] cannot be converted to a SqlConnectionStringBuilder.", value);
                        return false;
                    }
                }
                return false;
            }
        }
    }
}