using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Services;

namespace PowerPositionReporting
{
    /// <summary>
    /// Retrieves power position volumes from the power service and generates an aggregated hourly CSV report
    /// </summary>
    public class PowerPositionReporter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IPowerService _powerService;
        private readonly string _reportingPath;

        /// <summary>
        /// Constructor 
        /// </summary>
        public PowerPositionReporter(IPowerService powerService, string reportingPath)
        {
            // Verify a power service object and the reporting path have been provided
            if (powerService == null)
            {
                Exception ane = new ArgumentNullException(nameof(powerService));
                _logger.Error(ane, "Missing power service");
                throw ane;
            }

            if (reportingPath == null)
            {
                Exception ane = new ArgumentNullException(nameof(reportingPath));
                _logger.Error(ane, "Missing reporting path");
                throw ane;
            }

            // Store the power service and the reporting path, ensuring this ends with a backslash '\' character
            _powerService = powerService;
            _reportingPath = reportingPath.EndsWith("\\") ? reportingPath : reportingPath + "\\";

            _logger.Debug($"Reporting path is {_reportingPath}");
        }
        /// <summary>
        /// Generates a power position report asynchronously using the current date/time
        /// </summary>
        public async Task GenerateReportAsync()
        {
            await GenerateReportAsync(DateTime.Now);
        }
        /// <summary>
        /// Generates a power position report asynchronously using the specified date/time
        /// </summary>
        public async Task GenerateReportAsync(DateTime reportDateTime)
        {
            _logger.Info("Report started");
            try
            {
                // Derive the day ahead date from the reporting date, adjust by one hour to map 23:00-23:59 to next day
                var dayAheadDateTime = reportDateTime.AddHours(1).Date;

                // Retrieve the trades from the power service asynchronously, then extract the power period data into a list
                var trades = await _powerService.GetTradesAsync(dayAheadDateTime);
                var periods = trades.Select(t => t.Periods).ToList();
                _logger.Debug($"{periods.Count()} power periods selected");

                // Create a file name to write the power volume data, using the path and a formatted reporting date time
                var filename = GetFilename(reportDateTime);
                _logger.Info($"Report filename is {filename}", filename);

                // Create a writer to write to the file asynchronously
                using (var writer = new StreamWriter(filename))
                {
                    await writer.WriteLineAsync("Local Time,Volume");

                    // Determine the maximum power period across all power trades
                    var maxPeriods = periods.Max(p => p.Length);
                    _logger.Debug($"Maximum period index was {maxPeriods}");

                    // For each power period, sum the volumes and convert the period number into the starting period time and write to file
                    for (var i = 0; i < maxPeriods; ++i)
                    {
                        var periodTime = dayAheadDateTime.AddHours(i - 1).ToShortTimeString();
                        var periodVolumes = periods.Sum(p => p[i].Volume);
                        await writer.WriteLineAsync($"{periodTime},{periodVolumes}");
                    }
                }

                _logger.Info("Report complete");
            }
            catch (PowerServiceException ex)
            {
                LogAndRethrowException(ex, "Report failed whilst retrieving power trades");
            }
            catch (IOException ex)
            {
                LogAndRethrowException(ex, "Report failed whilst writing report");
            }
        }

        /// <summary>
        /// Logs and rethrows exceptions as PowerPositionExceptions
        /// </summary>
        private void LogAndRethrowException(Exception ex, string message)
        {
            _logger.Error(ex, message);
            throw new PowerPositionException(message, ex);
        }

        /// <summary>
        /// Returns a filename of the format (path)/PowerPosition_yyyyMMdd_HHmm.csv is created from the ReportingLocation setting and the reporting date/time 
        /// </summary>
        private string GetFilename(DateTime reportDateTime)
        {
            var reportDateTimeText = reportDateTime.ToString("yyyyMMdd_HHmm");
            var filename = $"{_reportingPath}PowerPosition_{reportDateTimeText}.csv";
            return filename;
        }
    }
}
