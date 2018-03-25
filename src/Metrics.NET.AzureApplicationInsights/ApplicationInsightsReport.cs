using Metrics.MetricData;
using Metrics.Reporters;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics.NET.AzureApplicationInsights
{
    public class ApplicationInsightsReport : BaseReport
    {
        /// <summary>
        /// Set one of the metric Tags with this value to suppress reporting it to Application Insights
        /// </summary>
        /// <example>
        /// <code>
        /// Counter counterSuppressed = Metric.Counter("MySuppressedCounter", Unit.Items, ApplicationInsightsReport.DoNotReport);
        /// </code>
        /// </example>
        public const string DoNotReport = "DoNotReportToAI";

        /// <summary>
        /// App Insights instrumentation key for the AI client
        /// </summary>
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Value used to populate the MetricsSource property of all metrics, to calculate the
        /// SyntheticSource value of the AI Operation, and the OperationName used
        /// to name the report in the AppInsights UI.  Defaults to "Metrics.NET".
        /// </summary>
        public string ReportSource { get; set; }

        /// <summary>
        /// Internal TelemetryClient instance to use for writing report metric data to AI
        /// </summary>
        private TelemetryClient Client;

        /// <summary>
        /// Internal object for holding the IOperationHolder instance for each unique report write operation.
        /// </summary>
        private IOperationHolder<RequestTelemetry> Operation;


        /// <summary>
        /// Constructor to use if you want a unique session ID per individual report write.
        /// </summary>
        /// <param name="instrumentationKey">App Insight instrumentation key</param>
        public ApplicationInsightsReport(string instrumentationKey)
        {
            SetupTelemetryClient(instrumentationKey);
            ReportSource = "Metrics.NET";
            Client.Context.Operation.SyntheticSource = ReportSource + " Report";
        }

        /// <summary>
        /// Constructor to use if you want a fixed session ID for all report writes, or want to
        /// override the ReportSource value.
        /// </summary>
        /// <param name="instrumentationKey">App Insight instrumentation key</param>
        /// <param name="sessionID">Fixed session ID to use for reports, or null</param>
        /// <param name="reportSource">Value for <see cref="ReportSource"/>, or null</param>
        public ApplicationInsightsReport(string instrumentationKey, string sessionID = null, string reportSource = null)
        {
            SetupTelemetryClient(instrumentationKey);

            ReportSource = "Metrics.NET";
            if (!String.IsNullOrEmpty(reportSource))
                ReportSource = reportSource;

            if (!String.IsNullOrEmpty(sessionID))
                Client.Context.Session.Id = sessionID;

            Client.Context.Operation.SyntheticSource = ReportSource + " Report";
        }

        /// <summary>
        /// Internal method to prepare the TelemetryClient before first use
        /// </summary>
        /// <param name="instKey">App Insights instrumentation key</param>
        private void SetupTelemetryClient(string instKey)
        {
            InstrumentationKey = instKey;
            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey =
                            instKey;

            Client = new TelemetryClient(Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active);
            Client.Context.Properties["MetricsSource"] = ReportSource;
        }

        protected override void StartReport(string contextName)
        {
            Client.Context.Properties["MetricsReportContext"] = contextName;
            Operation = Client.StartOperation<RequestTelemetry>(ReportSource.Replace(" ","") + "-Report-" + Guid.NewGuid().ToString());
            base.StartReport(contextName);
        }

        protected override void EndReport(string contextName)
        {
            base.EndReport(contextName);
            Client.Context.Properties.Remove("MetricsReportContext");
            Client.StopOperation(Operation);
            Operation = null;
        }

        protected override void StartContext(string contextName)
        {
            Client.Context.Properties["MetricsContext"] = contextName;
        }
        protected override void EndContext(string contextName)
        {
            Client.Context.Properties.Remove("MetricsContext");
        }
        protected override void StartMetricGroup(string metricName)
        {
            Client.Context.Properties["MetricsGroup"] = metricName;
        }
        protected override void EndMetricGroup(string metricName)
        {
            Client.Context.Properties.Remove("MetricsGroup");
        }

        /// <summary>
        /// Report Gauges as a single value metric
        /// </summary>
        protected override void ReportGauge(string name, double value, Unit unit, MetricTags tags)
        {
            if (tags.Tags.Contains(DoNotReport))
                return;

            // Gauges are mapped to TrackMetric
            var props = new Dictionary<string, string>();
            props["Name"] = name;
            props["Value"] = unit.FormatValue(value);
            props["Unit"] = unit.Name;
            if (tags.Tags != null && tags.Tags.Length > 0)
                props["Tags"] = String.Join(",", tags.Tags);

            Client.TrackMetric(name, value, props);
        }

        /// <summary>
        /// Report Counters as a single value metric
        /// </summary>
        protected override void ReportCounter(string name, CounterValue value, Unit unit, MetricTags tags)
        {
            if (tags.Tags.Contains(DoNotReport))
                return;

            var props = new Dictionary<string, string>();
            props["Name"] = name;
            props["Value"] = unit.FormatCount(value.Count);
            props["Unit"] = unit.Name;
            if (tags.Tags != null && tags.Tags.Length > 0)
                props["Tags"] = String.Join(",", tags.Tags);

            foreach(var kvp in value.Items)
                props[kvp.Item] = unit.FormatValue(kvp.Count);

            Client.TrackMetric(name, value.Count, props);
        }

        /// <summary>
        /// Report Meters as a single metric for the value, plus individual rate metrics
        /// in case the Ops team wants to report or alert on specific subvalues.
        /// </summary>
        protected override void ReportMeter(string name, MeterValue value, Unit unit, TimeUnit rateUnit, MetricTags tags)
        {
            if (tags.Tags.Contains(DoNotReport))
                return;

            var props = new Dictionary<string, string>();
            props["Unit"] = unit.Name;
            props["RateUnit"] = Enum.GetName(typeof(TimeUnit), rateUnit); ;
            if (tags.Tags != null && tags.Tags.Length > 0)
                props["Tags"] = String.Join(",", tags.Tags);

            props["Name"] = name + " {MeanRate}";
            props["Value"] = unit.FormatRate(value.MeanRate, rateUnit);
            Client.TrackMetric(props["Name"], value.MeanRate, props);

            props["Name"] = name + " {OneMinuteRate}";
            props["Value"] = unit.FormatRate(value.OneMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.OneMinuteRate, props);

            props["Name"] = name + " {FiveMinuteRate}";
            props["Value"] = unit.FormatRate(value.FiveMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.FiveMinuteRate, props);

            props["Name"] = name + " {FifteenMinuteRate}";
            props["Value"] = unit.FormatRate(value.FifteenMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.FifteenMinuteRate, props);

            props["Name"] = name;
            props["Value"] = unit.FormatCount(value.Count);
            props["MeanRate"] = unit.FormatRate(value.MeanRate, rateUnit);
            props["OneMinuteRate"] = unit.FormatRate(value.OneMinuteRate, rateUnit);
            props["FiveMinuteRate"] = unit.FormatRate(value.FiveMinuteRate, rateUnit);
            props["FifteenMinuteRate"] = unit.FormatRate(value.FifteenMinuteRate, rateUnit);

            Client.TrackMetric(name, value.Count, props);
        }

        /// <summary>
        /// Report Histograms as a single metric for the last value, plus individual histogram metrics
        /// in case the Ops team wants to report or alert on specific subvalues.
        /// </summary>
        protected override void ReportHistogram(string name, HistogramValue value, Unit unit, MetricTags tags)
        {
            if (tags.Tags.Contains(DoNotReport))
                return;

            var props = new Dictionary<string, string>();
            props["Unit"] = unit.Name;
            if (tags.Tags != null && tags.Tags.Length > 0)
                props["Tags"] = String.Join(",", tags.Tags);

            props["Name"] = name + " {Mean}";
            props["Value"] = unit.FormatValue(value.Mean);
            Client.TrackMetric(props["Name"], value.Mean, props);

            props["Name"] = name + " {Median}";
            props["Value"] = unit.FormatValue(value.Median);
            Client.TrackMetric(props["Name"], value.Median, props);

            props["Name"] = name + " {Percentile75}";
            props["Value"] = unit.FormatValue(value.Percentile75);
            Client.TrackMetric(props["Name"], value.Percentile75, props);

            props["Name"] = name + " {Percentile95}";
            props["Value"] = unit.FormatValue(value.Percentile95);
            Client.TrackMetric(props["Name"], value.Percentile95, props);

            props["Name"] = name + " {Percentile98}";
            props["Value"] = unit.FormatValue(value.Percentile98);
            Client.TrackMetric(props["Name"], value.Percentile98, props);

            props["Name"] = name + " {Percentile99}";
            props["Value"] = unit.FormatValue(value.Percentile99);
            Client.TrackMetric(props["Name"], value.Percentile99, props);

            props["Name"] = name + " {Percentile999}";
            props["Value"] = unit.FormatValue(value.Percentile999);
            Client.TrackMetric(props["Name"], value.Percentile999, props);


            var telm = new MetricTelemetry(name, value.SampleSize, value.Count, value.Min,
                value.Max, value.StdDev);

            telm.Properties["Name"] = name;
            telm.Properties["Value"] = unit.FormatValue(value.LastValue);
            telm.Properties["Unit"] = unit.Name;
            if (tags.Tags != null && tags.Tags.Length > 0)
                telm.Properties["Tags"] = String.Join(",", tags.Tags);

            telm.Properties["Mean"] = unit.FormatValue(value.Mean);
            telm.Properties["Median"] = unit.FormatValue(value.Median);
            telm.Properties["Percentile75"] = unit.FormatValue(value.Percentile75);
            telm.Properties["Percentile95"] = unit.FormatValue(value.Percentile95);
            telm.Properties["Percentile98"] = unit.FormatValue(value.Percentile98);
            telm.Properties["Percentile99"] = unit.FormatValue(value.Percentile99);
            telm.Properties["Percentile999"] = unit.FormatValue(value.Percentile999);

            Client.TrackMetric(telm);

        }

        /// <summary>
        /// Report Timers as a single metric for the time, plus individual rate and duration metrics
        /// in case the Ops team wants to report or alert on specific subvalues.
        /// </summary>
        protected override void ReportTimer(string name, TimerValue value, Unit unit, TimeUnit rateUnit, TimeUnit durationUnit, MetricTags tags)
        {
            if (tags.Tags.Contains(DoNotReport))
                return;

            var props = new Dictionary<string, string>();
            props["Unit"] = unit.Name;
            props["DurationUnit"] = Enum.GetName(typeof(TimeUnit), durationUnit); ;
            if (tags.Tags != null && tags.Tags.Length > 0)
                props["Tags"] = String.Join(",", tags.Tags);

            props["Name"] = name + " {Mean}";
            props["Value"] = unit.FormatDuration(value.Histogram.Mean, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Mean, props);

            props["Name"] = name + " {Median}";
            props["Value"] = unit.FormatDuration(value.Histogram.Median, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Median, props);

            props["Name"] = name + " {Percentile75}";
            props["Value"] = unit.FormatDuration(value.Histogram.Percentile75, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Percentile75, props);

            props["Name"] = name + " {Percentile95}";
            props["Value"] = unit.FormatDuration(value.Histogram.Percentile95, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Percentile95, props);

            props["Name"] = name + " {Percentile98}";
            props["Value"] = unit.FormatDuration(value.Histogram.Percentile98, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Percentile98, props);

            props["Name"] = name + " {Percentile99}";
            props["Value"] = unit.FormatDuration(value.Histogram.Percentile99, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Percentile99, props);

            props["Name"] = name + " {Percentile999}";
            props["Value"] = unit.FormatDuration(value.Histogram.Percentile999, durationUnit);
            Client.TrackMetric(props["Name"], value.Histogram.Percentile999, props);

            props["RateUnit"] = Enum.GetName(typeof(TimeUnit), rateUnit); ;
            props.Remove("DurationUnit");

            props["Name"] = name + " {MeanRate}";
            props["Value"] = unit.FormatRate(value.Rate.MeanRate, rateUnit);
            Client.TrackMetric(props["Name"], value.Rate.MeanRate, props);

            props["Name"] = name + " {OneMinuteRate}";
            props["Value"] = unit.FormatRate(value.Rate.OneMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.Rate.OneMinuteRate, props);

            props["Name"] = name + " {FiveMinuteRate}";
            props["Value"] = unit.FormatRate(value.Rate.FiveMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.Rate.FiveMinuteRate, props);

            props["Name"] = name + " {FifteenMinuteRate}";
            props["Value"] = unit.FormatRate(value.Rate.FifteenMinuteRate, rateUnit);
            Client.TrackMetric(props["Name"], value.Rate.FifteenMinuteRate, props);


            // Timer has both meter and histogram data
            var telm = new MetricTelemetry(name, (int) value.Histogram.Count, value.TotalTime, 
                value.Histogram.Min, value.Histogram.Max, value.Histogram.StdDev);

            telm.Properties["Name"] = name;
            telm.Properties["Value"] = unit.FormatDuration(value.TotalTime, durationUnit);
            telm.Properties["Unit"] = unit.Name;
            telm.Properties["RateUnit"] = Enum.GetName(typeof(TimeUnit), rateUnit); 
            telm.Properties["DurationUnit"] = Enum.GetName(typeof(TimeUnit), durationUnit); 
            if (tags.Tags != null && tags.Tags.Length > 0)
                telm.Properties["Tags"] = String.Join(",", tags.Tags);

            telm.Properties["MeanRate"] = unit.FormatRate(value.Rate.MeanRate, rateUnit);
            telm.Properties["OneMinuteRate"] = unit.FormatRate(value.Rate.OneMinuteRate, rateUnit);
            telm.Properties["FiveMinuteRate"] = unit.FormatRate(value.Rate.FiveMinuteRate, rateUnit);
            telm.Properties["FifteenMinuteRate"] = unit.FormatRate(value.Rate.FifteenMinuteRate, rateUnit);

            telm.Properties["Mean"] = unit.FormatDuration(value.Histogram.Mean, durationUnit);
            telm.Properties["Median"] = unit.FormatDuration(value.Histogram.Median, durationUnit);
            telm.Properties["Percentile75"] = unit.FormatDuration(value.Histogram.Percentile75, durationUnit);
            telm.Properties["Percentile95"] = unit.FormatDuration(value.Histogram.Percentile95, durationUnit);
            telm.Properties["Percentile98"] = unit.FormatDuration(value.Histogram.Percentile98, durationUnit);
            telm.Properties["Percentile99"] = unit.FormatDuration(value.Histogram.Percentile99, durationUnit);
            telm.Properties["Percentile999"] = unit.FormatDuration(value.Histogram.Percentile999, durationUnit);

            Client.TrackMetric(telm);
        }

        /// <summary>
        /// Report health check status as a single Availability metric, with individual
        /// healt checks as properties on the metric.
        /// </summary>
        protected override void ReportHealth(HealthStatus status)
        {
            var rptName = "[" + Client.Context.Properties["MetricsReportContext"] + "] " + Client.Context.Properties["MetricsGroup"];
            var telm = new AvailabilityTelemetry(rptName, ReportTimestamp, TimeSpan.Zero, null, status.IsHealthy);

            foreach (var h in status.Results)
                telm.Properties.Add(h.Name, "[" + (h.Check.IsHealthy ? "Ok" : "FAILED") + "] " + h.Check.Message);
            telm.Success = status.IsHealthy;
            Client.TrackAvailability(telm);
        }
    }
}
