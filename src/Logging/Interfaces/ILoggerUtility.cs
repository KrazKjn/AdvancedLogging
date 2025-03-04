using System;

namespace AdvancedLogging.Interfaces
{
    public interface ILoggerUtility
    {
        bool AutoCleanUpLogFiles { get; set; }

        int MinutesAfterMidnight { get; set; }

        int DaysInterval { get; set; }

        ICommonLogger MyLogger { get; set; }

        IDirectoryManager MyDirectoryManager { get; set; }

        void InitializeLogMonitor();

        void CleanUp();

        void CleanUp(string logDirectory, string logPrefix, DateTime date);

        string StringRemovePassword(string str);

        /// <summary>
        /// Translates from a string to a log4net level enumeration.
        /// </summary>
        /// <param name="thesholdString">String representation of the log 4 net logging level: "OFF";"FATAL";"ERROR";"WARN";"INFO";"DEBUG";"ALL"</param>
        /// <returns>log4net level enumeration</returns>
        log4net.Core.Level GetThresholdFromString(string thesholdString);
    }
}
