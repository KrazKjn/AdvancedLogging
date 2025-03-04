using AdvancedLogging.Loggers;
using AdvancedLogging.Tests.Utilities;
using System.Collections.Concurrent;

namespace AdvancedLogging.Tests.Loggers
{
    public class SeriLoggerTests
    {
        private readonly SeriLogger _logger;

        public SeriLoggerTests()
        {
            _logger = new SeriLogger();
        }

        [Fact]
        public void Debug_ShouldLogMessage()
        {
            // Arrange
            var message = "Debug message";

            // Act
            _logger.Debug(message);

            // Assert
            // Add your assertions here
        }

        [Fact]
        public void Error_ShouldLogMessage()
        {
            // Arrange
            var message = "Error message";

            // Act
            _logger.Error(message);

            // Assert
            // Add your assertions here
        }

        [Fact]
        public void Fatal_ShouldLogMessage()
        {
            // Arrange
            var message = "Fatal message";

            // Act
            _logger.Fatal(message);

            // Assert
            // Add your assertions here
        }

        [Fact]
        public void Info_ShouldLogMessage()
        {
            // Arrange
            var message = "Info message";

            // Act
            _logger.Info(message);

            // Assert
            // Add your assertions here
        }

        [Fact]
        public void Warn_ShouldLogMessage()
        {
            // Arrange
            var message = "Warn message";

            // Act
            _logger.Warn(message);

            // Assert
            // Add your assertions here
        }

        [Fact]
        public void ConfigFile_ShouldTriggerConfigFileChangedEvent()
        {
            // Arrange
            var configFile = $"{GetType().Name}.config";
            using (var eventSlim = new ManualResetEventSlim(false))
            {
                _logger.ConfigFileChanged += (sender, args) => eventSlim.Set();

                // Act
                _logger.ConfigFile = configFile;
                _logger.Monitoring = true;
                FileUtilities.Touch(_logger.ConfigFile);

                // Assert
                bool eventTriggered = eventSlim.Wait(3000); // Wait for up to 2 seconds
                Assert.True(eventTriggered, "The ConfigFileChanged event was not triggered within the timeout period.");
            }
        }

        [Fact]
        public void AutoLogSQLThreshold_ShouldSetAndGet()
        {
            // Arrange
            var threshold = 5.0;

            // Act
            _logger.AutoLogSQLThreshold = threshold;

            // Assert
            Assert.Equal(threshold, _logger.AutoLogSQLThreshold);
        }

        [Fact]
        public void LogLevel_ShouldSetAndGet()
        {
            // Arrange
            var logLevel = 3;

            // Act
            _logger.LogLevel = logLevel;

            // Assert
            Assert.Equal(logLevel, _logger.LogLevel);
        }

        [Fact]
        public void Monitoring_ShouldEnableAndDisable()
        {
            // Arrange
            var monitoring = true;
            var configFile = $"{GetType().Name}.config";

            // Act
            _logger.ConfigFile = configFile;
            _logger.Monitoring = monitoring;

            // Assert
            Assert.Equal(monitoring, _logger.Monitoring);
        }

        [Fact]
        public void Monitoring_ShouldThrowException()
        {
            // Arrange
            var monitoring = true;

            // Act
            var exception = Assert.Throws<FileNotFoundException>(() => _logger.Monitoring = monitoring);

            // Assert
            Assert.Equal("ConfigFile not set before Enabling ConfigMonitoring.", exception.Message);
        }

        [Fact]
        public void DebugLevels_ShouldSetAndGet()
        {
            // Arrange
            var debugLevels = new Dictionary<string, int> { { "Test", 1 } };

            // Act
            _logger.DebugLevels = debugLevels;

            // Assert
            Assert.Equal(debugLevels, _logger.DebugLevels);
        }

        [Fact]
        public void MonitoredSettings_ShouldSetAndGet()
        {
            // Arrange
            var monitoredSettings = new ConcurrentDictionary<string, string>();
            monitoredSettings.TryAdd("Test", "Value");

            // Act
            _logger.MonitoredSettings = monitoredSettings;

            // Assert
            Assert.Equal(monitoredSettings, _logger.MonitoredSettings);
        }

        [Fact]
        public void IsPassword_ShouldSetAndGet()
        {
            // Arrange
            var isPassword = new ConcurrentDictionary<string, bool>();
            isPassword.TryAdd("Test", true);

            // Act
            _logger.IsPassword = isPassword;

            // Assert
            Assert.Equal(isPassword, _logger.IsPassword);
        }
    }
}
