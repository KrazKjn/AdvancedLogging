namespace AdvancedLogging.Constants
{
    public class SystemStatus
    {
        // Reserved range 0-127; Maximum value is 255
        public const int DUMP_SVC_INFO = 128;

        // Registry location for the service configuration
        public const string REG_LOCATION = "SOFTWARE\\Company\\Product\\Services";

        // Date format used in logging
        public const string DateFormat = "MM-dd-yyyy HH:mm:ss.fff";

        // Default value for force refresh
        public const bool DefaultForce = false;

        public struct LogFormat
        {
            // General log format
            public const string General = "{0}";

            // Log format for version details
            public const string Version = "VersionDetails/StandardVersion => SW_Version:{0}, DB_Version:{1}";

            // Log format for database details
            public const string Database = "Database Name:{0}, Type:{1}, State:{2}, Version:{3}";

            // Log format for errors
            public const string Error = "{0}\n{1}";

            // Log format for version errors
            public const string VersionError = "VersionDetails/StandardVersion => {0}\n{1}";
        }

        public struct RegistryName
        {
            // Registry key name for minimum refresh time
            public const string MinRefreshTime = "MinRefreshTime";

            // Registry key name for timeout
            public const string Timeout = "Timeout";

            // Registry key name for force refresh
            public const string Force = "ForceRefresh";
        }

        public struct DefaultTime
        {
            // Default minimum refresh time in seconds
            public const double MinRefreshTime = 120; // seconds

            // Default maximum refresh time in seconds
            public const double MaxRefreshTime = 600; // seconds

            // Default minimum timeout in seconds
            public const double MinTimeout = 15; // seconds

            // Default maximum timeout in seconds
            public const double MaxTimeout = 30; // seconds
        }

        public enum CodeTest
        {
            // Code test is off
            Off,

            // Bypass and return true
            BypassReturnTrue,

            // Bypass and return false
            BypassReturnFalse,

            // Inner logic returns true
            InnerReturnTrue,

            // Inner logic returns false
            InnerReturnFalse
        }
    }
}