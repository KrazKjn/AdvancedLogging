using System;
using System.IO;

namespace AdvancedLogging.Events
{
    /// <summary>
    /// Provides data for the configuration file changed event.
    /// </summary>
    public class ConfigFileChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the configuration file that changed.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the type of change that occurred on the configuration file.
        /// </summary>
        public WatcherChangeTypes ChangeType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigFileChangedEventArgs"/> class.
        /// </summary>
        /// <param name="fileName">The name of the configuration file that changed.</param>
        /// <param name="changeType">The type of change that occurred on the configuration file.</param>
        public ConfigFileChangedEventArgs(string fileName, WatcherChangeTypes changeType)
        {
            FileName = fileName;
            ChangeType = changeType;
        }
    }
}
