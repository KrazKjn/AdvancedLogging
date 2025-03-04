using System;

namespace AdvancedLogging.Constants
{

    [Serializable]
    public sealed class SharedLevel : IComparable
    {
        //
        // Summary:
        //     The log4net.Core.Level.Off level designates a higher level than all the rest.
        public static readonly SharedLevel Off = new SharedLevel(int.MaxValue, "OFF");

        //
        // Summary:
        //     The log4net.Core.Level.Emergency level designates very severe error events. System
        //     unusable, emergencies.
        public static readonly SharedLevel Log4Net_Debug = new SharedLevel(120000, "log4net:DEBUG");

        //
        // Summary:
        //     The log4net.Core.Level.Emergency level designates very severe error events. System
        //     unusable, emergencies.
        public static readonly SharedLevel Emergency = new SharedLevel(120000, "EMERGENCY");

        //
        // Summary:
        //     The log4net.Core.Level.Fatal level designates very severe error events that will
        //     presumably lead the application to abort.
        public static readonly SharedLevel Fatal = new SharedLevel(110000, "FATAL");

        //
        // Summary:
        //     The log4net.Core.Level.Alert level designates very severe error events. Take
        //     immediate action, alerts.
        public static readonly SharedLevel Alert = new SharedLevel(100000, "ALERT");

        //
        // Summary:
        //     The log4net.Core.Level.Critical level designates very severe error events. Critical
        //     condition, critical.
        public static readonly SharedLevel Critical = new SharedLevel(90000, "CRITICAL");

        //
        // Summary:
        //     The log4net.Core.Level.Severe level designates very severe error events.
        public static readonly SharedLevel Severe = new SharedLevel(80000, "SEVERE");

        //
        // Summary:
        //     The log4net.Core.Level.Error level designates error events that might still allow
        //     the application to continue running.
        public static readonly SharedLevel Error = new SharedLevel(70000, "ERROR");

        //
        // Summary:
        //     The log4net.Core.Level.Warn level designates potentially harmful situations.
        public static readonly SharedLevel Warn = new SharedLevel(60000, "WARN");

        //
        // Summary:
        //     The log4net.Core.Level.Notice level designates informational messages that highlight
        //     the progress of the application at the highest level.
        public static readonly SharedLevel Notice = new SharedLevel(50000, "NOTICE");

        //
        // Summary:
        //     The log4net.Core.Level.Info level designates informational messages that highlight
        //     the progress of the application at coarse-grained level.
        public static readonly SharedLevel Info = new SharedLevel(40000, "INFO");

        //
        // Summary:
        //     The log4net.Core.Level.Debug level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Debug = new SharedLevel(30000, "DEBUG");

        //
        // Summary:
        //     The log4net.Core.Level.Fine level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Fine = new SharedLevel(30000, "FINE");

        //
        // Summary:
        //     The log4net.Core.Level.Trace level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Trace = new SharedLevel(20000, "TRACE");

        //
        // Summary:
        //     The log4net.Core.Level.Finer level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Finer = new SharedLevel(20000, "FINER");

        //
        // Summary:
        //     The log4net.Core.Level.Verbose level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Verbose = new SharedLevel(10000, "VERBOSE");

        //
        // Summary:
        //     The log4net.Core.Level.Finest level designates fine-grained informational events
        //     that are most useful to debug an application.
        public static readonly SharedLevel Finest = new SharedLevel(10000, "FINEST");

        //
        // Summary:
        //     The log4net.Core.Level.All level designates the lowest level possible.
        public static readonly SharedLevel All = new SharedLevel(int.MinValue, "ALL");

        private readonly int m_levelValue;

        private readonly string m_levelName;

        private readonly string m_levelDisplayName;

        //
        // Summary:
        //     Gets the name of this level.
        //
        // Value:
        //     The name of this level.
        //
        // Remarks:
        //     Gets the name of this level.
        public string Name => m_levelName;

