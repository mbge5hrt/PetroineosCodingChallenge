using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using Services;

namespace PowerPositionReporting
{
    /// <summary>
    /// Implements the power position service running at a fixed minute-based interval and generating power position reports. Reports are retried if failed until success or maximum retry count exceeded
    /// </summary>
    public partial class PowerPositionService : ServiceBase
    {
        // Constants used in calculation of timer polling interval
        private const int MillisecondsInOneMinute = 60000;
        private const int IntervalInMilliseconds = 5000; // Must be a factor of 60000

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly PowerPositionReporter _reporter;
        private readonly System.Timers.Timer _intervalTimer;

        // Fields to store app setting parameters
        private readonly int _maxRetries;
        private readonly int _reportingInterval;
        
        // Fields to manage timer and retries
        private bool _runReport;
        private int _intervalCounter;
        private int _retriesRemaining;

        /// <summary>
        /// Constructor initializes the service using a power position reporter, defined reporting interval and maximum number of retries
        /// </summary>
        private PowerPositionService(PowerPositionReporter reporter, int reportingInterval, int maxRetries)
        {
            InitializeComponent();
            _reporter = reporter;
            _reportingInterval = reportingInterval;
            _maxRetries = maxRetries;

            _logger.Debug($"Creating timer with interval of {IntervalInMilliseconds} milliseconds");
            _intervalTimer = new System.Timers.Timer(IntervalInMilliseconds);
            _intervalTimer.Elapsed += OnTimedEvent;
        }

        /// <summary>
        /// Called when service starts, reporting occurs immediately and then at fixed intervals
        /// </summary>
        protected override async void OnStart(string[] args)
        {
            _logger.Info("Service starting");

            // Reporting occurs immediately the service starts
            await RunReportAsync();

            // Reporting repeats after timer interval
            ResetIntervalCounter();
            _intervalTimer.Enabled = true;
        }

        /// <summary>
        /// Called at a shorter interval than the reporting interval to check if reporting interval exceeded or to run reporting if required
        /// </summary>
        private async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _logger.Debug("Service timer callback");

            // Decrement interval counter and when reaches 0, it is time to run the report
            _intervalCounter--;
            if (_intervalCounter <= 0)
            {
                _logger.Debug("Reporting interval complete");
                _runReport = true;
                ResetIntervalCounter();
            }

            // Report will run if the interval counter has just reset or previous attempts to run have failed and there are retries remaining
            if (_runReport)
                await RunReportAsync();
        }

        /// <summary>
        /// Resets interval counter and initial retry count
        /// </summary>
        private void ResetIntervalCounter()
        {
            _intervalCounter = _reportingInterval * (MillisecondsInOneMinute / IntervalInMilliseconds);
            _retriesRemaining = _maxRetries;
        }

        /// <summary>
        /// Runs the power position reporting asynchronously and reduces remaining retry count on failure
        /// </summary>
        private async Task RunReportAsync()
        {
            _logger.Debug("Report run required");
            try
            {
                // Try to generate the report and if successful, set the flag to run again to false until next interval has elapsed
                _runReport = false;
                await _reporter.GenerateReportAsync();
            }
            catch (Exception)
            {
                // Decrement retry count and if maximum number of retries has been exceeded, reporting is abandoned until next interval has elapsed.
                // If maximum retries is set to -1 then it will be retried until succeeded
                _retriesRemaining--;

                if (_retriesRemaining > 0)
                {
                    _logger.Info($"Report failed - {_retriesRemaining} retries remaining");
                    _runReport = true;
                }
                else
                {
                    _logger.Info("Report failed - no retries remaining");
                }
            }
        }

        /// <summary>
        /// Called when service stops, disabling the interval timer and scheduled reporting
        /// </summary>
        protected override void OnStop()
        {
            _logger.Info("Service stopping");
            _intervalTimer.Enabled = false;
        }

        /// <summary>
        /// Creates and returns a new instance of the power position service
        /// </summary>
        public static PowerPositionService GetPowerPositionService(IPowerService powerService, string reportingPath, int reportingInterval, int maxRetries)
        {
            LogManager.GetCurrentClassLogger().Debug("Creating instance of service");
            return new PowerPositionService(new PowerPositionReporter(powerService, reportingPath), reportingInterval, maxRetries);
        }
    }
}
