# Metrics.NET.ApplicationInsights
Metrics.NET reporter for Azure Application Insights

## What it is
Azure Application Insights is an Microsoft Azure cloud-based analytics
product that can accept metrics and availability data from both
cloud as well as private data center servers.  Data is typically
gathered in intervals, from which graphs, reports, and alerts can
be configured to execute in the Azure portal.
https://azure.microsoft.com/en-us/services/application-insights/

Metrics.NET is a metrics instrumentation library for .NET applications
that let you create a few base metrics types, such as gauges and counters,
plus some that have powerful aggregation features such as timers, meters
and histograms.  It also includes a health check module.  In addition
to a number of nice built-in reporters (console, files, etc.) it has
a really nice built-in UI done as a web page, that can be hooked into
web or non-web applications.
https://github.com/Recognos/Metrics.NET

This library is a Metrics.NET reporter for sending Metrics.NET data
to Azure Application Insights.  The data is written as custom metrics
(for all metric data) and availability reports (for health checks).
Once in Application Insights you or your operations team can then
build displays, reports and alerts using the standard AzureAI user
interface.

Using Metrics.NET as your metrics aggregator for sending data to
AzureAI is especially useful to avoid streaming too much data
to that service, which costs in both network and storage.  Using
this Metrics.NET reporter you can choose an interval to report 
(for example every 30 seconds) and the data is bundled up in
an already aggregated form, saving bandwidth and storage.

## Who is it for
This library is useful for developers already using Metrics.NET,
who want to branch out into Azure Application Insights for 
alerting and other AI functions. AzureAI can be an effective way
to analyze and view data from multiple machines at once, which the 
standard web page UI built into Metrics.NET doesn't help with.

## How do I use it
See the AzureAITest app Program.cs file in the src folder for a
simple example of using the reporter syntax with Metrics.NET.

Note you will need to create an Application Insights resource
in the Azure portal, and get the "Instrumentation Key" for that
resource to paste into the App.config file, to start writing
data to your AI repository.

Sample call for Metrics.NET configuration:
	Metric.Config
        //.WithAllCounters()
        .WithReporting(config => config
            .WithConsoleReport(TimeSpan.FromSeconds(30))
            .WithApplicationInsights(ConfigurationManager.AppSettings["AI_InstrumentationKey"].ToString(),
                Guid.NewGuid().ToString(), TimeSpan.FromSeconds(30))
        );


Also note that you can suppress reporting any metric to AzureAI
by adding the value of the ApplicationInsightsReport.DoNotReport
string constant to the Tags of the metric, for example:

    Counter counterSuppressed = Metric.Counter("MySuppressedCounter", Unit.Items, ApplicationInsightsReport.DoNotReport);
    counterSuppressed.Increment();

In this manner you can selectively choose which metrics to send up
to AzureAI in the report bundle.
		