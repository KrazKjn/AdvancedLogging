using AdvancedLogging.Events;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Utilities;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

namespace AdvancedLogging.Models
{
    public class ApplicationSettings
    {
        public class PropertyChangedEventArgs : EventArgs
        {
            public string Property { get; private set; }
            public object Value { get; private set; }
            public PropertyChangedEventArgs(string _Property, object _Value)
            {
                Property = _Property;
                Value = _Value;
            }
        }
#if __IOS__
        public static event EventHandler<PropertyChangedEventArgs>? PropertyChanged;

        private static ILoggerUtility? _loggerUtility;
        public static ILoggerUtility? LoggerUtility
#else
        public static event EventHandler<PropertyChangedEventArgs> PropertyChanged;

        private static ILoggerUtility _loggerUtility;
        public static ILoggerUtility LoggerUtility
#endif
        {
            get { return _loggerUtility; }
            set
            {
                _loggerUtility = value;
                LoggingUtils.LoggerUtility = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("LoggerUtility", _loggerUtility));
            }
        }

        private static ICommonLogger _log;

        public static ICommonLogger Logger
        {
            get { return _log; }
            set
            {
                _log = value;
                LoggingUtils.Logger = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Logger", value));
            }
        }

        private static ConcurrentDictionary<string, object> m_dicData = new ConcurrentDictionary<string, object>();

        public static ConcurrentDictionary<string, object> Data
        {
            get { return m_dicData; }
            set
            {
                m_dicData = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Data", Data));
            }
        }

        private static bool m_bLogToDebugWindow = false;

        public static bool LogToDebugWindow
        {
            get { return m_bLogToDebugWindow; }
            set
            {
                m_bLogToDebugWindow = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("LogToDebugWindow", value));
            }
        }

        private static bool m_bLogToConsole = false;

        public static bool LogToConsole
        {
            get { return m_bLogToConsole; }
            set
            {
                m_bLogToConsole = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("LogToConsole", value));
            }
        }

        private string m_strConfigFile = "";
        public string ConfigFile
        {
            get { return m_strConfigFile; }
            set
            {
                m_strConfigFile = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("ConfigFile", value));
                ReadConfigSettings();
            }
        }

#if __IOS__
        private FileInfo? m_fiConfigFile = null;
        private FileSystemWatcher? m_fswConfigFile = null;
#else
        private FileInfo m_fiConfigFile = null;
        private FileSystemWatcher m_fswConfigFile = null;
#endif
        public bool Monitoring
        {
            get
            {
                return m_fswConfigFile != null && m_fswConfigFile.EnableRaisingEvents;
            }
            set
            {
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Monitoring", value));
                ToggleConfigMonitoring(value);
            }
        }

        public static int MaxAutoRetriesSql = 3;
        public static int AutoRetrySleepMsSql = 250;
        public static int AutoTimeoutIncrementSecondsSql = 0;
        public static int MaxAutoRetriesHttp = 3;
        public static int AutoRetrySleepMsHttp = 250;
        public static int AutoTimeoutIncrementMsHttp = 0;
        public static bool IsRunning = true;
        public static System.Security.Authentication.SslProtocols SslProtocols = System.Security.Authentication.SslProtocols.None;
        private static ConcurrentDictionary<string, Guid> m_bagAutoLogActivity = new ConcurrentDictionary<string, Guid>();
        public static ConcurrentDictionary<string, Guid> AutoLogActivity
        {
            get { return m_bagAutoLogActivity; }
            set
            {
                m_bagAutoLogActivity = value;
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs("AutoLogActivity", AutoLogActivity));
            }
        }

#if __IOS__
        public event EventHandler<ConfigFileChangedEventArgs>? ConfigFileChanged;
        public event EventHandler<ConfigSettingChangedEventArgs>? ConfigSettingChanged;
#else
        public event EventHandler<ConfigFileChangedEventArgs> ConfigFileChanged;
        public event EventHandler<ConfigSettingChangedEventArgs> ConfigSettingChanged;
