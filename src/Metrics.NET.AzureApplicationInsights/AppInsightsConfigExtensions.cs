using Metrics.Reports;
using System;


namespace Metrics.NET.AzureApplicationInsights
{
    public static class AppInsightsConfigExtensions
    {
        /// <summary>
		/// Schedule a Report to be executed and sent to Application Insights at a fixed <paramref name="interval"/>.
		/// </summary>
		/// <param name="instrumentationKey">Instrumentation key from Application Insights</param>
		/// <param name="interval">Interval at which to run the report.</param>
		public static MetricsReports WithApplicationInsights(this MetricsReports reports, string instrumentationKey, TimeSpan interval)
        {
            return reports.WithReport(new ApplicationInsightsReport(instrumentationKey), interval);
        }

        /// <summary>
		/// Schedule a Report to be executed and sent to Application Insights at a fixed <paramref name="interval"/>,
        /// including a single static session ID that is set for all reports during this
        /// run of the application.
		/// </summary>
		/// <param name="instrumentationKey">Instrumentation key from Application Insights</param>
		/// <param name="sessionId">static session ID to include with all report runs</param>
        /// <param name="interval">Interval at which to run the report.</param>
        /// <remarks>Setting the session ID to a random GUID ex. Guid.NewGuid().ToString() would 
        /// allow you to track all the metrics reports for a single execution of the 
        /// app/service. A change in session ID would indicate the app had restarted.</remarks>
        public static MetricsReports WithApplicationInsights(this MetricsReports reports, string instrumentationKey, string sessionId, TimeSpan interval)
        {
            return reports.WithReport(new ApplicationInsightsReport(instrumentationKey, sessionId), interval);
        }

        /// <summary>
		/// Schedule a Report to be executed and sent to Application Insights at a fixed <paramref name="interval"/>,
        /// including a single static session ID that is set for all reports during this
        /// run of the application.
		/// </summary>
		/// <param name="instrumentationKey">Instrumentation key from Application Insights</param>
		/// <param name="sessionId">static session ID to include with all report runs, or null</param>
        /// <param name="reportSource">value to override ReportSource, or null</param>
        /// <param name="interval">Interval at which to run the report.</param>
        /// <remarks>Setting the session ID to a random GUID ex. Guid.NewGuid().ToString() would 
        /// allow you to track all the metrics reports for a single execution of the 
        /// app/service. A change in session ID would indicate the app had restarted.</remarks>
        public static MetricsReports WithApplicationInsights(this MetricsReports reports, string instrumentationKey, string sessionId, string reportSource, TimeSpan interval)
        {
            return reports.WithReport(new ApplicationInsightsReport(instrumentationKey, sessionId, reportSource), interval);
        }

    }
}
