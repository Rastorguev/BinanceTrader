using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;

namespace WebJob1
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }

        [NoAutomaticTrigger]
        public static async Task ProcessMethod(TextWriter log)
        {
            var key = "792fccae-78e5-414f-8bb3-804ec0f6a4d1";

            //TelemetryConfiguration.Active.InstrumentationKey =
            //    CloudConfigurationManager.GetSetting(key);

            var telemetryClient = new TelemetryClient {InstrumentationKey = key};

            while (true)
            {
                //Console.WriteLine((DateTime.Now + " Test"));

                //log.WriteLine(DateTime.Now + " Test");

                //telemetryClient.TrackEvent("TestEvent1");
                //telemetryClient.TrackTrace("TestTrace");
                //telemetryClient.TrackException(new Exception("TEST EXCEPTION"));

                telemetryClient.Flush();

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}