#endif
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileInfo fi = new FileInfo(e.FullPath);
            if (m_fiConfigFile == null || fi.LastWriteTime != m_fiConfigFile.LastWriteTime)
            {
                _log?.DebugFormat(2, "Watcher_Changed: {0}", e.FullPath);
                ReadConfigSettings();
                if (ConfigFileChanged != null)
                {
                    ConfigFileChangedEventArgs cfce = new ConfigFileChangedEventArgs(e.FullPath, e.ChangeType);
                    ConfigFileChanged(this, cfce);
                }
                m_fiConfigFile = fi;
            }
        }

        private void ToggleConfigMonitoring(bool bEnable)
        {
            if (m_fswConfigFile != null)
            {
                m_fswConfigFile.EnableRaisingEvents = bEnable;
                if (!bEnable)
                    m_fswConfigFile = null;
            }
            if (bEnable)
            {
                // Create a new FileSystemWatcher and set its properties.
                m_fswConfigFile = new FileSystemWatcher();
                if (m_fswConfigFile != null)
                {
                    FileInfo fi = new FileInfo(m_strConfigFile);
                    m_fswConfigFile.Path = fi.DirectoryName;

                    // Watch for changes in LastAccess and LastWrite times, and
                    // the renaming of files or directories.
                    m_fswConfigFile.NotifyFilter = NotifyFilters.CreationTime
                                            | NotifyFilters.LastWrite
                                            | NotifyFilters.FileName
                                            | NotifyFilters.DirectoryName;

                    // Only watch text files.
                    m_fswConfigFile.Filter = fi.Name;

                    // Add event handlers.
                    m_fswConfigFile.Changed += Watcher_Changed;
                    m_fswConfigFile.Created += Watcher_Changed;
                    m_fswConfigFile.Deleted += Watcher_Changed;
                    m_fswConfigFile.Renamed += Watcher_Changed;

                    // Begin watching.
                    m_fswConfigFile.EnableRaisingEvents = true;
                }
            }
        }

        private void ReadConfigSettings()
        {
            _log?.DebugFormat(2, "ReadConfigSettings: {0}", ConfigFile);
            XmlDocument xlConfig = new XmlDocument();
            XmlNodeList xlAppSettings;
            bool bLoaded = false;
            int iCount = 0;
            while (!bLoaded && iCount < 5)
            {
                try
                {
                    xlConfig.Load(ConfigFile);
                    bLoaded = true;
                }
                catch (Exception ex)
                {
                    iCount++;
                    if (iCount >= 5)
                        _log?.ErrorFormat("Error Loading Config File {0}", ex.ToString());
                    else
                        System.Threading.Thread.Sleep(500);
                }
            }
            try
            {
                if (ConfigSettingChanged != null)
                {
                    // reading LogFile key value to set LogPath.LogFile  and LogPath.LoggingThreshold properties
                    xlAppSettings = xlConfig.SelectNodes("/configuration/appSettings/add");
                    foreach (XmlNode xlSetting in xlAppSettings)
                    {
                        XmlAttribute attrKey = xlSetting.Attributes["key"];
                        XmlAttribute attrValue = xlSetting.Attributes["value"];
                        bool bIsPassword = false;

                        if (attrKey != null && attrValue != null)
                        {
                        //    //Data.AddOrUpdate(attrKey.Value, attrValue.Value.Trim(), (key, oldValue) => attrValue.Value.Trim());
                        //    Logger.MonitoredSettings.AddOrUpdate(attrKey.Value, attrValue.Value, (key, oldValue) => attrValue.Value);
                            if (_log != null && _log.IsUnEvaluated(attrKey.Value, attrValue.Value))
                            {
                        //        vAutoLogFunction.WriteError(string.Format("Template Value Found!  Item: [{0}] - Value: [{1}].", attrKey.Value, attrValue.Value));
                            }
                            else
                            {
                                if (_log != null && _log.IsConnectionString(attrKey.Value, attrValue.Value))
                                {
                                    //attrValue.InnerText = ApplicationSettings.LoggerUtility.StringRemovePassword(attrValue.InnerText);
                                    SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(attrValue.Value)
                                    {
                                        Password = "**************"
                                    };
                                    //strXmlData = strXmlData.Replace(attrValue.Value, scsb.ToString());
                                    bIsPassword = true;
                                }
                                if (attrKey.Value.ToLower() == "PassWord".ToLower())
                                {
                                    //attrValue.InnerText = "";
                                    //strXmlData = strXmlData.Replace(attrValue.OuterXml, attrValue.OuterXml.Replace(attrValue.InnerText, "********"));
                                    bIsPassword = true;
                                }
                                if (attrKey.Value.ToLower() == "RptServerDBPassWord".ToLower())
                                {
                                    //attrValue.InnerText = "";
                                    //strXmlData = strXmlData.Replace(attrValue.OuterXml, attrValue.OuterXml.Replace(attrValue.InnerText, "********"));
                                    bIsPassword = true;
                                }
                            }
                        }

                        ConfigSettingChangedEventArgs csc;
                        if (Data.TryGetValue(xlSetting.Attributes["key"].Value, out object oData))
                        {
                            csc = new ConfigSettingChangedEventArgs(xlSetting.Attributes["key"].Value, (string)oData, xlSetting.Attributes["value"].Value, false, bIsPassword);
                        }
                        else
                        {
                            csc = new ConfigSettingChangedEventArgs(xlSetting.Attributes["key"].Value, "", xlSetting.Attributes["value"].Value, true, bIsPassword);
                        }
                        ConfigSettingChanged(this, csc);
                        Data.AddOrUpdate(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].Value, (key, oldValue) => xlSetting.Attributes["value"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.ErrorFormat("Error Loading Config File {0}", ex.ToString());
            }
        }

    }
}
