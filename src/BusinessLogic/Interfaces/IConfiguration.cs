using System;
using System.Collections.Generic;

namespace AdvancedLogging.Interfaces
{
    /// <summary>
    /// Interface for configuration management.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Get a collection of keys in the configuration.
        /// </summary>
        /// <returns>A collection of the keys.</returns>
        Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys();

        /// <summary>
        /// Get a collection of keys in the configuration.
        /// </summary>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        /// <returns>A collection of the keys.</returns>
        Dictionary<string, Models.ConfigurationParameter>.KeyCollection GetKeys(string baseKeyPath);

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
        /// <returns>True if the key exists, otherwise false.</returns>
        bool ContainsKey(string keyPath);

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
        string GetKeyValue(string keyPath);

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
        /// <param name="description">A description of what this parameter is. May be null.</param>
        /// <param name="defaultedFrom">
        ///     If the parameter was defaulted, the value will be one of: 
        ///         "Generic", "Application Level" or "Client Level".
        ///     If the parameter is not defaulted, this will be null.
        /// </param>
        void GetKeyInformation(string keyPath, out string description, out string defaultedFrom);

        /// <summary>
        /// Add configuration information from a Configuration Server.
        /// </summary>
        /// <param name="svrDal">A Data Access Layer object for a Configuration Server.</param>
        void AddConfigFromServer(IConfigurationServer svrDal);

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        void AssignValuesToFields(Type assignTo);

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        void AssignValuesToFields(Type assignTo, string baseKeyPath);

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        void AssignValuesToFields(Object assignTo);

        /// <summary>
        /// Assigns values of keys to corresponding field names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        void AssignValuesToFields(Object assignTo, string baseKeyPath);

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        void AssignValuesToProperty(Type assignTo);

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        void AssignValuesToProperty(Type assignTo, string baseKeyPath);

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        void AssignValuesToProperty(Object assignTo);

        /// <summary>
        /// Assigns values of keys to corresponding property names in the object where 
        /// the field name is the key name less the key pattern and its terminating slash.
        /// </summary>
        /// <param name="assignTo">Object to assign values to.</param>
        /// <param name="baseKeyPath">
        ///     The base path to the key desired.  
        ///     A path begins with a slash.  
        ///     The parts of a path are separated by a slash.  
        ///     There is no trailing slash.  
        ///     Parts may not contain a slash.
        /// </param>
        void AssignValuesToProperty(Object assignTo, string baseKeyPath);
    }
}
