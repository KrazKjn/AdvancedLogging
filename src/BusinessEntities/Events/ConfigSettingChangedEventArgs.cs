using System;

namespace AdvancedLogging.Events
{
    /// <summary>
    /// Provides data for the configuration setting changed event.
    /// </summary>
    public class ConfigSettingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the setting that changed.
        /// </summary>
        public string SettingName { get; private set; }

        /// <summary>
        /// Gets the old value of the setting before the change.
        /// </summary>
        public string OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the setting after the change.
        /// </summary>
        public string NewValue { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the setting was added.
        /// </summary>
        public bool ValueAdded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the setting is a password.
        /// </summary>
        public bool IsPassword { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSettingChangedEventArgs"/> class.
        /// </summary>
        /// <param name="settingName">The name of the setting that changed.</param>
        /// <param name="oldValue">The old value of the setting before the change.</param>
        /// <param name="newValue">The new value of the setting after the change.</param>
        /// <param name="valueAdded">Indicates whether the setting was added.</param>
        /// <param name="isPassword">Indicates whether the setting is a password.</param>
        public ConfigSettingChangedEventArgs(string settingName, string oldValue, string newValue, bool valueAdded, bool isPassword)
        {
            SettingName = settingName;
            OldValue = oldValue;
            NewValue = newValue;
            ValueAdded = valueAdded;
            IsPassword = isPassword;
        }
    }
}