        //
        // Summary:
        //     Gets the value of this level.
        //
        // Value:
        //     The value of this level.
        //
        // Remarks:
        //     Gets the value of this level.
        public int Value => m_levelValue;

        //
        // Summary:
        //     Gets the display name of this level.
        //
        // Value:
        //     The display name of this level.
        //
        // Remarks:
        //     Gets the display name of this level.
        public string DisplayName => m_levelDisplayName;

        //
        // Summary:
        //     Constructor
        //
        // Parameters:
        //   level:
        //     Integer value for this level, higher values represent more severe levels.
        //
        //   levelName:
        //     The string name of this level.
        //
        //   displayName:
        //     The display name for this level. This may be localized or otherwise different
        //     from the name
        //
        // Remarks:
        //     Initializes a new instance of the log4net.Core.Level class with the specified
        //     level name and value.
        public SharedLevel(int level, string levelName, string displayName)
        {
            if (levelName == null)
            {
                throw new ArgumentNullException("levelName");
            }

            m_levelValue = level;
            m_levelName = string.Intern(levelName);
            m_levelDisplayName = displayName ?? throw new ArgumentNullException("displayName");
        }

        //
        // Summary:
        //     Constructor
        //
        // Parameters:
        //   level:
        //     Integer value for this level, higher values represent more severe levels.
        //
        //   levelName:
        //     The string name of this level.
        //
        // Remarks:
        //     Initializes a new instance of the log4net.Core.Level class with the specified
        //     level name and value.
        public SharedLevel(int level, string levelName)
            : this(level, levelName, levelName)
        {
        }

        //
        // Summary:
        //     Returns the System.String representation of the current log4net.Core.Level.
        //
        // Returns:
        //     A System.String representation of the current log4net.Core.Level.
        //
        // Remarks:
        //     Returns the level log4net.Core.Level.Name.
        public override string ToString()
        {
            return m_levelName;
        }

        //
        // Summary:
        //     Compares levels.
        //
        // Parameters:
        //   o:
        //     The object to compare against.
        //
        // Returns:
        //     true if the objects are equal.
        //
        // Remarks:
        //     Compares the levels of log4net.Core.Level instances, and defers to base class
        //     if the target object is not a log4net.Core.Level instance.
        public override bool Equals(object o)
        {
            SharedLevel level = o as SharedLevel;
            if (level != null)
            {
                return m_levelValue == level.m_levelValue;
            }

            return base.Equals(o);
        }

        //
        // Summary:
        //     Returns a hash code
        //
        // Returns:
        //     A hash code for the current log4net.Core.Level.
        //
        // Remarks:
        //     Returns a hash code suitable for use in hashing algorithms and data structures
        //     like a hash table.
        //
        //     Returns the hash code of the level log4net.Core.Level.Value.
        public override int GetHashCode()
        {
            return m_levelValue;
        }

        //
        // Summary:
        //     Compares this instance to a specified object and returns an indication of their
        //     relative values.
        //
        // Parameters:
        //   r:
        //     A log4net.Core.Level instance or null to compare with this instance.
        //
        // Returns:
        //     A 32-bit signed integer that indicates the relative order of the values compared.
        //     The return value has these meanings:
        //
        //     Value – Meaning
        //     Less than zero – This instance is less than r.
        //     Zero – This instance is equal to r.
        //     Greater than zero –
        //
        //     This instance is greater than r.
        //
        //     -or-
        //
        //     r is null.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     r is not a log4net.Core.Level.
        //
        // Remarks:
        //     r must be an instance of log4net.Core.Level or null; otherwise, an exception
        //     is thrown.
        public int CompareTo(object r)
        {
            SharedLevel level = r as SharedLevel;
            if (level != null)
            {
                return Compare(this, level);
            }

            throw new ArgumentException("Parameter: r, Value: [" + r?.ToString() + "] is not an instance of Level");
        }

