using AdvancedLogging.Interfaces;
using log4net.Appender;

namespace AdvancedLogging.Models
{
    public class LoggerConfig
    {
        private ICommonLogger logger;
        private ICommonLogger loggerLocalErrorFile;
        private ICommonLogger loggerLocalRunFile;
        private int minutesAfterMidnight;
        private int daysInterval;
        private bool autoCleanUpLogFiles;
        private bool remotingLogging;
        private string logFileName;
        private string serviceName;
        private string serviceDisplayName;
        private string clientName;
        private string processName;
        private string clientLogFileName;

        public LoggerConfig()
        {
            minutesAfterMidnight = 60;
            daysInterval = 1;
            autoCleanUpLogFiles = true;
            remotingLogging = false;
            logFileName = string.Empty;
            serviceName = string.Empty;
            serviceDisplayName = string.Empty;
            clientName = string.Empty;
            processName = string.Empty;
            clientLogFileName = string.Empty;
        }

        public string LogFileName
        {
            get => logFileName;
            set => logFileName = value;
        }

        public string ServiceName
        {
            get => serviceName;
            set => serviceName = value;
        }

        public string ServiceDisplayName
        {
            get => serviceDisplayName;
            set => serviceDisplayName = value;
        }

        public string ClientName
        {
            get => clientName;
            set => clientName = value;
        }

        public string ProcessName
        {
            get => processName;
            set => processName = value;
        }

        public string ClientLogFileName
        {
            get => clientLogFileName;
            set => clientLogFileName = value;
        }

        public bool AutoCleanUpLogFiles
        {
            get => autoCleanUpLogFiles;
            set => autoCleanUpLogFiles = value;
        }

        public int MinutesAfterMidnight
        {
            get => minutesAfterMidnight;
            set => minutesAfterMidnight = value;
        }

        public int DaysInterval
        {
            get => daysInterval;
            set => daysInterval = value;
        }

        public ICommonLogger MyLogger
        {
            get => logger;
            set
            {
                logger = value;
                remotingLogging = logger is RemoteSyslogAppender;
            }
        }

        public ICommonLogger MyLoggerLocalErrorFile
        {
            get => loggerLocalErrorFile;
            set => loggerLocalErrorFile = value;
        }

        public ICommonLogger MyLoggerLocalRunFile
        {
            get => loggerLocalRunFile;
            set => loggerLocalRunFile = value;
        }
    }
}