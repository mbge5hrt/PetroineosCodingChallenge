using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using NLog;
using NLog.Targets;
using Services;

namespace PowerPositionReporting
{
    /// <summary>
    /// Initializes and starts Power Position Service
    /// </summary>
    public static class Program
    {
        private const string ExceptionErrorMessage = "An error occurred during service start";

        private static ILogger _logger;

        /// <summary>
        /// Reads the configuration settings, creates a logger and power position reporter and starts the power position service
        /// </summary>
        public static void Main()
        {
            try
            {
                // Open app settings and verify all required keys are present for logging
                var appSettings = System.Configuration.ConfigurationManager.AppSettings;
                VerifyLogAppSettings(appSettings);

                // Use the log filename and log level to create a logger and record them in the log
                var logFilename = appSettings["LogFilename"];
                var logLevel = appSettings["LogLevel"];
                CreateLogger(logFilename, logLevel);
                _logger.Info($"LogFilename={logFilename}");
                _logger.Info($"LogLevel={logLevel}");

                // Verify all required keys are present for reporting
                VerifyReportingAppSettings(appSettings);

                // Access and log the reporting location
                var reportingLocation = appSettings["ReportingLocation"];
                if (!Directory.Exists(reportingLocation))
                    throw new ConfigurationErrorsException($"Reporting location '{reportingLocation}' does not exist");
                _logger.Debug($"ReportingLocation={reportingLocation}");

                // Access the reporting interval, convert to a number and log
                var reportingIntervalText = appSettings["ReportingInterval"];
                var reportingInterval = int.Parse(reportingIntervalText);
                _logger.Debug($"ReportingInterval={reportingInterval}");

                // Access the maximum retries value, convert to a number and log
                var maxRetriesText = appSettings["MaxRetries"];
                var maxRetries = int.Parse(maxRetriesText);
                _logger.Debug($"Retries={maxRetriesText}");

                // Start the service with the loaded configuration
                StartService(reportingLocation, reportingInterval, maxRetries);
            }
            catch (Exception e)
            {
                // If an error occurs, use the logger to log this or, if not available, write the error to the console
                if (_logger != null)
                {
                    _logger.Error(e, ExceptionErrorMessage);
                }
                else
                {
                    Console.WriteLine($@"{ExceptionErrorMessage} {e.Message}");
                }
            }
        }

        /// <summary>
        /// Verifies the settings required to create the logger are present in the configuration settings
        /// </summary>
        private static void VerifyLogAppSettings(NameValueCollection appSettings)
        {
            var appSettingsKeys = appSettings.AllKeys;
            VerifyAppSetting(appSettingsKeys, "LogFilename");
            VerifyAppSetting(appSettingsKeys, "LogLevel");
        }

        /// <summary>
        /// Verifies the settings required to create the reporting components are present in the configuration settings
        /// </summary>
        private static void VerifyReportingAppSettings(NameValueCollection appSettings)
        {
            var appSettingsKeys = appSettings.AllKeys;
            VerifyAppSetting(appSettingsKeys, "ReportingLocation");
            VerifyAppSetting(appSettingsKeys, "ReportingInterval");
            VerifyAppSetting(appSettingsKeys, "MaxRetries");
        }

        /// <summary>
        /// Verifies a named setting is present in the configuration settings
        /// </summary>
        private static void VerifyAppSetting(IEnumerable<string> appSettingsKeys, string keyName)
        {
            if (!appSettingsKeys.Contains(keyName))
                throw new ConfigurationErrorsException($"Missing key '{keyName}' in app settings");
        }

        /// <summary>
        /// Creates and starts a power position service
        /// </summary>
        private static void StartService(string reportingLocation, int reportingInterval, int maxRetries)
        {
            // Retrieve an instance of the power position service and run this
            _logger.Debug("Requesting service start");
            ServiceBase.Run(new ServiceBase[]
            {
                PowerPositionService.GetPowerPositionService(new PowerService(), reportingLocation, reportingInterval, maxRetries)
            });
        }

        /// <summary>
        /// Creates and initializes a file-based logger 
        /// </summary>
        private static void CreateLogger(string logFilename, string logLevel)
        {
            // Initialise the logger as a file logger, set the filename and log level and create an instance
            var target = new FileTarget
            {
                FileName = logFilename
            };
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.FromString(logLevel));
            _logger = LogManager.GetCurrentClassLogger();
        }
    }
}
