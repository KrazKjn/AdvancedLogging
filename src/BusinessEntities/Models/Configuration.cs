using System.Collections.Generic;

namespace AdvancedLogging.Models
{
    /// <summary>
    /// A configuration class.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
            Keys = new Dictionary<string, ConfigurationParameter>();
        }

        /// <summary>
        /// The Client Name the configuration is for.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// The Application Name the configuration is for.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// A dictionary containing the configuration parameters keys and their value and some metadata.
        /// A key is a full key path. A path always starts with a slash ("/") and the parts of a path
        /// are separated by a slash. A key path ends in the key's name (no trailing slash). Path parts
        /// and key names may not have a slash as part of them.
        /// </summary>
        public Dictionary<string, ConfigurationParameter> Keys { get; set; }
    }
}
