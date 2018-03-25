using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using Metrics.NET.AzureApplicationInsights;

namespace AzureAITest
{
    class Program
    {
        static void Main(string[] args)
        {
            Metric.Config
                //.WithAllCounters()
                .WithReporting(config => config
                    .WithConsoleReport(TimeSpan.FromSeconds(30))
                    .WithApplicationInsights(ConfigurationManager.AppSettings["AI_InstrumentationKey"].ToString(),
                        Guid.NewGuid().ToString(), TimeSpan.FromSeconds(30))
                );

            // create some metrics
            Counter counter = Metric.Counter("MyItemCounter", Unit.Items);
            counter.Increment();
            counter.Increment();
            counter.Increment();


            Counter counterSuppressed = Metric.Counter("MySuppressedCounter", Unit.Items, ApplicationInsightsReport.DoNotReport);
            counterSuppressed.Increment();

            Metric.Gauge("MyPercentageGauge", () => DateTime.Now.Second, Unit.Percent);

            Meter meter = Metric.Meter("MyErrorsPerSecMeter", Unit.Errors, TimeUnit.Seconds);
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();
            meter.Mark();

            Histogram histogram = Metric.Histogram("MyItemsHistogram", Unit.Items);
            histogram.Update(456);
            histogram.Update(123);
            histogram.Update(789);

            HealthChecks.RegisterHealthCheck("Seconds", () =>
            {
                if (DateTime.Now.Second > 55)
                {
                    return HealthCheckResult.Unhealthy("Seconds > 55");
                }
                else
                {
                    return HealthCheckResult.Healthy("Seconds <= 55");
                }
            });
            HealthChecks.RegisterHealthCheck("AlwaysTrue", () => HealthCheckResult.Healthy("Always True check"));

            var rnd = new Random();

            Metrics.Timer timer = Metric.Timer("MyEventsTimer", Unit.Events);
            for (int i = 0; i < 3; i++)
            {
                using (var context = timer.NewContext("TimerCtx"))
                {
                    Thread.Sleep(132 + rnd.Next(15));
                }
            }

            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
        }
    }
}
