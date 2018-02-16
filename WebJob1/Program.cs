using Microsoft.Azure.WebJobs;

namespace WebJob1
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    internal class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        private static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }


            var host = new JobHost(config);
            host.CallAsync(typeof(Functions).GetMethod("ProcessMethod"));

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}