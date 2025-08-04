using AdvancedLogging.DataAccess.Configurations;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using AdvancedLogging.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace AdvancedLogging.BusinessLogic
{
    public class Configuration : IConfiguration
    {
        protected Models.Configuration configuration;

        private readonly string clientNameKey = "/appSettings/ClientName";
        private readonly string applicationNameKey = "/appSettings/ApplicationName";
        private readonly string serverKey = "/appSettings/ConfigurationServerUrl";

        private readonly ICommonLogger _logger;
        private readonly ILoggingContext _loggingContext;

        public Configuration(ICommonLogger logger, ILoggingContext loggingContext, string configurationXML)
        {
            _logger = logger;
            _loggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { configurationXML }))
            {
                try
                {
                    configuration = new Models.Configuration()
                    {
                        ApplicationName = null,
                        ClientName = null,
                        Keys = new Dictionary<string, Models.ConfigurationParameter>()
                    };

                    ParseXmlConfiguration(configurationXML);

                    if (ContainsKey(clientNameKey) && ContainsKey(applicationNameKey) && ContainsKey(serverKey))
                    {
                        string serverUrl = GetKeyValue(serverKey);
                        configuration.ClientName = GetKeyValue(clientNameKey);
                        configuration.ApplicationName = GetKeyValue(applicationNameKey);
                        IConfigurationServerFactory configurationServerFactory = new ConfigurationFactory();
                        AddConfigFromServer(configurationServerFactory.Create(serverUrl));
                    }

                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { configurationXML }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Configuration(ICommonLogger logger, ILoggingContext loggingContext, string configurationXML, IConfigurationServerFactory configurationServerFactory)
        {
            _logger = logger;
            _loggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { configurationXML, configurationServerFactory }))
            {
                try
                {
                    configuration = new Models.Configuration()
                    {
                        ApplicationName = null,
                        ClientName = null,
                        Keys = new Dictionary<string, Models.ConfigurationParameter>()
                    };

                    ParseXmlConfiguration(configurationXML);

                    if (ContainsKey(clientNameKey) && ContainsKey(applicationNameKey) && ContainsKey(serverKey))
                    {
                        string serverUrl = GetKeyValue(serverKey);
                        configuration.ClientName = GetKeyValue(clientNameKey);
                        configuration.ApplicationName = GetKeyValue(applicationNameKey);
                        AddConfigFromServer(configurationServerFactory.Create(serverUrl));
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { configurationXML, configurationServerFactory }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys()
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { }))
            {
                try
                {
                    var results = configuration.Keys.Keys;
                    return results;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys(string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { baseKeyPath }))
            {
                try
                {
                    Dictionary<string, Models.ConfigurationParameter> results = new Dictionary<string, Models.ConfigurationParameter>();

                    foreach (string key in configuration.Keys.Keys)
                    {
                        if (key.StartsWith(baseKeyPath))
                        {
                            results.Add(key, configuration.Keys[key]);
                        }
                    }

                    return results.Keys;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { baseKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToFields(Type assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo }))
            {
                try
                {
                    AssignValuesToFields(assignTo, "/");
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToFields(Type assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo, baseKeyPath }))
            {
                try
                {
                    Type t = assignTo;

                    string key;
                    string oldValue;
                    string newValue;
                    baseKeyPath = baseKeyPath.TrimEnd('/') + "/";
                    foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        key = baseKeyPath + field.Name;
                        if ((Type.Equals(typeof(string), field.GetType())) && ContainsKey(key))
                        {
                            newValue = GetKeyValue(key);
                            oldValue = (string)field.GetValue(null);
                            if (String.IsNullOrEmpty(oldValue) || !String.IsNullOrEmpty(newValue))
                            {
                                field.SetValue(null, newValue);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo, baseKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToFields(Object assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo }))
            {
                try
                {
                    AssignValuesToFields(assignTo, "/");
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToFields(Object assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo, baseKeyPath }))
            {
                try
                {
                    Type t = assignTo.GetType();

                    string key;
                    string oldValue;
                    string newValue;
                    baseKeyPath = baseKeyPath.TrimEnd('/') + "/";
                    foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        key = baseKeyPath + field.Name;
                        if ((Type.Equals(typeof(string), field.GetType())) && ContainsKey(key))
                        {
                            newValue = GetKeyValue(key);
                            oldValue = (string)field.GetValue(assignTo);
                            if (String.IsNullOrEmpty(oldValue) || !String.IsNullOrEmpty(newValue))
                            {
                                field.SetValue(assignTo, newValue);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo, baseKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToProperty(Type assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo }))
            {
                try
                {
                    AssignValuesToProperty(assignTo, "/");
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToProperty(Type assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo, baseKeyPath }))
            {
                try
                {
                    Type t = assignTo;

                    string key;
                    string oldValue;
                    string newValue;
                    baseKeyPath = baseKeyPath.TrimEnd('/') + "/";
                    foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        key = baseKeyPath + field.Name;
                        if ((Type.Equals(typeof(string), field.GetType())) && ContainsKey(key))
                        {
                            newValue = GetKeyValue(key);
                            oldValue = (string)field.GetValue(null);
                            if (String.IsNullOrEmpty(oldValue) || !String.IsNullOrEmpty(newValue))
                            {
                                field.SetValue(null, newValue);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo, baseKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToProperty(Object assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo }))
            {
                try
                {
                    AssignValuesToProperty(assignTo, "/");
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void AssignValuesToProperty(Object assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { assignTo, baseKeyPath }))
            {
                try
                {
                    Type t = assignTo.GetType();

                    string key;
                    string oldValue;
                    string newValue;
                    baseKeyPath = baseKeyPath.TrimEnd('/') + "/";
                    foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        key = baseKeyPath + field.Name;
                        if ((Type.Equals(typeof(string), field.GetType())) && ContainsKey(key))
                        {
                            newValue = GetKeyValue(key);
                            oldValue = (string)field.GetValue(assignTo);
                            if (String.IsNullOrEmpty(oldValue) || !String.IsNullOrEmpty(newValue))
                            {
                                field.SetValue(assignTo, newValue);
                            }
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { assignTo, baseKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public bool ContainsKey(string keyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { keyPath }))
            {
                try
                {
                    return configuration.Keys.ContainsKey(keyPath);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { keyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public string GetKeyValue(string keyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { keyPath }))
            {
                try
                {
                    string returnValue = string.Empty;

                    if (configuration.Keys.ContainsKey(keyPath))
                    {
                        if (configuration.Keys[keyPath].Value != null)
                        {
                            returnValue = configuration.Keys[keyPath].Value;
                        }
                    }
                    return returnValue;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { keyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void GetKeyInformation(string keyPath, out string description, out string defaultedFrom)
        {
            description = configuration.Keys[keyPath].Description;
            defaultedFrom = configuration.Keys[keyPath].DefaultLevel;
        }

        public void AddConfigFromServer(IConfigurationServer srvDAL)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { srvDAL }))
            {
                try
                {
                    Dictionary<string, Models.ConfigurationParameter> results;
                    results = srvDAL.GetConfigurationParametersWithDefaults(configuration.ClientName, configuration.ApplicationName);

                    MergeConfigurations(configuration.Keys, results);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { srvDAL }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static System.Configuration.Configuration OpenConfigurationFile(ICommonLogger logger, ILoggingContext loggingContext, string fileName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { fileName }))
            {
                if (!fileName.ToLower().EndsWith(".config"))
                    fileName += ".config";

                System.Configuration.Configuration cfg = null;
                try
                {
                    cfg = System.Configuration.ConfigurationManager.OpenExeConfiguration(fileName);
                }
                catch
                {
                    cfg = null;
                }
                if (cfg == null || cfg.AppSettings.Settings.Count == 0)
                {
                    try
                    {
                        System.Configuration.ExeConfigurationFileMap map = new System.Configuration.ExeConfigurationFileMap { ExeConfigFilename = fileName };
                        cfg = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(map, System.Configuration.ConfigurationUserLevel.None);
                    }
                    catch
                    {
                        cfg = null;
                    }
                }
                return cfg;
            }
        }
        private void ParseXmlConfiguration(string configurationXML)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { configurationXML }))
            {
                try
                {
                    Dictionary<string, Models.ConfigurationParameter> results = new Dictionary<string, Models.ConfigurationParameter>();

                    XElement contacts = XElement.Parse(configurationXML);
                    foreach (XElement child in contacts.Elements())
                    {
                        ParseConfigurationElement(child, "/");
                    }

                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { configurationXML }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private void ParseConfigurationElement(XElement node, string parentKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { node, parentKeyPath }))
            {
                try
                {
                    string keyPath = parentKeyPath;

                    if (node.HasElements)
                    {
                        keyPath = keyPath + node.Name + "/";
                        foreach (XElement child in node.Elements())
                        {
                            ParseConfigurationElement(child, keyPath);
                        }
                    }
                    else
                    {
                        XAttribute keyName = node.Attribute("key");
                        XAttribute keyValue = node.Attribute("value");

                        if (keyName != null && keyValue != null)
                        {
                            keyPath += keyName.Value;

                            Models.ConfigurationParameter configurationParameter = new Models.ConfigurationParameter(keyValue.Value);

                            configuration.Keys.Add(keyPath, configurationParameter);
                        }
                    }

                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { node, parentKeyPath }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private Dictionary<string, Models.ConfigurationParameter> MergeConfigurations(
                            Dictionary<string, Models.ConfigurationParameter> baseCongfiguration,
                            Dictionary<string, Models.ConfigurationParameter> overrides)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { baseCongfiguration, overrides }))
            {
                try
                {
                    if (overrides != null)
                    {
                        foreach (KeyValuePair<string, Models.ConfigurationParameter> pair in overrides)
                        {
                            string key = pair.Key;
                            Models.ConfigurationParameter value = pair.Value;
                            baseCongfiguration[key] = value;
                        }
                    }

                    return baseCongfiguration;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { baseCongfiguration, overrides }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void UpdateConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value, false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void UpdateConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, int value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void UpdateConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void UpdateConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, bool value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void UpdateConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, DateTime value, bool bDateOnly = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value, bDateOnly }))
            {
                try
                {
                    if (bDateOnly)
                        SetConfigurationStringValue(config, Log, loggingContext, key, value.Date.ToString(), false);
                    else
                        SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value, bDateOnly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void CreateConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value, true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void CreateConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, int value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void CreateConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void CreateConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, DateTime value, bool bDateOnly = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value, bDateOnly }))
            {
                try
                {
                    if (bDateOnly)
                        SetConfigurationStringValue(config, Log, loggingContext, key, value.Date.ToString(), true);
                    else
                        SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value, bDateOnly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static void CreateConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, bool value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, loggingContext, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        private static void SetConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, string value, Boolean createKeyIfNotFound)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, value, createKeyIfNotFound }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        vAutoLogFunction.WriteDebugFormat("SetConfigurationStringValue({0}) = {1}", key, value);
                        config.AppSettings.Settings[key].Value = value;
                        config.Save();
                    }
                    else
                    {
                        if (createKeyIfNotFound)
                        {
                            vAutoLogFunction.WriteDebugFormat("SetConfigurationStringValue({0}) = {1}", key, value);
                            config.AppSettings.Settings.Add(new System.Configuration.KeyValueConfigurationElement(key, value));
                            config.AppSettings.Settings[key].Value = value;
                            config.Save();
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("SetConfigurationStringValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new KeyNotFoundException("Settings collection does not contain the requested key: " + key);
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value, createKeyIfNotFound }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string GetConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, string defaultvalue = null, bool bIsPassword = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue, bIsPassword }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                if (defaultvalue == null)
                                    throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                                else
                                {
                                    vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (Template place holder has not been populated with a value.  Using DEFAULT VALUE)", key, bIsPassword ? "**************" : defaultvalue);
                                    vAutoLogFunction.Logger.IsPassword.AddOrUpdate(key, bIsPassword, (ExistingKey, oldValue) => bIsPassword);
                                    return defaultvalue;
                                }
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1}", key, bIsPassword ? "**************" : config.AppSettings.Settings[key].Value);
                            vAutoLogFunction.Logger.IsPassword.AddOrUpdate(key, bIsPassword, (ExistingKey, oldValue) => bIsPassword);
                            return config.AppSettings.Settings[key].Value;
                        }
                    }
                    else
                    {
                        if (defaultvalue == null)
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (DEFAULT VALUE)", key, bIsPassword ? "**************" : defaultvalue);
                            vAutoLogFunction.Logger.IsPassword.AddOrUpdate(key, bIsPassword, (ExistingKey, oldValue) => bIsPassword);
                            return defaultvalue;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue, bIsPassword }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static int GetConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, int defaultvalue = int.MaxValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                return defaultvalue;
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);
                            if (int.TryParse(config.AppSettings.Settings[key].Value, out int iVal))
                            {
                                return iVal;
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("GetConfigurationIntValue({0}):  Invalid Int Value [{1}].", key, config.AppSettings.Settings[key].Value);
                                if (defaultvalue == int.MaxValue)
                                {
                                    vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}):  Settings collection does not contain the requested key.", key);
                                    throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                                }
                                else
                                {
                                    vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                                    return defaultvalue;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (defaultvalue == int.MaxValue)
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                            return defaultvalue;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static double GetConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, double defaultvalue = double.MaxValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                return defaultvalue;
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);
                            if (double.TryParse(config.AppSettings.Settings[key].Value, out double dVal))
                            {
                                return dVal;
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("GetConfigurationIntValue({0}):  Invalid double Value [{1}].", key, config.AppSettings.Settings[key].Value);
                                if (defaultvalue == double.MaxValue)
                                {
                                    vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}):  Settings collection does not contain the requested key.", key);
                                    throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                                }
                                else
                                {
                                    vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                                    return defaultvalue;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (defaultvalue == double.MaxValue)
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationIntValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                            return defaultvalue;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string GetConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, DateTime defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                if (defaultvalue == null)
                                    throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                                else
                                    return defaultvalue.Date.ToString();
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);
                            if (DateTime.TryParse(config.AppSettings.Settings[key].Value, out DateTime dt))
                            {
                                return dt.Date.ToString();
                            }
                            throw new ArgumentException(string.Format("Value [{0}] cannot be converted to a DateTime.Date.", config.AppSettings.Settings[key].Value));
                        }
                    }
                    else
                    {
                        if (defaultvalue == null)
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                            return defaultvalue.Date.ToString();
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static DateTime GetConfigurationDateTimeValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, DateTime defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                if (defaultvalue == null)
                                    throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                                else
                                    return defaultvalue;
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);
                            if (DateTime.TryParse(config.AppSettings.Settings[key].Value, out DateTime dt))
                            {
                                return dt;
                            }
                            throw new ArgumentException(string.Format("Value [{0}] cannot be converted to a DateTime.", config.AppSettings.Settings[key].Value));
                        }
                    }
                    else
                    {
                        if (defaultvalue == null)
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}):  Settings collection does not contain the requested key.", key);
                            throw new IndexOutOfRangeException("Settings collection does not contain the requested key: " + key);
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue);
                            return defaultvalue;
                        }
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static bool GetConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, bool defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                return defaultvalue;
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);

                            if (Int16.TryParse(config.AppSettings.Settings[key].Value, out short i))
                            {
                                return (i != 0);
                            }
                            else
                            {

                                if (bool.TryParse(config.AppSettings.Settings[key].Value, out bool b))
                                {
                                    return b;
                                }
                                else
                                {
                                    var arrTrueStatus = new[] { "T", "Y", "YES" };
                                    var arrFalseStatus = new[] { "F", "N", "NO" };

                                    if (arrTrueStatus.Contains(config.AppSettings.Settings[key].Value.ToUpper()))
                                        return true;
                                    if (arrFalseStatus.Contains(config.AppSettings.Settings[key].Value.ToUpper()))
                                        return false;
                                }
                            }
                            vAutoLogFunction.WriteErrorFormat("Value [{0}] cannot be converted to a boolean.", config.AppSettings.Settings[key].Value);
                            return defaultvalue;
                        }
                    }
                    else
                    {
                        vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue.ToString());
                        return defaultvalue;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static SqlConnectionStringBuilder GetConfigurationConnectionValue(System.Configuration.Configuration config, ICommonLogger Log, ILoggingContext loggingContext, string key, SqlConnectionStringBuilder defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(Log, loggingContext, new { config, Log, key, defaultvalue }))
            {
                try
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        if (config.AppSettings.Settings[key].Value.StartsWith("#{"))
                        {
                            if (Log == null)
                            {
                                throw new ArgumentOutOfRangeException(key, "Template place holder has not been populated with a value.");
                            }
                            else
                            {
                                vAutoLogFunction.WriteErrorFormat("{0}: Template place holder has not been populated with a value.", key);
                                return defaultvalue;
                            }
                        }
                        else
                        {
                            vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1}", key, config.AppSettings.Settings[key].Value);

                            try
                            {
                                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(config.AppSettings.Settings[key].Value);
                                return scsb;
                            }
                            catch
                            {
                                vAutoLogFunction.WriteErrorFormat("Value [{0}] cannot be converted to a SqlConnectionStringBuilder.", config.AppSettings.Settings[key].Value);
                                return defaultvalue;
                            }
                        }
                    }
                    else
                    {
                        vAutoLogFunction.WriteDebugFormat("GetConfigurationValue({0}) = {1} (DEFAULT VALUE)", key, defaultvalue.ToString());
                        return defaultvalue;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string GetConnectionStringItemValue(ICommonLogger logger, ILoggingContext loggingContext, string itemName, string connectionString, string defaultvalue = null)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { itemName, connectionString, defaultvalue }))
            {
                try
                {
                    SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

                    if (sqlConnectionStringBuilder.ContainsKey(itemName))
                        return sqlConnectionStringBuilder[itemName].ToString().Trim();
                    else
                    {
                        if (defaultvalue == null)
                        {
                            throw new KeyNotFoundException("ConnectionString does not contain the requested key: " + itemName);
                        }
                        else
                            return defaultvalue;
                    }
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { itemName, connectionString, defaultvalue }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string SetConnectionStringItemValue(ICommonLogger logger, ILoggingContext loggingContext, string itemName, object itemValue, string connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { itemName, itemValue, connectionString }))
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                    if (builder.ContainsKey(itemName))
                    {
                        if (builder[itemName].GetType() == itemValue.GetType())
                            builder[itemName] = itemValue;
                        else
                            throw new DataMisalignedException("ConnectionString item value for: [" + itemName + "] is not of the type [" + builder[itemName].GetType().Name + "]");
                    }
                    else
                    {
                        throw new KeyNotFoundException("Requested key is not valid in the specified ConnectionString type: " + itemName);
                    }
                    return builder.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { itemName, itemValue, connectionString }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string RedactConfigFileContents(ICommonLogger logger, ILoggingContext loggingContext, XmlDocument xmlConfig, string maskvalue = "********", bool purge = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { xmlConfig, maskvalue, purge }))
            {
                try
                {
                    XmlNodeList xmlAppSettings;
                    string[] nodes = { "/configuration/appSettings/add", "/configuration/connectionStrings/add", "/configuration/add" };
                    foreach (string node in nodes)
                    {
                        xmlAppSettings = xmlConfig.SelectNodes(node);

                        string[] keys = { "DBConnectionString", "SQLConnection", "SQLConnection2", "SQLConn", "SQLConnString" };
                        string[] names = { "iNeedItConnection", "DBConnectionString" };

                        foreach (XmlNode xlSetting in xmlAppSettings)
                        {
                            if (xlSetting.Attributes["key"] == null)
                            {
                                if (xlSetting.Attributes["name"] != null)
                                {
                                    if (names.Contains(xlSetting.Attributes["name"].Value))
                                    {
                                        if (purge)
                                        {
                                            if (xlSetting.Attributes["value"].InnerText.StartsWith("#{"))
                                            {
                                                vAutoLogFunction.WriteErrorFormat("", xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText);
                                            }
                                            else
                                            {
                                                xlSetting.Attributes["value"].InnerText = LoggerUtility.StringRemovePasswordStatic(xlSetting.Attributes["value"].InnerText);
                                            }
                                        }
                                        else
                                        {
                                            if (xlSetting.Attributes["value"].InnerText.StartsWith("#{"))
                                            {
                                                vAutoLogFunction.WriteErrorFormat("", xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText);
                                            }
                                            else
                                            {
                                                xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                            }
                                        }
                                        vAutoLogFunction.WriteLogFormat("RedactConfigFile: Sanitizing: Section: {0}; Item: {1}", xlSetting, xlSetting.Attributes["name"].Value);
                                    }
                                    else if (logger.IsPassword.GetOrAdd(xlSetting.Attributes["name"].Value, false))
                                    {
                                        xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                    }
                                    else
                                    {
                                        if (logger.IsPassword.Keys.Any(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower())))
                                        {
                                            var passwords = logger.IsPassword.Keys.Where(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower()));
                                            foreach (string password in passwords)
                                            {
                                                if (logger.IsPassword.GetOrAdd(password, false))
                                                    xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (keys.Contains(xlSetting.Attributes["key"].Value))
                                {
                                    if (purge)
                                    {
                                        if (xlSetting.Attributes["value"].InnerText.StartsWith("#{"))
                                        {
                                            logger.ErrorFormat("", xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText);
                                        }
                                        else
                                        {
                                            xlSetting.Attributes["value"].InnerText = LoggerUtility.StringRemovePasswordStatic(xlSetting.Attributes["value"].InnerText);
                                        }
                                    }
                                    else
                                    {
                                        if (xlSetting.Attributes["value"].InnerText.StartsWith("#{"))
                                        {
                                            logger.ErrorFormat("", xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText);
                                        }
                                        else
                                        {
                                            xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                        }
                                    }
                                    vAutoLogFunction.WriteLogFormat("RedactConfigFile: Sanitizing: Section: {0}; Item: {1}", xlSetting, xlSetting.Attributes["key"].Value);
                                }
                                else if (logger.IsPassword.GetOrAdd(xlSetting.Attributes["key"].Value, false))
                                {
                                    xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                }
                                else
                                {
                                    if (logger.IsPassword.Keys.Any(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower())))
                                    {
                                        var passwords = logger.IsPassword.Keys.Where(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower()));
                                        foreach (string password in passwords)
                                        {
                                            if (logger.IsPassword.GetOrAdd(password, false))
                                                xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return (XDocument.Parse(xmlConfig.OuterXml).ToString());

                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { xmlConfig, maskvalue, purge }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public static string RedactConfigFile(ICommonLogger logger, ILoggingContext loggingContext, string szConfigFile, string maskvalue = "********", bool purge = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(logger, loggingContext, new { szConfigFile, maskvalue, purge }))
            {
                try
                {
                    XmlDocument xlConfig = new XmlDocument();
                    xlConfig.Load(szConfigFile);
                    return RedactConfigFileContents(logger, loggingContext, xlConfig, maskvalue, purge);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { szConfigFile, maskvalue, purge }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
