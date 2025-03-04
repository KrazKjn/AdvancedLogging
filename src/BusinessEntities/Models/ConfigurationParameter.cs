using System;

[assembly: CLSCompliant(true)]
namespace AdvancedLogging.Models
{
    /// <summary>
    /// A configuration parameter.
    /// </summary>
    public class ConfigurationParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationParameter"/> class.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="description">An optional description about what the parameter is for.</param>
        /// <param name="defaultLevel">An optional comment on where the value was inherited from.</param>
        public ConfigurationParameter(string value, string description = null, string defaultLevel = null)
        {
            Value = value;
            Description = description;
            DefaultLevel = defaultLevel;
        }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        /// <value>The value of the parameter.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets an optional description about what the parameter is for. 
        /// Note -- this could be null/empty/missing.
        /// </summary>
        /// <value>The description of the parameter.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets an optional comment on where the value was inherited from.  
        /// If the value was specified instead of inherited, then this will be null/empty/missing.
        /// </summary>
        /// <value>The default level of the parameter.</value>
        public string DefaultLevel { get; set; }
    }
}