        //
        // Summary:
        //     Returns a value indicating whether a specified log4net.Core.Level is greater
        //     than another specified log4net.Core.Level.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level
        //
        //   r:
        //     A log4net.Core.Level
        //
        // Returns:
        //     true if l is greater than r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator >(SharedLevel l, SharedLevel r)
        {
            return l.m_levelValue > r.m_levelValue;
        }

        //
        // Summary:
        //     Returns a value indicating whether a specified log4net.Core.Level is less than
        //     another specified log4net.Core.Level.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level
        //
        //   r:
        //     A log4net.Core.Level
        //
        // Returns:
        //     true if l is less than r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator <(SharedLevel l, SharedLevel r)
        {
            return l.m_levelValue < r.m_levelValue;
        }

        //
        // Summary:
        //     Returns a value indicating whether a specified log4net.Core.Level is greater
        //     than or equal to another specified log4net.Core.Level.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level
        //
        //   r:
        //     A log4net.Core.Level
        //
        // Returns:
        //     true if l is greater than or equal to r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator >=(SharedLevel l, SharedLevel r)
        {
            return l.m_levelValue >= r.m_levelValue;
        }

        //
        // Summary:
        //     Returns a value indicating whether a specified log4net.Core.Level is less than
        //     or equal to another specified log4net.Core.Level.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level
        //
        //   r:
        //     A log4net.Core.Level
        //
        // Returns:
        //     true if l is less than or equal to r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator <=(SharedLevel l, SharedLevel r)
        {
            return l.m_levelValue <= r.m_levelValue;
        }

        //
        // Summary:
        //     Returns a value indicating whether two specified log4net.Core.Level objects have
        //     the same value.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level or null.
        //
        //   r:
        //     A log4net.Core.Level or null.
        //
        // Returns:
        //     true if the value of l is the same as the value of r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator ==(SharedLevel l, SharedLevel r)
        {
            if (l is object && r is object)
            {
                return l.m_levelValue == r.m_levelValue;
            }

            return (object)l == r;
        }

        //
        // Summary:
        //     Returns a value indicating whether two specified log4net.Core.Level objects have
        //     different values.
        //
        // Parameters:
        //   l:
        //     A log4net.Core.Level or null.
        //
        //   r:
        //     A log4net.Core.Level or null.
        //
        // Returns:
        //     true if the value of l is different from the value of r; otherwise, false.
        //
        // Remarks:
        //     Compares two levels.
        public static bool operator !=(SharedLevel l, SharedLevel r)
        {
            return !(l == r);
        }

        //
        // Summary:
        //     Compares two specified log4net.Core.Level instances.
        //
        // Parameters:
        //   l:
        //     The first log4net.Core.Level to compare.
        //
        //   r:
        //     The second log4net.Core.Level to compare.
        //
        // Returns:
        //     A 32-bit signed integer that indicates the relative order of the two values compared.
        //     The return value has these meanings:
        //
        //     Value – Meaning
        //     Less than zero – l is less than r.
        //     Zero – l is equal to r.
        //     Greater than zero – l is greater than r.
        //
        // Remarks:
        //     Compares two levels.
        public static int Compare(SharedLevel l, SharedLevel r)
        {
            if ((object)l == r)
            {
                return 0;
            }

            if (l == null && r == null)
            {
                return 0;
            }

            if (l == null)
            {
                return -1;
            }

            if (r == null)
            {
                return 1;
            }

            return l.m_levelValue.CompareTo(r.m_levelValue);
        }
        //
        // Summary:
        //     Returns a SharedLevel instance by providing a string value.
        //
        // Parameters:
        //   levelName:
        //     The string name of the level.
        //
        // Returns:
        //     A SharedLevel instance that matches the provided string value.
        //
        // Remarks:
        //     Returns null if no matching level is found.
        public static SharedLevel GetLevelByName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                throw new ArgumentNullException(nameof(levelName));
            }

            foreach (var field in typeof(SharedLevel).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.GetValue(null) is SharedLevel level && level.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                {
                    return level;
                }
            }

            return null;
        }
    }
}