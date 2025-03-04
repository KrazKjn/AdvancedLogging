using AdvancedLogging.DataAccess.Configurations;
using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
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

        /// <summary>
        /// Initialize a Configuration object using only, classical, XML from a configuration file.
        /// 
        /// The configuration server is queried, using the default configuration factory,
        /// for additional/override configuration if the configuration contains the following keys:
        ///     ApplicationName
        ///     ClientName
        ///     ConfigurationServerUrl
        /// </summary>
        /// <param name="configurationXML">Content of configuration file</param>
        public Configuration(string configurationXML)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { configurationXML }))
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

        /// <summary>
        /// Initialize a Configuration object using XML from a configuration file and a specified configuration
        /// server factory (suitable for dependency injection).
        /// 
        /// The configuration server is queried for additional/override configuration if 
        /// the configuration contains the following keys:
        ///     ApplicationName
        ///     ClientName
        ///     ConfigurationServerUrl
        /// </summary>
        /// <param name="configurationXML">Content of configuration file</param>
        /// <param name="configurationServerFactory">Factory to use to create a configuration server</param>
        public Configuration(string configurationXML, IConfigurationServerFactory configurationServerFactory)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { configurationXML, configurationServerFactory }))
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

        /// <summary>
        /// Get a collection of keys in the configuration.
        /// </summary>
        /// <returns>A collection of the keys.</returns>
        public Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
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

        /// <summary>
        /// Get a collection of keys in the configuration that begin with a given path.
        /// </summary>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        /// <returns>A collection of the keys.</returns>
        public Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys(string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { baseKeyPath }))
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

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        public void AssignValuesToFields(Type assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo }))
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

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        public void AssignValuesToFields(Type assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo, baseKeyPath }))
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

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        public void AssignValuesToFields(Object assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo }))
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

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        public void AssignValuesToFields(Object assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo, baseKeyPath }))
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

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        public void AssignValuesToProperty(Type assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo }))
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

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        public void AssignValuesToProperty(Type assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo, baseKeyPath }))
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

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        public void AssignValuesToProperty(Object assignTo)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo }))
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

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        public void AssignValuesToProperty(Object assignTo, string baseKeyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { assignTo, baseKeyPath }))
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

        /// <summary>
        /// This method will return a boolean indicating if the specified key exists.
        /// </summary>
        /// <param name="keyPath">
        ///     The full path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        /// <returns>The value of the key.</returns>
        public bool ContainsKey(string keyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { keyPath }))
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

        /// <summary>
        /// This method will return the value of a configuration parameter.
        /// </summary>
        /// <param name="keyPath">
        ///     The full path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        /// <returns>The value of the key.</returns>
        public string GetKeyValue(string keyPath)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { keyPath }))
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

        /// <summary>
        /// This method will return the metadata of a configuration parameter.
        /// </summary>
        /// <param name="keyPath">
        ///     The full path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        /// <param name="description">A description of what this parameter is.  May be null.</param>
        /// <param name="defaultedFrom">
        ///     If the parameter was defaulted, the value will be one of: 
        ///         "Generic", "Application Level" or "Client Level".
        ///     If the parameter is not defaulted, this will be null.
        /// </param>
        public void GetKeyInformation(string keyPath, out string description, out string defaultedFrom)
        {
            description = configuration.Keys[keyPath].Description;
            defaultedFrom = configuration.Keys[keyPath].DefaultLevel;
        }

        /// <summary>
        /// Add configuration information from a Configuration Server
        /// </summary>
        /// <param name="svrDal">A Data Access Layer object for a Configuration Server</param>
        public void AddConfigFromServer(IConfigurationServer srvDAL)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { srvDAL }))
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

        /// <summary>
        /// Opens another application's Config File as a System.Configuration.Configuration
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>System.Configuration.Configuration</returns>
        public static System.Configuration.Configuration OpenConfigurationFile(string fileName)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { fileName }))
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
            using (var vAutoLogFunction = new AutoLogFunction(new { configurationXML }))
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
            using (var vAutoLogFunction = new AutoLogFunction(new { node, parentKeyPath }))
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
            using (var vAutoLogFunction = new AutoLogFunction(new { baseCongfiguration, overrides }))
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


        /// <summary>
        /// Update only a String type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void UpdateConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, string key, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value, false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update only a Int type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void UpdateConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, string key, int value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update only a Double type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void UpdateConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, string key, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update only a bool/Boolean type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void UpdateConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, string key, bool value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update only a DateTime type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void UpdateConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, string key, DateTime value, bool bDateOnly = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value, bDateOnly }))
            {
                try
                {
                    if (bDateOnly)
                        SetConfigurationStringValue(config, Log, key, value.Date.ToString(), false);
                    else
                        SetConfigurationStringValue(config, Log, key, value.ToString(), false);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value, bDateOnly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update or Add (if does not exist) a String type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void CreateConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, string key, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value, true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update or Add (if does not exist) a Int type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void CreateConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, string key, int value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update or Add (if does not exist) a Double type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void CreateConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, string key, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update or Add (if does not exist) a DateTime type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void CreateConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, string key, DateTime value, bool bDateOnly = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value, bDateOnly }))
            {
                try
                {
                    if (bDateOnly)
                        SetConfigurationStringValue(config, Log, key, value.Date.ToString(), true);
                    else
                        SetConfigurationStringValue(config, Log, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value, bDateOnly }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        /// <summary>
        /// Update or Add (if does not exist) a bool/Boolean type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKey"></param>
        public static void CreateConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, string key, bool value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value }))
            {
                try
                {
                    SetConfigurationStringValue(config, Log, key, value.ToString(), true);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { config, Log, key, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        /// <summary>
        /// Private - Update or Add (if does not exist) a String type Key value in the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="createKeyIfNotFound"></param>
        private static void SetConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, string key, string value, Boolean createKeyIfNotFound)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, value, createKeyIfNotFound }))
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

        /// <summary>
        /// Get a String type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static string GetConfigurationStringValue(System.Configuration.Configuration config, ICommonLogger Log, string key, string defaultvalue = null, bool bIsPassword = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue, bIsPassword }))
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

        /// <summary>
        /// Get a Int type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static int GetConfigurationIntValue(System.Configuration.Configuration config, ICommonLogger Log, string key, int defaultvalue = int.MaxValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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

        /// <summary>
        /// Get a Double type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static double GetConfigurationDoubleValue(System.Configuration.Configuration config, ICommonLogger Log, string key, double defaultvalue = double.MaxValue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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

        /// <summary>
        /// Get a DateTime.Date type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static string GetConfigurationDateValue(System.Configuration.Configuration config, ICommonLogger Log, string key, DateTime defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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

        /// <summary>
        /// Get a DateTime type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static DateTime GetConfigurationDateTimeValue(System.Configuration.Configuration config, ICommonLogger Log, string key, DateTime defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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

        /// <summary>
        /// Get a bool/Boolean type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static bool GetConfigurationBooleanValue(System.Configuration.Configuration config, ICommonLogger Log, string key, bool defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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
        // DbConnectionStringBuilder

        /// <summary>
        /// Get a SqlConnectionStringBuilder  type Key value from the Configuration file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="Log"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static SqlConnectionStringBuilder GetConfigurationConnectionValue(System.Configuration.Configuration config, ICommonLogger Log, string key, SqlConnectionStringBuilder defaultvalue)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { config, Log, key, defaultvalue }))
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


        /// <summary>
        /// Gets an item value from the Connection String
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static string GetConnectionStringItemValue(string itemName, string connectionString, string defaultvalue = null)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { itemName, connectionString, defaultvalue }))
            {
                try
                {
                    // The connection string builder provides strongly typed properties corresponding to the known key/value pairs allowed by SQL Server
                    // Case insensitive and also allows synonyms as documented in .NET by the ConnectionString property for this class
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

        /// <summary>
        /// Sets an item value in the Connection String
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="itemValue"></param>
        /// <param name="connectionString"></param>
        public static string SetConnectionStringItemValue(string itemName, object itemValue, string connectionString)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { itemName, itemValue, connectionString }))
            {
                try
                {
                    // The connection string builder provides strongly typed properties corresponding to the known key/value pairs allowed by SQL Server
                    // Case insensitive and also allows synonyms as documented in .NET by the ConnectionString property for this class
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szConfigFile"></param>
        /// <param name="commonLogger"></param>
        /// <returns></returns>
        public static string RedactConfigFileContents(XmlDocument xmlConfig, ICommonLogger commonLogger, string maskvalue = "********", bool purge = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { xmlConfig, commonLogger, maskvalue, purge }))
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
                                if (xlSetting.Attributes["name"] == null)
                                {
                                    vAutoLogFunction.WriteDebugFormat("RedactConfigFile: Section: {0}; Item: {1} - Nothing to Replace.", xlSetting, xlSetting.InnerText);
                                }
                                else
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
                                    else if (vAutoLogFunction.Logger.IsPassword.GetOrAdd(xlSetting.Attributes["name"].Value, false))
                                    {
                                        xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["name"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                    }
                                    else
                                    {
                                        if (vAutoLogFunction.Logger.IsPassword.Keys.Any(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower())))
                                        {
                                            var passwords = vAutoLogFunction.Logger.IsPassword.Keys.Where(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower()));
                                            foreach (string password in passwords)
                                            {
                                                if (vAutoLogFunction.Logger.IsPassword.GetOrAdd(password, false))
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
                                            commonLogger.ErrorFormat("", xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText);
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
                                            commonLogger.ErrorFormat("", xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText);
                                        }
                                        else
                                        {
                                            xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                        }
                                    }
                                    vAutoLogFunction.WriteLogFormat("RedactConfigFile: Sanitizing: Section: {0}; Item: {1}", xlSetting, xlSetting.Attributes["key"].Value);
                                }
                                else if (vAutoLogFunction.Logger.IsPassword.GetOrAdd(xlSetting.Attributes["key"].Value, false))
                                {
                                    xlSetting.Attributes["value"].InnerText = LoggerUtility.StringMaskPassword(xlSetting.Attributes["key"].Value, xlSetting.Attributes["value"].InnerText, maskvalue);
                                }
                                else
                                {
                                    if (vAutoLogFunction.Logger.IsPassword.Keys.Any(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower())))
                                    {
                                        var passwords = vAutoLogFunction.Logger.IsPassword.Keys.Where(x => xlSetting.Attributes["key"].Value.ToLower().Contains(x.ToLower()));
                                        foreach (string password in passwords)
                                        {
                                            if (vAutoLogFunction.Logger.IsPassword.GetOrAdd(password, false))
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
                    vAutoLogFunction.LogFunction(new { xmlConfig, commonLogger, maskvalue, purge }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="szConfigFile"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static string RedactConfigFile(string szConfigFile, ICommonLogger Log, string maskvalue = "********", bool purge = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { szConfigFile, Log, maskvalue, purge }))
            {
                try
                {
                    XmlDocument xlConfig = new XmlDocument();
                    xlConfig.Load(szConfigFile);
                    return RedactConfigFileContents(xlConfig, Log, maskvalue, purge);
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { szConfigFile, Log, maskvalue, purge }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }
}
